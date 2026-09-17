// ONI localization uses a global STRINGS root. Vanilla code references this
// generated tree directly (BUILDINGS.PREFABS.*, RESEARCH.TECHS.*, etc.).
public static class STRINGS {
        public static class ELEMENTS {
            public static class BAIYEFORBIDDENPROTOMATTER {
                public static LocString NAME = "Proto-Matter";
                public static LocString DESC = "A compact, transportable substrate produced by forbidden matter processing.";
            }
        }

        public static class BUILDINGS {
            public static class PREFABS {
                public static class BAIYEMASSCRUSHER {
                    public static LocString NAME = "Mass Crusher";
                    public static LocString DESC = "Breaks solid materials into transportable Proto-Matter.";
                    public static LocString EFFECT = "Consumes solid materials and produces Proto-Matter. Supports solid conveyor rails.";

                    public static class LOGIC_PORT {
                        public static LocString NAME = "Enable Mass Crusher";
                        public static LocString ACTIVE = "When receiving a Green signal, the Mass Crusher may operate.";
                        public static LocString INACTIVE = "When receiving a Red signal, the Mass Crusher is disabled.";
                    }
                }

                public static class BAIYEMATTERANALYZER {
                    public static LocString NAME = "Matter Analyzer";
                    public static LocString DESC = "Studies a physical material sample to unlock forbidden matter recipes.";
                    public static LocString EFFECT = "Consumes a material sample, then unlocks it for Matter Compilation.";
                    public static LocString SAMPLE_ALREADY_ANALYZED = "This material has already been analyzed.";

                    public static class LOGIC_PORT {
                        public static LocString NAME = "Enable Analyzer";
                        public static LocString ACTIVE = "When receiving a Green signal, the Matter Analyzer may operate.";
                        public static LocString INACTIVE = "When receiving a Red signal, the Matter Analyzer is disabled.";
                    }
                }

                public static class BAIYEMATTERCOMPILER {
                    public static LocString NAME = "Matter Compiler";
                    public static LocString DESC = "Reassembles Proto-Matter into analyzed solid materials.";
                    public static LocString EFFECT = "Consumes Proto-Matter and a safe liquid coolant loop to compile unlocked materials.";

                    public static class LOGIC_PORT {
                        public static LocString NAME = "Enable Matter Compiler";
                        public static LocString ACTIVE = "When receiving a Green signal, the Matter Compiler may operate.";
                        public static LocString INACTIVE = "When receiving a Red signal, the Matter Compiler is disabled.";
                    }
                }

                public static class BAIYEMATTERRECONSTRUCTOR {
                    public static LocString NAME = "Matter Reconstructor";
                    public static LocString DESC = "Uses Proto-Matter to lever reality and rewrite an existing material substrate into an analyzed structure.";
                    public static LocString EFFECT = "Consumes a tier-matched reality substrate and Proto-Matter to produce an equal mass of an analyzed target material.";
                    public static LocString NO_RECIPES = "No analyzed reconstruction targets are available.";
                    public static LocString WAITING_SUBSTRATE = "Waiting for a compatible reality substrate.";
                    public static LocString WAITING_PROTO_MATTER = "Waiting for Proto-Matter.";
                    public static LocString OUTPUT_BLOCKED = "Reconstructed material output is blocked.";
                    public static LocString INTERFERED = "Proto-Matter interference has destabilized reality anchoring. Reconstruction is paused.";

                    public static class LOGIC_PORT {
                        public static LocString NAME = "Enable Matter Reconstructor";
                        public static LocString ACTIVE = "When receiving a Green signal, the Matter Reconstructor may operate.";
                        public static LocString INACTIVE = "When receiving a Red signal, the Matter Reconstructor is disabled.";
                    }
                }

                public static class BAIYEENTROPYFLUXDIVERTER {
                    public static LocString NAME = "Entropy Flux Diverter";
                    public static LocString DESC = "Uses a Proto-Matter field to establish an artificial thermodynamic coupling between two liquid streams.";
                    public static LocString EFFECT = "Moves equal thermal energy from a hotter liquid stream into a colder one while consuming a small amount of Proto-Matter. It does not delete heat.";
                    public static LocString WAITING_HOT = "Waiting for hot liquid.";
                    public static LocString WAITING_COLD = "Waiting for cold liquid.";
                    public static LocString WAITING_PROTO_MATTER = "Waiting for Proto-Matter.";
                    public static LocString NO_GRADIENT = "No usable thermal gradient is present.";
                    public static LocString PHASE_LIMITED = "Transfer is limited by a liquid phase boundary.";
                    public static LocString TRANSFERRING = "Redirecting entropy between the two liquid streams.";
                    public static LocString OUTPUT_BLOCKED = "A processed liquid output is blocked.";
                    public static LocString INTERFERED = "Proto-Matter interference has broken the coupling field. Transfer is paused.";

