# Forbidden Technology Pack / 禁忌科技建筑包

Forbidden Technology Pack adds a configurable high-power solid-material processing chain to Oxygen Not Included. Materials can be analyzed, converted into transportable Proto-Matter, and later reconstructed through a coolant-gated compiler.

## Requirements

- Oxygen Not Included build 744825 or newer.
- Base game and supported DLC content modes are declared through `supportedContent: ALL`.
- The mod ships its required runtime code and does not require users to install PLib separately.

## Installation

Extract `ForbiddenTechnologyPack` into the game's local mods directory:

`Documents\Klei\OxygenNotIncluded\mods\local\ForbiddenTechnologyPack`

Restart the game after installing or updating the mod.

## Buildings

### Matter Analyzer

Consumes a physical solid sample to unlock that material for Matter Compiler recipes. Already analyzed materials are filtered from the available analysis recipes.

### Mass Crusher

Converts eligible solid materials into Proto-Matter while preserving the configured mass-conversion rules. It supports manual handling and solid conveyor logistics.

### Matter Compiler

Reconstructs unlocked solids from Proto-Matter. It requires a liquid coolant loop and pauses instead of consuming a batch when safe cooling or output capacity is unavailable.

## Configuration presets

Balanced, Strong, and Extreme presets provide increasing production strength. Custom settings are clamped to supported safety limits. Individual buildings can also be disabled for future construction without deleting existing instances or their storage.

## Safe Removal

Use **Safe Removal** before disabling the mod in an existing colony. The workflow uses two confirmations, returns stored inputs where applicable, converts remaining Proto-Matter to Igneous Rock, removes custom buildings, and reports anything that still blocks safe removal. Save under a new name and reload successfully before disabling the mod.

## Compatibility

The mod is designed to tolerate base-game-only and supported DLC combinations by filtering unavailable content instead of requiring every DLC. Existing unlock state is preserved when a DLC recipe becomes temporarily unavailable.

## Updates

Updates preserve published building and element IDs. Back up important colonies before installing a new version, and review the changelog for migration or compatibility notes.
