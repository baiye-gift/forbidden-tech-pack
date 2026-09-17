# Entropy Flux Diverter Design

## Goal

Add the Phase-2 **Entropy Flux Diverter / 熵流偏转器** as a forbidden-tech thermal machine that transfers heat between two liquid streams without deleting DTU.

Worldbuilding contract: Proto-Matter is consumed to temporarily rewrite the thermodynamic coupling between two otherwise unrelated liquid systems. Proto-Matter is not coolant and never becomes product mass.

## Building Contract

- Stable ID: `BaiyeEntropyFluxDiverter`
- Research: `BaiyeForbiddenProtoFieldEngineering`
- Category: Refining
- Footprint: 4×4, floor-mounted, not rotatable in v1
- Construction: 600 kg Refined Metal + 300 kg Ceramic + 100 kg Glass
- Power: 3600 W × `PowerMultiplier`
- Self heat: 12 kDTU/s × `HeatMultiplier`
- Automation: standard operational input
- Forbidden device: yes; Proto-Matter interference pauses transfer
- Copy settings / priority: yes

## Ports and Storage

The first version supports liquid only.

- Primary liquid input/output: **hot side**
- Secondary liquid input/output: **cold side**
- Each side has a sealed 10 kg storage buffer. A buffer is one transfer batch; while a processed batch waits on a blocked output, that side must not ingest another batch.
- Proto-Matter is held in a dedicated sealed solid storage. V1 permits duplicant delivery; no third solid conduit port is introduced just to automate catalyst delivery.

## Transfer Rule

A transfer cycle starts only when:

1. operational/powered/automation requirements are satisfied;
2. the building is not Proto-Matter-interfered;
3. both liquid buffers contain a valid liquid batch;
4. the hot batch temperature is greater than the cold batch temperature;
5. enough Proto-Matter exists for the actual transfer;
6. neither batch has already been processed and is waiting for output.

Core policy input:

- hot mass, SHC, temperature, low/high safe phase limits;
- cold mass, SHC, temperature, low/high safe phase limits;
- requested DTU.

The policy returns a single conservative transfer amount. The exact same DTU is removed from the hot side and added to the cold side.

### Phase guard

The policy keeps each side at least **1 K** inside the liquid phase boundaries supplied by the game adapter. If either side has no safe transfer room, transfer is zero.

The policy also prevents overshoot: the two streams may approach thermal equilibrium, but the transfer cannot invert hot/cold ordering in one cycle.

### Throughput and Proto-Matter cost

- Maximum requested transfer per completed batch: **4,000,000 DTU**.
- Proto-Matter cost: **0.025 kg per 1,000,000 DTU actually transferred**, multiplied by `CostMultiplier`.
- Zero transfer consumes zero Proto-Matter.
- A transfer smaller than the maximum consumes only the proportional cost.

The policy contains no Unity/ONI types.

## Output Blocking

After a successful transfer the two liquid buffers are marked processed and their dispensers are enabled. No further temperature mutation or Proto-Matter consumption occurs until the processed buffers have drained.

If either output is blocked, the already-processed batch remains stored. It is not processed again and no additional input is accepted into that full 10 kg buffer.

## Interference

`ForbiddenTechDevice` is attached to the building. If interfered before processing, no transfer occurs. If interference begins after a batch has already been processed, the building may finish dispensing that already-processed liquid, but it must not start another transfer.

## Save / Reload

The controller serializes whether the current pair of buffers has already been processed. Reloading a blocked processed batch must not apply heat transfer or Proto-Matter consumption again.

## Safe Removal

Safe removal must:

- disable further processing;
- return/drop both liquid buffers and Proto-Matter storage using native storage semantics;
- remove the building without deleting ordinary liquid mass;
- include the Diverter in remaining-custom-building counts.

## UI / Status

Required localized states:

- Waiting for hot liquid
- Waiting for cold liquid
- Waiting for Proto-Matter
- No thermal gradient
- Phase-boundary limited
- Transferring entropy
- Output blocked
- Proto-Matter interference

## Visual Contract

KAnim: `baiye_entropy_flux_diverter_kanim`

Visual language: two opposed liquid channels wrapped by a central Proto-Matter field coupler. The icon must read as directional heat redirection rather than a generic aquatuner.

Minimum animations: `idle`, `working`, `off`, `ui` (additional states are allowed).

## Testing

Portable/Core tests must cover:

- equal and opposite DTU accounting;
- mass identity preserved;
- equilibrium clamp;
- phase-boundary clamp;
- zero/invalid input;
- finite outputs;
- Proto-Matter cost proportional to actual DTU.

Game/source contracts must cover dual liquid ports, `ForbiddenTechDevice`, registration/research/localization/safe removal, and encoded asset presence.

Actual conduit flow, blocked output, temperature mutation, save/reload, and interference are `IN_GAME_VERIFIED` only after ONI testing.