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
        self.prd5 = {
            'candidate_validation': deepcopy(preview.PRD5_VALIDATION),
            'runtime_equivalence': preview.PRD5_RUNTIME_BOUNDARY,
            'implementation_baseline': deepcopy(preview.PRD5_IMPLEMENTATION_BASELINE),
            'windows_validation': deepcopy(preview.PRD5_WINDOWS_VALIDATION),
            'tests': {name: ['PASS: candidate checks passed.']
                      for name in preview.PRD5_REQUIRED_TESTS},
        }

    def test_scope_contracts_accept_their_own_manifest(self):
        preview.validate_preview_candidate(self.prd2, 'prd2')
        preview.validate_preview_candidate(self.prd4, 'prd4')
        preview.validate_preview_candidate(self.prd5, 'prd5')

    def test_scopes_cannot_silently_replace_each_other(self):
        manifests = {'prd2': self.prd2, 'prd4': self.prd4, 'prd5': self.prd5}
        for original_scope, manifest in manifests.items():
            for scope in manifests:
                if scope != original_scope:
                    with self.subTest(original_scope=original_scope, scope=scope), self.assertRaises(RuntimeError):
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

    def test_all_twelve_test_programs_are_required(self):
        self.assertEqual(len(preview.PRD4_REQUIRED_TESTS), 12)
        self.assertIn('LogOpenerIntegrationTests', preview.PRD4_REQUIRED_TESTS)
        for name in preview.PRD4_REQUIRED_TESTS:
            manifest = deepcopy(self.prd4)
            del manifest['tests'][name]
            with self.subTest(name=name), self.assertRaises(RuntimeError):
                preview.validate_preview_candidate(manifest, 'prd4')

    def test_prd5_boundaries_require_exact_pending_evidence(self):
        for key in preview.PRD5_VALIDATION:
            for value in ('confirmed', None):
                manifest = deepcopy(self.prd5)
                manifest['candidate_validation'][key] = value
                with self.subTest(key=key, value=value), self.assertRaises(RuntimeError):
                    preview.validate_preview_candidate(manifest, 'prd5')
        manifest = deepcopy(self.prd5)
        manifest['candidate_validation']['historical_windows_confirmation_applies_to_this_candidate'] = 0
        with self.assertRaises(RuntimeError):
            preview.validate_preview_candidate(manifest, 'prd5')
        manifest = deepcopy(self.prd5)
        manifest['candidate_validation']['extra_completion_claim'] = True
        with self.assertRaises(RuntimeError):
            preview.validate_preview_candidate(manifest, 'prd5')

    def test_prd5_baseline_must_identify_user_confirmed_v105_without_added_claims(self):
        for key in preview.PRD5_IMPLEMENTATION_BASELINE:
            for missing in (False, True):
                manifest = deepcopy(self.prd5)
                if missing:
                    del manifest['implementation_baseline'][key]
                else:
                    manifest['implementation_baseline'][key] = 'candidate confirmed'
                with self.subTest(key=key, missing=missing), self.assertRaises(RuntimeError):
                    preview.validate_preview_candidate(manifest, 'prd5')
        for value in (None, {}, {'release': 'v1.0.5'}):
            manifest = deepcopy(self.prd5)
            manifest['implementation_baseline'] = value
            with self.subTest(value=value), self.assertRaises(RuntimeError):
                preview.validate_preview_candidate(manifest, 'prd5')
        manifest = deepcopy(self.prd5)
        manifest['implementation_baseline']['candidate_directly_tested'] = True
        with self.assertRaises(RuntimeError):
            preview.validate_preview_candidate(manifest, 'prd5')

    def test_all_fourteen_prd5_test_programs_are_required(self):
        self.assertEqual(len(preview.PRD5_REQUIRED_TESTS), 14)
        self.assertEqual(preview.PRD5_REQUIRED_TESTS - preview.PRD4_REQUIRED_TESTS,
                         {'EventSelectionRuntimeTests', 'EventPopupTests'})
        for name in preview.PRD5_REQUIRED_TESTS:
            manifest = deepcopy(self.prd5)
            del manifest['tests'][name]
            with self.subTest(name=name), self.assertRaises(RuntimeError):
                preview.validate_preview_candidate(manifest, 'prd5')

    def test_prd5_rejects_missing_or_malformed_test_collections_and_evidence(self):
        for value in (None, [], '', {}):
            manifest = deepcopy(self.prd5)
            manifest['tests'] = value
            with self.subTest(value=value), self.assertRaises(RuntimeError):
                preview.validate_preview_candidate(manifest, 'prd5')
        for name in preview.PRD5_REQUIRED_TESTS:
            for evidence in ([], '', [''], ['   '], [None], ['PASS:', None]):
                manifest = deepcopy(self.prd5)
                manifest['tests'][name] = evidence
                with self.subTest(name=name, evidence=evidence), self.assertRaises(RuntimeError):
                    preview.validate_preview_candidate(manifest, 'prd5')

    def test_empty_or_malformed_test_evidence_is_rejected(self):
        for name in ('LogUiWiringTests', 'LogOpenerIntegrationTests'):
            for evidence in ([], '', [''], ['   '], [None], ['PASS:', None]):
                manifest = deepcopy(self.prd4)
                manifest['tests'][name] = evidence
                with self.subTest(name=name, evidence=evidence), self.assertRaises(RuntimeError):
                    preview.validate_preview_candidate(manifest, 'prd4')

    def test_candidate_windows_claims_are_rejected(self):
        for scope, original in (('prd4', self.prd4), ('prd5', self.prd5)):
            for value in (None, True, 0, 'confirmed'):
                manifest = deepcopy(original)
                manifest['windows_validation']['new_release_dll_directly_windows_tested'] = value
                with self.subTest(scope=scope, value=value), self.assertRaises(RuntimeError):
                    preview.validate_preview_candidate(manifest, scope)
            manifest = deepcopy(original)
            manifest['runtime_equivalence'] = 'verified'
            with self.subTest(scope=scope), self.assertRaises(RuntimeError):
                preview.validate_preview_candidate(manifest, scope)

    def test_prd5_rejects_missing_windows_boundary_or_runtime_equivalence(self):
        for key in ('windows_validation', 'runtime_equivalence'):
            for value in (None, {}, ''):
                manifest = deepcopy(self.prd5)
                manifest[key] = value
                with self.subTest(key=key, value=value), self.assertRaises(RuntimeError):
                    preview.validate_preview_candidate(manifest, 'prd5')
            manifest = deepcopy(self.prd5)
            del manifest[key]
            with self.subTest(key=key), self.assertRaises(RuntimeError):
                preview.validate_preview_candidate(manifest, 'prd5')

    def test_prd5_current_windows_metadata_cannot_inherit_past_confirmation(self):
        for key in preview.PRD5_WINDOWS_VALIDATION:
            for missing in (False, True):
                manifest = deepcopy(self.prd5)
                if missing:
                    del manifest['windows_validation'][key]
                else:
                    manifest['windows_validation'][key] = 'user-confirmed'
                with self.subTest(key=key, missing=missing), self.assertRaises(RuntimeError):
                    preview.validate_preview_candidate(manifest, 'prd5')
        manifest = deepcopy(self.prd5)
        manifest['windows_validation']['tested_release'] = 'v1.0.5'
        with self.assertRaises(RuntimeError):
            preview.validate_preview_candidate(manifest, 'prd5')

    def test_completion_and_equivalence_claims_are_rejected(self):
        for scope, original in (('prd2', self.prd2), ('prd4', self.prd4), ('prd5', self.prd5)):
            for key in ('historical_full_runtime_equivalence_claimed', 'feature_or_issue_completion_claimed'):
                for value in (True, 0, None, 'complete'):
                    manifest = deepcopy(original)
                    manifest[key] = value
                    with self.subTest(scope=scope, key=key, value=value), self.assertRaises(RuntimeError):
                        preview.validate_preview_candidate(manifest, scope)

    def test_missing_validation_and_unknown_scope_are_rejected(self):
        for scope in ('prd2', 'prd4', 'prd5'):
            with self.subTest(scope=scope), self.assertRaises(RuntimeError):
                preview.validate_preview_candidate({}, scope)
        with self.assertRaises(RuntimeError):
            preview.validate_preview_candidate(self.prd4, 'unknown')


if __name__ == '__main__':
    unittest.main()
