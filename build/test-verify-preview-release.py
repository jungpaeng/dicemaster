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

        self.prd6 = {
            'candidate_validation': deepcopy(preview.PRD6_VALIDATION),
            'runtime_equivalence': preview.PRD6_RUNTIME_BOUNDARY,
            'implementation_baseline': deepcopy(preview.PRD6_IMPLEMENTATION_BASELINE),
            'windows_validation': deepcopy(preview.PRD6_WINDOWS_VALIDATION),
            'windows_runtime_executed': False,
            'historical_full_runtime_equivalence_claimed': False,
            'feature_or_issue_completion_claimed': False,
            'tests': {name: ['PASS: candidate checks passed.']
                      for name in preview.PRD6_REQUIRED_TESTS},
        }

    def test_scope_contracts_accept_their_own_manifest(self):
        preview.validate_preview_candidate(self.prd2, 'prd2')
        preview.validate_preview_candidate(self.prd4, 'prd4')
        preview.validate_preview_candidate(self.prd5, 'prd5')
        preview.validate_preview_candidate(self.prd6, 'prd6')

    def test_scopes_cannot_silently_replace_each_other(self):
        manifests = {'prd2': self.prd2, 'prd4': self.prd4, 'prd5': self.prd5, 'prd6': self.prd6}
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
        for scope, original in (('prd2', self.prd2), ('prd4', self.prd4), ('prd5', self.prd5), ('prd6', self.prd6)):
            for key in ('historical_full_runtime_equivalence_claimed', 'feature_or_issue_completion_claimed'):
                for value in (True, 0, None, 'complete'):
                    manifest = deepcopy(original)
                    manifest[key] = value
                    with self.subTest(scope=scope, key=key, value=value), self.assertRaises(RuntimeError):
                        preview.validate_preview_candidate(manifest, scope)

    def test_all_twenty_prd6_programs_required_with_pass_evidence(self):
        self.assertEqual(len(preview.PRD6_REQUIRED_TESTS), 22)
        self.assertEqual(preview.PRD6_REQUIRED_TESTS - preview.PRD5_REQUIRED_TESTS,
                         {'MapInformationTests', 'MapRouteTests', 'MapGuideRouteIntegrationTests', 'ShopInformationTests',
                          'BranchHistoryTests', 'InformationRuntimeTests', 'ChoiceStoryTests', 'DiceGuideTests'})
        for name in preview.PRD6_REQUIRED_TESTS:
            manifest = deepcopy(self.prd6)
            del manifest['tests'][name]
            with self.subTest(name=name), self.assertRaises(RuntimeError):
                preview.validate_preview_candidate(manifest, 'prd6')
            for evidence in ([], '', [''], ['   '], [None], ['PASS:'], ['FAIL: no'],
                             ['PASS: valid', None], ['PASS: valid', 'error']):
                manifest = deepcopy(self.prd6)
                manifest['tests'][name] = evidence
                with self.subTest(name=name, evidence=evidence), self.assertRaises(RuntimeError):
                    preview.validate_preview_candidate(manifest, 'prd6')
        for tests in (None, {}, [], ''):
            manifest = deepcopy(self.prd6)
            manifest['tests'] = tests
            with self.subTest(tests=tests), self.assertRaises(RuntimeError):
                preview.validate_preview_candidate(manifest, 'prd6')

    def test_prd6_accepts_actual_twenty_suite_success_record_formats(self):
        manifest = deepcopy(self.prd6)
        # These are the existing build/source_build.py result prefixes. The
        # lifecycle program has no PASS prefix; metadata supplements API PASS.
        manifest['tests']['LifecycleTests'] = ['Lifecycle assertions passed: 4023']
        manifest['tests']['RealGameApiTests'] = [
            'METADATA CONTRACT: actual game/Harmony target signatures and pure description wrappers verified; native game NOT executed.',
            'PASS: 4265 managed behavior assertions and 1278 metadata contract assertions; native game runtime NOT executed.',
        ]
        self.assertEqual(len(manifest['tests']), 22)
        preview.validate_preview_candidate(manifest, 'prd6')

    def test_prd6_rejects_metadata_only_wrong_suite_and_invalid_success_counts(self):
        cases = {
            'RealGameApiTests': [
                ['METADATA CONTRACT: signatures verified'],
                ['METADATA CONTRACT:'],
                ['METADATA CONTRACT: signatures verified', 'FAIL: behavior failed'],
                ['METADATA CONTRACT: signatures verified', ''],
            ],
            'LifecycleTests': [
                ['Lifecycle assertions passed: 0'],
                ['Lifecycle assertions passed: -1'],
                ['Lifecycle assertions passed: unknown'],
                ['Lifecycle assertions passed: 4023', 'ERROR: failed'],
                ['METADATA CONTRACT: only metadata'],
            ],
            'ShopInformationTests': [
                ['Lifecycle assertions passed: 4023'],
                ['METADATA CONTRACT: metadata from another program', 'PASS: valid'],
            ],
        }
        for name, invalid_collections in cases.items():
            for evidence in invalid_collections:
                manifest = deepcopy(self.prd6)
                manifest['tests'][name] = evidence
                with self.subTest(name=name, evidence=evidence), self.assertRaises(RuntimeError):
                    preview.validate_preview_candidate(manifest, 'prd6')

    def test_prd6_exact_boundaries_cannot_inherit_confirmation_or_extra_claims(self):
        for boundary in ('candidate_validation', 'windows_validation', 'implementation_baseline'):
            for key in self.prd6[boundary]:
                for missing in (False, True):
                    manifest = deepcopy(self.prd6)
                    if missing:
                        del manifest[boundary][key]
                    else:
                        manifest[boundary][key] = 'candidate user-confirmed'
                    with self.subTest(boundary=boundary, key=key, missing=missing), self.assertRaises(RuntimeError):
                        preview.validate_preview_candidate(manifest, 'prd6')
            manifest = deepcopy(self.prd6)
            manifest[boundary]['extra_confirmation'] = True
            with self.subTest(boundary=boundary), self.assertRaises(RuntimeError):
                preview.validate_preview_candidate(manifest, 'prd6')
        for value in ('verified', None, {}, preview.PRD5_RUNTIME_BOUNDARY):
            manifest = deepcopy(self.prd6)
            manifest['runtime_equivalence'] = value
            with self.subTest(value=value), self.assertRaises(RuntimeError):
                preview.validate_preview_candidate(manifest, 'prd6')

    def test_prd6_baseline_hash_and_confirmation_are_fixed_and_not_v105(self):
        baseline = preview.PRD6_IMPLEMENTATION_BASELINE
        self.assertEqual(baseline['release'], 'v1.0.6')
        self.assertEqual(baseline['source_commit'], '0d38cc6ce0e33fecf3341eba6df12f96831788e2')
        self.assertEqual(baseline['public_release_dll']['sha256'],
                         '134ef356b512628e16795314a9308a91c5bb3c16d5c521aaa7a5e5bb0da198dc')
        self.assertEqual(baseline['confirmed_on'], '2026-10-21')
        self.assertEqual(baseline['windows_confirmation'], 'v1.0.6 Windows 확인을 완료했고 남은 문제가 없습니다.')
        for field, value in (('release', 'v1.0.5'), ('source_commit', '0'*40),
                             ('windows_confirmation', 'all individual scenarios passed')):
            manifest = deepcopy(self.prd6)
            manifest['implementation_baseline'][field] = value
            with self.subTest(field=field), self.assertRaises(RuntimeError):
                preview.validate_preview_candidate(manifest, 'prd6')
        for field, value in (('sha256', '0'*64), ('bytes', 654336.0)):
            manifest = deepcopy(self.prd6)
            manifest['implementation_baseline']['public_release_dll'][field] = value
            with self.subTest(field=field), self.assertRaises(RuntimeError):
                preview.validate_preview_candidate(manifest, 'prd6')

    def test_prd6_false_is_not_numeric_zero_and_completion_must_be_explicit(self):
        for claim in ('windows_runtime_executed', 'historical_full_runtime_equivalence_claimed',
                      'feature_or_issue_completion_claimed'):
            manifest = deepcopy(self.prd6)
            del manifest[claim]
            with self.subTest(claim=claim), self.assertRaises(RuntimeError):
                preview.validate_preview_candidate(manifest, 'prd6')
            for value in (True, 0, 0.0, None, 'false'):
                manifest = deepcopy(self.prd6)
                manifest[claim] = value
                with self.subTest(claim=claim, value=value), self.assertRaises(RuntimeError):
                    preview.validate_preview_candidate(manifest, 'prd6')
        for boundary, key in (('candidate_validation', 'historical_windows_confirmation_applies_to_this_candidate'),
                              ('windows_validation', 'new_release_dll_directly_windows_tested'),
                              ('implementation_baseline', 'environment_details_supplied'),
                              ('implementation_baseline', 'installed_dll_hash_resubmitted'),
                              ('implementation_baseline', 'applies_to_new_candidate')):
            manifest = deepcopy(self.prd6)
            manifest[boundary][key] = 0
            with self.subTest(boundary=boundary, key=key), self.assertRaises(RuntimeError):
                preview.validate_preview_candidate(manifest, 'prd6')

    def test_missing_validation_and_unknown_scope_are_rejected(self):
        for scope in ('prd2', 'prd4', 'prd5', 'prd6'):
            with self.subTest(scope=scope), self.assertRaises(RuntimeError):
                preview.validate_preview_candidate({}, scope)
        with self.assertRaises(RuntimeError):
            preview.validate_preview_candidate(self.prd4, 'unknown')


if __name__ == '__main__':
    unittest.main()
