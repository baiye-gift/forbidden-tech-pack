// ONI localization uses a global STRINGS root. Vanilla code references this
// generated tree directly (BUILDINGS.PREFABS.*, RESEARCH.TECHS.*, etc.).
public static class STRINGS {
        public static class ELEMENTS {
            public static class BAIYEFORBIDDENPROTOMATTER {
                public static LocString NAME = "Proto-Matter";
                public static LocString DESC = "A dense, stable aggregate with a dark crust, violet grains and cyan inner strata. Produced by mass crushing or paid energy condensation; used for material compilation and controlled annihilation. Recoverable energy budget: up to 12 MJ/kg; condensation costs 18 MJ/kg. Store and convey it as a solid. The material itself is non-radioactive; radiation occurs inside the active reactor.";
            }
        }

        public static class BUILDINGS {
            public static class PREFABS {
                public static class BAIYEPROTOANNIHILATIONREACTOR {
                    public static LocString NAME = "Proto-Matter Annihilation Reactor";
                    public static LocString DESC = "Experimental confinement converts Proto-Matter rest mass into usable energy. The colony must supply fuel, cooling and maintenance for this extreme engineering process.";
                    public static LocString EFFECT = "Consumes 0.02 kg/s Proto-Matter. Gross power 48 kW; operation 8 kW; net 40 kW. Transfers 200 kDTU/s to a liquid cooling loop. Cold ignition needs 600 kJ of paid power. Radiation shielding is needed in radiation-enabled colonies.";
                }
                public static class BAIYEENERGYMATTERCONDENSER {
                    public static LocString NAME = "Energy-Matter Condenser";
                    public static LocString DESC = "Condenses purchased electrical energy into physical Proto-Matter, with irreversible conversion losses.";
                    public static LocString EFFECT = "Consumes 36 kW. Each 18 MJ of paid work produces 1 kg Proto-Matter in 500 seconds and transfers 6 MJ of heat to coolant. Stores outputs for manual or conveyor pickup.";
                }
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

        public static class RESEARCH {
            public static class TECHS {
                public static class BAIYEFORBIDDENMASSENERGYENGINEERING {
                    public static LocString NAME = "Forbidden Mass-Energy Engineering";
                    public static LocString DESC = "Push confinement and mass-energy conversion beyond known industrial limits. Requires fuel, paid power, thermal control and maintenance.";
                    public static LocString SEARCH_TERMS = "annihilation energy matter condenser proto reactor";
                }
                public static class BAIYEFORBIDDENMATTERENGINEERING {
                    public static LocString NAME = "Forbidden Matter Engineering";
                    public static LocString DESC = "Reverse-engineer matter itself and build machines that analyze, deconstruct, and compile solid materials.";
                    public static LocString SEARCH_TERMS = "forbidden matter proto-matter analyzer crusher compiler";
                }
            }
        }

        public static class UI {
            public static class MASSENERGY {
                public static LocString FUEL_EMPTY = "Waiting for Proto-Matter fuel";
                public static LocString POWER_FULL = "Waiting for generated energy to leave";
                public static LocString IGNITING = "Charging confinement: {0:0}%";
                public static LocString REACTING = "Controlled Proto-Matter annihilation";
                public static LocString READY = "Confinement ready; waiting for paid operating power";
                public static LocString CONDENSING = "Condensing Proto-Matter: {0:0}%";
                public static LocString REACTOR_DETAIL = "Fuel: {0:0.###} / 120 kg. Ignition paid: {1:0.#} / 600 kJ. Generator buffer: {2:0.#} / 96 kJ. Lifetime annihilated: {3:0.###} kg; gross generation: {4:0.###} MJ. Connect a fuel rail or allow deliveries, a powered wire for ignition, and an external liquid heat sink. Full rates: 48 kW gross, 8 kW operating, 40 kW net, 200 kDTU/s coolant heat. Stored ignition survives pauses and saves. Native overheating damage requires repair.";
                public static LocString CONDENSER_DETAIL = "Current order paid: {0:0.###} / 18 MJ. Output: {1:0.###} / 20 kg. Full rate: 36 kW, 1 kg per 500 seconds, 12 kDTU/s coolant heat. Queue an order or an infinite queue, supply power/coolant, and remove completed products. Cancellation returns no spent electricity and does not undo heat.";
                public static LocString CONDENSE_RECIPE = "Condense 1 kg of physical Proto-Matter using 18 MJ of paid electrical work. Transfers 6 MJ to coolant, with up to 12 MJ/kg recoverable material energy. Even full annihilation heat recovery cannot create a gain cycle.";
            }
            public static class MATTER {
                public static LocString ANALYZE = "Analyze {0} to unlock its compilation recipe. The {1} kg sample is {2}.";
                public static LocString CRUSH = "Convert {0} kg of {1} into {2} kg of Proto-Matter. Loss: {3} kg per batch. Automated processing; supply power and remove outputs.";
                public static LocString COMPILE = "Compile {0} kg of {1} from {2} kg of Proto-Matter. Requires paid power and a liquid coolant loop; heat must leave the colony's loop through an external heat sink.";
                public static LocString CONSUMED = "consumed";
                public static LocString RETURNED = "returned after analysis";
                public static LocString STATE = "{State}";
                public static LocString DETAIL = "{Detail}";
                public static LocString CHOOSE = "Choose a material and queue an order";
                public static LocString WORKING = "Processing {0}: {1:0}%";
                public static LocString WAITING_OPERATOR = "Waiting for an operator";
                public static LocString NO_UNLOCKS = "No materials analyzed yet";
                public static LocString UNLOCK_HELP = "Collect 10 kg of an eligible solid and complete an order in the Matter Analyzer first.";
                public static LocString ALL_ANALYZED = "No remaining eligible samples";
                public static LocString SAMPLE_HELP = "All currently eligible solids are analyzed, or disabled by the selected preset.";
                public static LocString MISSING = "Waiting for {0}";
                public static LocString INGREDIENT_HELP = "Input: {0:0.##} / {1:0.##} kg of {2}. Supply it through manual delivery, an auto-sweeper, or the building's input rail where supported.";
                public static LocString OUTPUT_FULL = "Waiting for output space";
                public static LocString OUTPUT_HELP = "Output: {0:0.##} / {1:0.##} kg. Remove the stored products manually or connect an unblocked output rail.";
                public static LocString NOT_OPERATIONAL = "Waiting for the building to become operational";
                public static LocString OPERATION_HELP = "Check the native power, automation, flooded, buried, or damaged status above. Paused inputs and completed products stay in storage.";
                public static LocString COOLANT_EMPTY = "Waiting for liquid coolant";
                public static LocString COOLANT_DRAIN = "Waiting for coolant to leave";
                public static LocString COOLANT_HOT = "Coolant is too hot";
                public static LocString COOLANT_HELP = "Coolant: {0:0.##} / 10 kg, {1}. Connect liquid input and output. A heated packet is held until the outlet accepts it; unsafe unheated coolant is returned unchanged.";
                public static LocString HEATED_BUFFER = "Return buffer: {0:0.##} / 10 kg. This liquid keeps its heat and germs until the outlet accepts it.";
                public static LocString ACCOUNTS = "Batch: {0:0.##} kg input → {1:0.##} kg output; base processing time {2:0.##} s. Current input {3:0.##} kg, reserved input {4:0.##} kg, output {5:0.##} kg. Native inventory entries show actual temperatures and germs.";
            }
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
