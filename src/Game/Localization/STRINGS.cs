namespace ForbiddenTechnologyPack.Game {
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
}
