# Forbidden Technology Pack 0.1.0 Test Matrix

Automated checks may be recorded as PASS only when the corresponding command has been run successfully. Full in-game cases remain **PENDING** until they are executed in Oxygen Not Included and supported by a Player.log excerpt or an explicit observation note. A limited startup smoke test does not complete a content-mode row.

## Automated gates

| Check | Status | Evidence |
|---|---|---|
| Core and contract tests | PASS | `.\test.ps1 -Suite All -GamePath '<GamePath>'` — 152 tests passed on 2026-09-17 |
| Original KAnim compilation | PASS | `build-assets.ps1` generated all four KAnim triplets |
| Game DLL/package build | PASS | `build.ps1` completed against local ONI installation; deprecation warnings remain in explicit safe-removal scans |
| Package structure verification | PASS | `verify-package.ps1` passed after build |
| Local installation | PASS | `install.ps1` verified the package and installed only to `mods\\local\\ForbiddenTechnologyPack` |
| Release ZIP verification | PASS | `pack-release.ps1` rebuilt, SHA256-hashed, extracted, and reverified `ForbiddenTechnologyPack-0.1.0.zip` |

## Recorded smoke test

| Check | Status | Evidence |
|---|---|---|
| Steam main-menu startup | PASS | On 2026-09-17, the current Spaced Out!-enabled mode loaded the DLL and Animation group; the main menu remained responsive and Player.log contained no fatal error attributable to this Mod. This does not validate colony gameplay or other content modes. |

## Content mode

| Mode | Main menu | New colony | Existing colony | Research/build menu | Proto-Matter | Save/reload | Player.log | Status |
|---|---|---|---|---|---|---|---|---|
| Base game only | — | — | — | — | — | — | — | PENDING |
| Spaced Out! only / permitted DLC mode | — | — | — | — | — | — | — | PENDING |
| Frosty Planet Pack / permitted DLC mode | — | — | — | — | — | — | — | PENDING |
| Bionic Booster Pack / permitted DLC mode | — | — | — | — | — | — | — | PENDING |
| All supported DLC enabled | — | — | — | — | — | — | — | PENDING |

Record the exact game build and active content IDs for every executed row. A missing-content exception is a failure, not a skip.

## Building behavior

| Case | Expected result | Status | Evidence / notes |
|---|---|---|---|
| Analyze a common solid | Compiler recipe appears after completion | PENDING | |
| Analyze the same solid again | Duplicate analysis recipe is absent | PENDING | |
| Crusher manual input | Correct Proto-Matter mass is produced | PENDING | |
| Crusher rail input/output | Conveyor flow works without invalid material acceptance | PENDING | |
| Crusher blocked output | No input/output mass is lost | PENDING | |
| Compiler common/industrial/rare/endgame solids | Each unlocked tier completes with configured conversion math | PENDING | |
| Power loss during work | Batch pauses and resumes intact | PENDING | |
| Automation disable during work | Batch pauses and resumes intact | PENDING | |
| Near-transition coolant | Compiler pauses; coolant does not change phase or disappear | PENDING | |
| Compiler blocked output + save/reload | Exactly one completed result batch remains after unblock | PENDING | |
| Deconstruct each machine with partial storage | Original element, mass, temperature and disease data are returned | PENDING | |

## Configuration and persistence

| Case | Expected result | Status | Evidence / notes |
|---|---|---|---|
| Balanced preset | Values and behavior match preset | PENDING | |
| Strong preset | Values and behavior match preset | PENDING | |
| Extreme preset | Values and behavior match preset | PENDING | |
| Out-of-range Custom values | Values clamp to supported limits | PENDING | |
| Disable Analyzer | Hidden for new construction; existing instance/storage preserved | PENDING | |
| Disable Crusher | Hidden for new construction; existing instance/storage preserved | PENDING | |
| Disable Compiler | Hidden for new construction; existing instance/storage preserved | PENDING | |
| Restart after configuration change | Selected settings persist | PENDING | |
| DLC off/on around an unlocked material | Recipe hides/reappears while serialized unlock ID remains | PENDING | |

## Safe removal

| Case | Expected result | Status | Evidence / notes |
|---|---|---|---|
| Loose Proto-Matter | Converted to equal-mass Igneous Rock | PENDING | |
| Stored Proto-Matter | Converted/returned without mass loss | PENDING | |
| Rail Proto-Matter | Converted with zero custom objects remaining | PENDING | |
| Crusher/Compiler Proto-Matter | Converted with storage safely returned | PENDING | |
| First + second confirmation | Destructive action requires both confirmations | PENDING | |
| Save under new name and reload | Colony opens without missing-element errors | PENDING | |
| Disable mod after successful cleanup | Prepared save loads without custom-content dependency | PENDING | |

## Performance soak

Run 10 Mass Crushers and 10 Matter Compilers for 20 production cycles at speed 3.

| Observation | Requirement | Status | Evidence / notes |
|---|---|---|---|
| Repeating exceptions/warnings | None caused by normal operation | PENDING | |
| Per-frame full element scans | None | PENDING | |
| Recipe queue growth | Bounded | PENDING | |
| Stored object growth | Bounded by logistics/output state | PENDING | |
| Catalog construction | Once per game load | PENDING | |
| Safe-removal object scans | Only after explicit confirmation | PENDING | |

## Release sign-off

Release remains **PENDING** until all required in-game rows above are executed for the intended supported content modes. Automated gates alone are not sufficient for publication.
