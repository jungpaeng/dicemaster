"""Materialize the fixed comparison DLL from local Git history, never the worktree.

Only audit/recovery/legacy tools call this helper. Ordinary source builds import
the identity constants without reading Git history or an existing mod DLL.
"""
from pathlib import Path
import hashlib
import os
import subprocess
import tempfile


REPOSITORY = Path(__file__).resolve().parent.parent
BASELINE_COMMIT = 'f8d8dd8610f20ccc323d8c3e0386bc5b98bcaeec'
BASELINE_SHA256 = '485581c2b6d2eb9270e7e11ee5315a9e987dcc3a9b93f79e78a7d0ced4ee3633'
BASELINE_GIT_PATH = 'plugins/DiceMaster/DiceMaster.dll'
BASELINE_VERSION = 'v1.0.3-alpha.3'


def baseline_cache_path(repository=REPOSITORY):
    """Return the generated cache location without reading Git or a DLL."""
    return Path(repository).resolve() / 'artifacts/baselines' / BASELINE_VERSION / 'DiceMaster.dll'


def protect_baseline_output(output, repository=REPOSITORY):
    """Keep legacy patch outputs out of the immutable comparison cache."""
    reserved = Path(repository).resolve() / 'artifacts/baselines'
    output = Path(output).resolve()
    cached = baseline_cache_path(repository)
    aliases_cache = output.exists() and cached.exists() and output.samefile(cached)
    if output == reserved.resolve() or output.is_relative_to(reserved.resolve()) or aliases_cache:
        raise RuntimeError('Output must not overwrite the generated baseline cache: ' + str(output))


def get_baseline(repository=REPOSITORY):
    """Read and verify the pinned Git blob, then atomically refresh its cache.

    A valid cached DLL is never a fallback for missing local Git history. No
    network access or fetch is performed. Git replacement objects are disabled
    so the fixed commit continues to identify the original object graph.
    """
    repository = Path(repository).resolve()
    command = ['git', '--no-replace-objects', '-C', str(repository), 'cat-file', 'blob',
               BASELINE_COMMIT + ':' + BASELINE_GIT_PATH]
    try:
        result = subprocess.run(command, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
    except OSError as error:
        raise RuntimeError('Git is required to recreate the fixed baseline DLL: ' + str(error)) from error
    if result.returncode:
        detail = result.stderr.decode('utf-8', errors='replace').strip()
        raise RuntimeError('Cannot read the fixed baseline DLL from local Git history at ' +
                           BASELINE_COMMIT + ':' + BASELINE_GIT_PATH + '. '
                           'The commit must be present locally; cached DLLs are not a fallback. ' + detail)
    actual = hashlib.sha256(result.stdout).hexdigest()
    if actual != BASELINE_SHA256:
        raise RuntimeError('Fixed Git baseline SHA-256 mismatch: expected ' + BASELINE_SHA256 +
                           ', actual ' + actual)

    destination = baseline_cache_path(repository)
    # Refuse symlink traversal or replacing an unrelated special file in the
    # generated cache. Only this fixed DLL and ordinary cache directories belong
    # here; an ordinary stale/modified DLL is safely regenerated from Git.
    directory = repository
    for name in destination.relative_to(repository).parts[:-1]:
        directory = directory / name
        if directory.is_symlink() or (directory.exists() and not directory.is_dir()):
            raise RuntimeError('Unsafe baseline cache directory: ' + str(directory))
        directory.mkdir(exist_ok=True)
    if destination.is_symlink() or (destination.exists() and not destination.is_file()):
        raise RuntimeError('Unsafe baseline cache file: ' + str(destination))
    if destination.is_file() and destination.read_bytes() == result.stdout:
        return destination

    temporary = None
    try:
        with tempfile.NamedTemporaryFile(prefix='.DiceMaster.', suffix='.tmp',
                                         dir=destination.parent, delete=False) as stream:
            temporary = Path(stream.name)
            stream.write(result.stdout)
        if destination.is_symlink() or (destination.exists() and not destination.is_file()):
            raise RuntimeError('Unsafe baseline cache file: ' + str(destination))
        os.replace(temporary, destination)
    finally:
        if temporary is not None:
            temporary.unlink(missing_ok=True)
    return destination


if __name__ == '__main__':
    try:
        baseline = get_baseline()
        print('BASELINE DLL:', baseline)
        print('BASELINE COMMIT:', BASELINE_COMMIT)
        print('BASELINE SHA-256:', BASELINE_SHA256)
    except Exception as error:
        raise SystemExit('BASELINE RECREATION FAILED: ' + str(error))