                    public static class LOGIC_PORT {
                        public static LocString NAME = "Enable Entropy Flux Diverter";
                        public static LocString ACTIVE = "When receiving a Green signal, the Entropy Flux Diverter may operate.";
                        public static LocString INACTIVE = "When receiving a Red signal, the Entropy Flux Diverter is disabled.";
                    }
                }

                public static class BAIYEMATTERANNIHILATIONREACTOR {
                    public static LocString NAME = "Matter Annihilation Reactor";
                    public static LocString DESC = "Confines Proto-Matter at the boundary of reality and deliberately relaxes its material anchor to release usable energy.";
                    public static LocString EFFECT = "Requires external power to establish confinement, then consumes Proto-Matter to generate large amounts of electricity and heat. Cooling or confinement failure can trigger localized Proto-Matter decoherence.";
                    public static LocString OFFLINE = "Reactor offline.";
                    public static LocString CHARGING = "Charging confinement field.";
                    public static LocString STABLE = "Stable annihilation reaction.";
                    public static LocString FLUCTUATING = "Constraint field fluctuating.";
                    public static LocString CRITICAL = "Proto-Matter confinement is at the decoherence threshold.";
                    public static LocString DECOHERED = "Proto-Matter decohered. Emergency lockout engaged.";
                    public static LocString LOCKOUT = "Cooling lockout active.";
                    public static LocString WAITING_PROTO_MATTER = "Waiting for Proto-Matter.";
                    public static LocString COOLING_INSUFFICIENT = "Coolant cannot safely absorb reactor heat.";
                    public static LocString POWER_INSUFFICIENT = "Constraint power is insufficient.";
                    public static LocString INTERFERED = "External Proto-Matter interference is destabilizing confinement.";

                    public static class LOGIC_PORT {
                        public static LocString NAME = "Enable Matter Annihilation Reactor";
                        public static LocString ACTIVE = "When receiving a Green signal, the reactor may charge or sustain confinement.";
                        public static LocString INACTIVE = "When receiving a Red signal, the reactor performs an orderly shutdown into cooling lockout.";
                    }
                }
            }
        }

        public static class BUILDING {
            public static class STATUSITEMS {
                public static class BAIYEMATTERCOMPILERUNSAFECOOLANT {
                    public static LocString NAME = "Waiting for Safe Coolant";
                    public static LocString TOOLTIP = "A liquid packet that can absorb the process heat and an open coolant output are required.";
                }
            }
        }

        public static class RESEARCH {
            public static class TECHS {
                public static class BAIYEFORBIDDENMATTERENGINEERING {
                    public static LocString NAME = "Forbidden Matter Engineering";
                    public static LocString DESC = "Reverse-engineer matter itself and build machines that analyze, deconstruct, and compile solid materials.";
                    public static LocString SEARCH_TERMS = "forbidden matter proto-matter analyzer crusher compiler";
                }

                public static class BAIYEFORBIDDENPROTOFIELDENGINEERING {
                    public static LocString NAME = "Proto-Matter Field Engineering";
                    public static LocString DESC = "Use Proto-Matter as a dimensional coupling medium to lever reality itself and drive advanced forbidden machinery.";
                    public static LocString SEARCH_TERMS = "proto-matter field dimensional coupling reconstruction reality reconstructor entropy diverter annihilation reactor forbidden";
                }
            }
        }

        public static class UI {
            public static class SAFEREMOVAL {
                public static LocString TITLE = "Safe Removal - Forbidden Technology Pack";
                public static LocString FIRST_WARNING = "This workflow permanently prepares the current colony for disabling this mod. It will stop the forbidden matter machines, return their stored inputs, and remove the custom buildings. Continue only after making a normal save first.";
                public static LocString SECOND_WARNING = "Final confirmation: convert every Proto-Matter object to Igneous Rock and stop/remove all forbidden matter buildings now? Completed ordinary outputs are preserved. This action cannot be undone except by reloading an earlier save.";
                public static LocString CONTINUE = "Continue";
                public static LocString CONVERT_AND_STOP = "Convert Proto-Matter and Stop Buildings";
                public static LocString CANCEL = "Cancel";
                public static LocString CLOSE = "Close";
                public static LocString NO_ACTIVE_GAME = "Load the colony you want to prepare before starting safe removal.";
                public static LocString SUCCESS = "Safe removal scan is complete. Converted {0} Proto-Matter objects ({1:0.##} kg), removed {2} custom buildings, and returned {3} stored input objects. Save the colony under a new name, reload that save once, verify it opens normally, and only then disable the mod.";
                public static LocString INCOMPLETE = "Safe removal is not complete. {0} custom objects remain. Keep the mod enabled, reload the colony if necessary, and run Safe Removal again. Do not disable the mod yet.";
            }
        }
}
