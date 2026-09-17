# Matter Annihilation Reactor Design

## Goal

Add the Phase-2 flagship **Matter Annihilation Reactor / 物质湮灭堆**. It converts Proto-Matter into high-density electrical output while imposing a real startup-power, cooling, heat and localized-decoherence engineering problem.

The machine is not a fusion reactor. Its toroidal geometry is a magnetic/reality-confinement system that maintains Proto-Matter at the boundary of the current material dimension and deliberately relaxes that anchor.

## Building Contract

- Stable ID: `BaiyeMatterAnnihilationReactor`
- Research: `BaiyeForbiddenProtoFieldEngineering`
- Category: Power
- Footprint: 7×6, floor-mounted, not rotatable in v1
- Construction: 1200 kg Refined Metal + 800 kg Steel + 400 kg Ceramic + 200 kg Glass
- External startup/constraint power: 12 kW × `PowerMultiplier`
- Stable gross generation: 40 kW
- Stable net generation after constraint demand: nominal 28 kW before grid losses/settings
- Stable process heat to coolant/building: 1.2 MDTU/s × `HeatMultiplier`
- Proto-Matter stable consumption: 0.2 kg/s × `CostMultiplier`
- Liquid coolant input/output: one 10 kg primary liquid loop
- Automation: standard operational input; red requests orderly stop
- Forbidden device: yes, but its own decoherence state machine remains authoritative

## No Black Start

The reactor cannot begin generating from an unpowered state. It must receive external grid power continuously during `Charging` for **30 seconds** to establish confinement.

Only after charging succeeds may it enter `Stable` and enable generation. Losing all external power while charging returns to `Offline`. Power instability during an active reaction contributes to reactor instability rather than granting free generation.

## Deterministic Core State Machine

States:

1. `Offline`
2. `Charging`
3. `Stable`
4. `Fluctuating`
5. `Critical`
6. `Decohered`
7. `CoolingLockout`

The controller persists the state plus state elapsed time. One-shot effects are keyed to state entry and must never replay after save/reload.

### Healthy path

`Offline -> Charging -> Stable`

Requirements for Stable:

- startup charge completed;
- Proto-Matter available;
- coolant available and below the safe inlet limit;
- automation enabled;
- no lockout.

### Degradation path

A stability fault is any of:

- coolant cannot absorb the requested process heat without crossing its liquid phase limit;
- coolant/input flow absent during an active reaction;
- required constraint power is unavailable;
- building temperature is above the configured stability threshold.

Transitions:

- `Stable` + fault for 5 s -> `Fluctuating`
- `Fluctuating` + healthy for 10 s -> `Stable`
- `Fluctuating` + fault for 15 s -> `Critical`
- `Critical` + healthy for 5 s -> `CoolingLockout` (successful emergency shutdown)
- `Critical` + fault for 10 s -> `Decohered`
- `Decohered` immediately applies one-shot consequences then -> `CoolingLockout`
- `CoolingLockout` -> `Offline` only after temperature/coolant safety conditions are restored and at least 30 s lockout has elapsed

Automation red from `Stable` requests an orderly shutdown into `CoolingLockout`; it does not intentionally force decoherence.

## Energy / Mass Rules

The controller consumes Proto-Matter only while a Stable reaction tick actually produces power.

The reactor never creates Proto-Matter or ordinary material. Stable production intentionally removes Proto-Matter mass from the current reality and converts that gameplay resource into energy/heat.

Generation is disabled outside Stable.

## Cooling

The controller uses the same conservative phase-margin concept as the Entropy Flux Diverter. Process heat is transferred into the coolant packet; coolant may not be silently pushed across its liquid phase boundary.

If there is insufficient thermal headroom, the reactor raises a stability fault rather than deleting excess heat.

## Decoherence Consequence

On first entry to `Decohered`, exactly once:

- lose **35%** of Proto-Matter currently committed/stored for reaction, clamped to available mass;
- produce a one-time heat pulse of **20,000,000 DTU per kg actually lost**, multiplied by `HeatMultiplier`;
- activate a localized Proto-Matter interference field with **radius 12 cells** for **60 seconds**;
- force the reactor into `CoolingLockout`;
- do not explode, permanently destroy itself, damage ordinary buildings, alter global automation, or patch the power grid.

Overlapping interference fields are source-ID based and clear independently.

## Interference Receiving Behavior

External Proto-Matter interference cannot delete reactor state. For v1:

- `Offline`, `Charging`, and `CoolingLockout`: no special escalation;
- `Stable`: external interference counts as a stability fault;
- `Fluctuating`/`Critical`: it remains one of the active fault causes.

The reactor does not use the generic forbidden-device Operational flag to bypass its state policy; the controller reads `ForbiddenTechDevice.IsInterfered` as a fault signal.

## Save / Reload

Serialize:

- reactor state;
- state elapsed seconds;
- charging progress;
- whether the current Decohered entry's one-shot consequence has fired;
- active reaction Proto-Matter bookkeeping required to avoid duplicate consumption.

On reload, if the reactor was already in `CoolingLockout`, no decoherence heat/interference may replay.

## Safe Removal

Safe removal:

- disables generation/processing first;
- deactivates any interference source owned by this reactor;
- returns coolant and unconsumed Proto-Matter using native storage semantics;
- removes the building without replaying decoherence;
- counts the reactor in remaining custom buildings.

## UI / Status

Localized states:

- Offline
- Charging confinement
- Stable annihilation
- Constraint fluctuation
- Decoherence critical
- Proto-Matter decohered
- Cooling lockout
- Missing Proto-Matter
- Cooling insufficient
- Constraint power insufficient
- Proto-Matter interference

## Visual Contract

KAnim: `baiye_matter_annihilation_reactor_kanim`

Visual language: large toroidal confinement ring, central empty/field volume, visible field-collapse state. Not a generic nuclear explosion.

Minimum animations: `idle`, `charge`, `working`, `unstable`, `decohere`, `locked`, `off`, `ui`.

## Testing

Core tests must cover every transition, recovery timing, loss clamp, finite/non-negative heat pulse, no loss above available batch, and one-shot event semantics.

Source contracts cover generator + power-input intent, coolant plumbing, ForbiddenTechDevice, interference emission, registration/research/localization/safe removal and asset source.

Real generation, power-grid interaction, coolant flow, save/reload in every state and forced decoherence remain game/Codex integration validation.