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
    }
}
