Matter Reconstructor source art represents a four-cell-wide reality anchoring chamber: a central substrate block held by directional anchors inside a pulsing Proto-Matter field.

The two PNG source sprites are stored losslessly as adjacent .png.b64 payloads so text-only repository tooling can preserve the binary art. test.ps1 and build-assets.ps1 call scripts/Restore-EncodedAssetSources.ps1 to materialize the PNG files before validation or KAnim compilation. The restored PNG files are generated source working copies and are git-ignored.

Generated packaging/anim and dist files must not be edited directly.
