"""PRD 6 local-candidate evidence, separate from historical release policy.

The user's v1.0.6 confirmation is an implementation prerequisite only. Nothing
here marks the new DLL tested in Windows or the feature/issue complete.
"""
PRD6_VALIDATION = {
    'scope': 'PRD 6 map, one-shot, actual branch and shop information local candidate',
    'windows_runtime': 'not-run; maintainer confirmation required',
    'real_lua_comparison': 'PRD 2/5 analysis and selection preserved; PRD 6 read-only information boundaries tested separately',
    'final_trace_limits': 'PRD 2 limits preserved; unsupported or dynamic information remains unknown',
    'historical_windows_confirmation_applies_to_this_candidate': False,
}
WINDOWS_VALIDATION = {
    'status': 'pending',
    'new_release_dll_directly_windows_tested': False,
    'confirmation_source': None,
    'environment_note': 'PRD 6 candidate has not been run in Windows; user-confirmed v1.0.6 is an implementation baseline only.',
}
RUNTIME_BOUNDARY = (
    'PRD 6 adds read-only map, one-shot, actual branch and shop information. '
    'User-confirmed v1.0.6 is the implementation baseline, not Windows validation '
    'of this candidate. No historical full runtime equivalence is claimed.')
IMPLEMENTATION_BASELINE = {
    'release': 'v1.0.6',
    'source_commit': '0d38cc6ce0e33fecf3341eba6df12f96831788e2',
    'public_release_dll': {
        'bytes': 654336,
        'sha256': '134ef356b512628e16795314a9308a91c5bb3c16d5c521aaa7a5e5bb0da198dc',
    },
    'working_head_at_start': '375c39715c91d9a2bcdc629dbfe8a88ac0ff127a',
    'windows_validation_status': 'user-confirmed',
    'confirmed_on': '2026-10-21',
    'timezone': 'Asia/Seoul',
    'confirmation_source': 'User statement in this task conversation',
    'windows_confirmation': 'v1.0.6 Windows 확인을 완료했고 남은 문제가 없습니다.',
    'confirmation_scope': 'Version-wide user confirmation; no per-scenario or environment detail supplied.',
    'installed_dll_hash_resubmitted': False,
    'environment_details_supplied': False,
    'applies_to_new_candidate': False,
}
REQUIRED_TESTS = frozenset({
    'LogRefreshTests', 'LiveLogEngineTests', 'LifecycleTests',
    'LiveLogIntegrationTests', 'RealGameApiTests', 'GamepadTests',
    'SourceBehaviorTests', 'PreviewIntegrationTests', 'LogUiModelTests',
    'LogUiWiringTests', 'LogUiKeyboardTests', 'LogOpenerIntegrationTests',
    'EventSelectionRuntimeTests', 'EventPopupTests',
    'MapInformationTests', 'ShopInformationTests', 'BranchHistoryTests',
    'InformationRuntimeTests',
})
