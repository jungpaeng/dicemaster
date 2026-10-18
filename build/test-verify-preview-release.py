"""Regression tests for prerelease scope/evidence boundaries; no build or game run."""
from copy import deepcopy
import importlib.util
from pathlib import Path
import unittest


spec = importlib.util.spec_from_file_location(
    'preview_verifier', Path(__file__).with_name('verify-preview-release.py'))
preview = importlib.util.module_from_spec(spec)
spec.loader.exec_module(preview)


class PreviewValidationTests(unittest.TestCase):
    def setUp(self):
        self.prd2 = {'candidate_validation': {
            'windows_runtime': 'not-run', 'real_lua_comparison': 'not-run',
            'historical_windows_confirmation_applies_to_this_candidate': False,
        }}
        self.prd4 = {
            'candidate_validation': deepcopy(preview.PRD4_VALIDATION),
            'runtime_equivalence': preview.PRD4_RUNTIME_BOUNDARY,
            'windows_validation': {'new_release_dll_directly_windows_tested': False},
            'tests': {name: ['PASS: candidate checks passed.']
                      for name in preview.PRD4_REQUIRED_TESTS},
        }

    def test_scope_contracts_accept_their_own_manifest(self):
        preview.validate_preview_candidate(self.prd2, 'prd2')
        preview.validate_preview_candidate(self.prd4, 'prd4')

    def test_scopes_cannot_silently_replace_each_other(self):
        for manifest, scope in ((self.prd4, 'prd2'), (self.prd2, 'prd4')):
            with self.subTest(scope=scope), self.assertRaises(RuntimeError):
                preview.validate_preview_candidate(manifest, scope)

    def test_prd4_boundaries_require_exact_pending_evidence(self):
        for key in preview.PRD4_VALIDATION:
            manifest = deepcopy(self.prd4)
            manifest['candidate_validation'][key] = 'confirmed'
            with self.subTest(key=key), self.assertRaises(RuntimeError):
                preview.validate_preview_candidate(manifest, 'prd4')
        manifest = deepcopy(self.prd4)
        manifest['candidate_validation']['historical_windows_confirmation_applies_to_this_candidate'] = 0
        with self.assertRaises(RuntimeError):
            preview.validate_preview_candidate(manifest, 'prd4')

    def test_all_eleven_test_programs_are_required(self):
        self.assertEqual(len(preview.PRD4_REQUIRED_TESTS), 11)
        for name in preview.PRD4_REQUIRED_TESTS:
            manifest = deepcopy(self.prd4)
            del manifest['tests'][name]
            with self.subTest(name=name), self.assertRaises(RuntimeError):
                preview.validate_preview_candidate(manifest, 'prd4')

    def test_empty_or_malformed_test_evidence_is_rejected(self):
        for evidence in ([], '', [''], ['   '], [None], ['PASS:', None]):
            manifest = deepcopy(self.prd4)
            manifest['tests']['LogUiWiringTests'] = evidence
            with self.subTest(evidence=evidence), self.assertRaises(RuntimeError):
                preview.validate_preview_candidate(manifest, 'prd4')

    def test_candidate_windows_claims_are_rejected(self):
        for value in (None, True, 0, 'confirmed'):
            manifest = deepcopy(self.prd4)
            manifest['windows_validation']['new_release_dll_directly_windows_tested'] = value
            with self.subTest(value=value), self.assertRaises(RuntimeError):
                preview.validate_preview_candidate(manifest, 'prd4')
        manifest = deepcopy(self.prd4)
        manifest['runtime_equivalence'] = 'verified'
        with self.assertRaises(RuntimeError):
            preview.validate_preview_candidate(manifest, 'prd4')

    def test_completion_and_equivalence_claims_are_rejected(self):
        for scope, original in (('prd2', self.prd2), ('prd4', self.prd4)):
            for key in ('historical_full_runtime_equivalence_claimed', 'feature_or_issue_completion_claimed'):
                manifest = deepcopy(original)
                manifest[key] = True
                with self.subTest(scope=scope, key=key), self.assertRaises(RuntimeError):
                    preview.validate_preview_candidate(manifest, scope)

    def test_missing_validation_and_unknown_scope_are_rejected(self):
        with self.assertRaises(RuntimeError):
            preview.validate_preview_candidate({}, 'prd4')
        with self.assertRaises(RuntimeError):
            preview.validate_preview_candidate(self.prd4, 'unknown')


if __name__ == '__main__':
    unittest.main()
