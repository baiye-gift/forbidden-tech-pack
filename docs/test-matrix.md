# Forbidden Technology Pack Phase 2 Test Matrix

Automated checks may be recorded as PASS only when the corresponding command has been run successfully. Full in-game cases remain **PENDING** until they are executed in Oxygen Not Included and supported by a Player.log excerpt or an explicit observation note. A limited startup smoke test does not complete a content-mode row.

## Automated gates

| Check | Status | Evidence |
|---|---|---|
| Core / source / runtime contract tests | PASS | `.\test.ps1 -Suite All -GamePath '<GamePath>'` — **223 tests passed on 2026-09-18** |
| KAnim compilation | PASS | The All-suite build path successfully restored encoded Reconstructor sprites and rebuilt the current animation set before package build |
| Game DLL/package build | PASS | The 2026-09-18 All-suite run wrote `dist\ForbiddenTechnologyPack\ForbiddenTechnologyPack.dll` and completed package build against the local ONI installation |
| Reconstructor safe-removal runtime fixture | PASS | `be657795` fixture update reverified by the same 223-test All-suite run |
| Package structure verification | PASS | Covered by the current build/test packaging path; package generation completed without structure failure |
| Local installation of current Phase 2 build | PENDING | Previous Phase 1 local install passed, but the current Reconstructor build has not yet been copied into the local mod directory and launched |
| Release ZIP verification for current Phase 2 build | PENDING | Phase 2 is not yet at release sign-off; in-game validation is still pending |

## Recorded smoke test

| Check | Status | Evidence |
|---|---|---|
| Steam main-menu startup (Phase 1 baseline) | PASS | On 2026-09-17, the Spaced Out!-enabled mode loaded the DLL and animation group; the main menu remained responsive and Player.log contained no fatal error attributable to this Mod. This predates the Matter Reconstructor and does not validate current Phase 2 gameplay. |
| Steam main-menu startup (current Phase 2) | PENDING | Install the current build and fully restart ONI |

## Content mode

| Mode | Main menu | New colony | Existing colony | Research/build menu | Proto-Matter / Reconstructor | Save/reload | Player.log | Status |
|---|---|---|---|---|---|---|---|---|
| Base game only | — | — | — | — | — | — | — | PENDING |
| Spaced Out! only / permitted DLC mode | — | — | — | — | — | — | — | PENDING |
| Frosty Planet Pack / permitted DLC mode | — | — | — | — | — | — | — | PENDING |
| Bionic Booster Pack / permitted DLC mode | — | — | — | — | — | — | — | PENDING |
| All supported DLC enabled | — | — | — | — | — | — | — | PENDING |

Record the exact game build and active content IDs for every executed row. A missing-content exception is a failure, not a skip.

## Building behavior — Phase 1 baseline

| Case | Expected result | Status | Evidence / notes |
|---|---|---|---|
| Analyze a common solid | Compiler recipe appears after completion | PENDING | |
| Analyze the same solid again | Duplicate analysis recipe is absent | PENDING | |
| Crusher manual input | Correct Proto-Matter mass is produced | PENDING | |
| Crusher rail input/output | Conveyor flow works without invalid material acceptance | PENDING | Compiled predicate passed; native rail movement still requires in-game validation |
| Crusher blocked output | No input/output mass is lost | PENDING | |
| Compiler common/industrial/rare/endgame solids | Each unlocked tier completes with configured conversion math | PENDING | |
| Power loss during work | Batch pauses and resumes intact | PENDING | |
| Automation disable during work | Batch pauses and resumes intact | PENDING | |
| Near-transition coolant | Compiler pauses; coolant does not change phase or disappear | PENDING | |
| Compiler blocked output + save/reload | Exactly one completed result batch remains after unblock | PENDING | |
| Deconstruct each Phase 1 machine with partial storage | Original element, mass, temperature and disease data are returned | PENDING | |

## Matter Reconstructor behavior

| Case | Expected result | Status | Evidence / notes |
|---|---|---|---|
| Proto-Field Engineering research | Node is visible after Forbidden Matter Engineering and unlocks the Reconstructor | PENDING | |
| Refining menu entry and icon | Reconstructor is visible when enabled and uses its own icon, not a question mark | PENDING | |
| Building placement | 4×4 footprint, floor alignment and animation placement are correct | PENDING | |
| Unanalyzed target filtering | Materials not analyzed by the Analyzer do not appear as reconstruction targets | PENDING | |
| Analyze new target while idle | Reconstructor refreshes and exposes the newly legal recipe | PENDING | |
| Analyze new target during active work | Current work order/input is preserved; recipe refresh does not eat the batch | PENDING | |
| Reconstruction mass rule | 1000 kg substrate + configured Proto-Matter produces exactly 1000 kg target material | PENDING | Proto-Matter is a reality-lever cost, not product mass |
| Manual input | Duplicants can deliver required substrate and Proto-Matter normally | PENDING | |
| Solid rail input | Valid substrate/Proto-Matter can enter without accepting unrelated forbidden materials | PENDING | |
| Solid rail output | Completed product can leave via conveyor | PENDING | |
| Blocked output | Product/input mass is retained and production does not continue destructively | PENDING | |
| Power loss | Active reconstruction pauses and resumes intact | PENDING | |
| Automation red/green | Red pauses and green resumes without losing storage/order | PENDING | |
| Proto-Matter interference | Nearby interference pauses the Reconstructor and clearing all sources resumes it | PENDING | |
| Save/reload during work | Storage, queue and active state remain consistent | PENDING | |
| Deconstruct with partial storage | Stored ordinary materials/Proto-Matter are returned according to native rules | PENDING | |

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
| Disable Reconstructor | Hidden for new construction; existing instance/storage preserved | PENDING | |
| Restart after configuration change | Selected settings persist | PENDING | |
| DLC off/on around an unlocked material | Recipe hides/reappears while serialized unlock ID remains | PENDING | |

## Safe removal

| Case | Expected result | Status | Evidence / notes |
|---|---|---|---|
| Loose Proto-Matter | Converted to equal-mass Igneous Rock | PENDING | Runtime compiled/behavior contracts pass; still needs game execution |
| Stored Proto-Matter | Converted/returned without mass loss | PENDING | |
| Rail Proto-Matter | Converted with zero custom objects remaining | PENDING | |
| Crusher/Compiler Proto-Matter | Converted with storage safely returned | PENDING | |
| Reconstructor ordinary inputs + Proto-Matter | Inputs are returned and custom content is removed without loss | PENDING | Reconstructor is now included by runtime safe-removal contract |
| First + second confirmation | Destructive action requires both confirmations | PENDING | |
| Save under new name and reload | Colony opens without missing-element errors | PENDING | |
| Disable mod after successful cleanup | Prepared save loads without custom-content dependency | PENDING | |

## Performance soak

Run 10 Mass Crushers, 10 Matter Compilers and a representative Reconstructor load for 20 production cycles at speed 3.

| Observation | Requirement | Status | Evidence / notes |
|---|---|---|---|
| Repeating exceptions/warnings | None caused by normal operation | PENDING | |
| Per-frame full element/building scans | None | PENDING | Interference manager is designed as event-driven; verify under gameplay |
| Recipe queue growth | Bounded | PENDING | |
| Stored object growth | Bounded by logistics/output state | PENDING | |
| Catalog construction | Once per game load | PENDING | |
| Safe-removal object scans | Only after explicit confirmation | PENDING | |

## Release sign-off

Release remains **PENDING** until the required in-game rows above are executed for the intended supported content modes. The 223-test local All-suite result establishes `LOCAL_VERIFIED`, not `IN_GAME_VERIFIED`.
