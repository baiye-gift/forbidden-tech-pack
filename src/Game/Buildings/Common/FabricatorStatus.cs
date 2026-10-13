using System;
using System.Linq;
using ForbiddenTechnologyPack.Game.Buildings.Analyzer;
using ForbiddenTechnologyPack.Game.Buildings.Compiler;

namespace ForbiddenTechnologyPack.Game.Buildings.Common {
    public sealed class FabricatorStatus : KMonoBehaviour, ISim1000ms {
        private static StatusItem summary;
        private ComplexFabricator fabricator;
        private Operational operational;
        private KSelectable selectable;
        private Guid statusHandle;
        private string state = "", detail = "";
        protected override void OnSpawn() {
            base.OnSpawn();
            fabricator = GetComponent<ComplexFabricator>();
            operational = GetComponent<Operational>();
            selectable = GetComponent<KSelectable>();
            if (summary == null) {
                summary = new StatusItem("BaiyeMatterProcessing", STRINGS.UI.MATTER.STATE,
                    STRINGS.UI.MATTER.DETAIL, "", StatusItem.IconType.Info,
                    NotificationType.Neutral, false, OverlayModes.None.ID);
                summary.resolveStringCallback = (text, data) => {
                    var target = data as FabricatorStatus;
                    return target == null ? "" : text.Replace("{State}", target.state).Replace("{Detail}", target.detail);
                };
            }
            Sim1000ms(1f);
            if (selectable != null) statusHandle = selectable.AddStatusItem(summary, this);
        }
        protected override void OnCleanUp() {
            if (selectable != null) selectable.RemoveStatusItem(statusHandle);
            base.OnCleanUp();
        }
        public void Sim1000ms(float dt) {
            if (fabricator == null) return;
            var recipes = fabricator.GetRecipes();
            if (recipes.Length == 0) {
                bool compiler = fabricator is MatterCompiler;
                state = compiler ? STRINGS.UI.MATTER.NO_UNLOCKS : STRINGS.UI.MATTER.ALL_ANALYZED;
                detail = compiler ? STRINGS.UI.MATTER.UNLOCK_HELP : STRINGS.UI.MATTER.SAMPLE_HELP;
                return;
            }
            var recipe = fabricator.CurrentWorkingOrder ?? recipes.FirstOrDefault(r => fabricator.GetRecipeQueueCount(r) != 0);
            if (recipe == null) {
                state = STRINGS.UI.MATTER.CHOOSE; detail = STRINGS.UI.MATTER.CHOOSE; return;
            }
            float output = fabricator is MatterAnalyzer ? 0f : recipe.results.Sum(r => r.amount);
            detail = string.Format(STRINGS.UI.MATTER.ACCOUNTS, recipe.ingredients.Sum(i => i.amount),
                output, recipe.time, fabricator.inStorage.MassStored(), fabricator.buildStorage.MassStored(), fabricator.outStorage.MassStored());
            if (output > fabricator.outStorage.RemainingCapacity()) {
                state = STRINGS.UI.MATTER.OUTPUT_FULL;
                detail = string.Format(STRINGS.UI.MATTER.OUTPUT_HELP, fabricator.outStorage.MassStored(), fabricator.outStorage.capacityKg);
                return;
            }
            var coolant = GetComponent<CoolantController>();
            if (coolant != null && !coolant.IsReady) {
                state = coolant.StateText; detail = coolant.DetailText; return;
            }
            if (operational != null && !operational.IsOperational) {
                state = STRINGS.UI.MATTER.NOT_OPERATIONAL;detail = STRINGS.UI.MATTER.OPERATION_HELP;return;
            }
            if (fabricator.CurrentWorkingOrder != null) {
                state = fabricator.WaitingForWorker ? (string)STRINGS.UI.MATTER.WAITING_OPERATOR :
                    string.Format(STRINGS.UI.MATTER.WORKING, MaterialName(fabricator is MatterCompiler
                        ? recipe.results[0].material : recipe.ingredients[0].material),
                        fabricator.OrderProgress * 100f);
                return;
            }
            var missing = recipe.ingredients.FirstOrDefault(i => fabricator.inStorage.GetAmountAvailable(i.material) + .001f < i.amount);
            if (missing != null) {
                var name = MaterialName(missing.material);
                state = string.Format(STRINGS.UI.MATTER.MISSING, name);
                detail = string.Format(STRINGS.UI.MATTER.INGREDIENT_HELP,
                    fabricator.inStorage.GetAmountAvailable(missing.material), missing.amount, name);
                return;
            }
            state = STRINGS.UI.MATTER.CHOOSE;
        }
        private static string MaterialName(Tag material) {
            var element = ElementLoader.FindElementByHash((SimHashes)Hash.SDBMLower(material.Name));
            return element == null ? material.Name : element.name;
        }
    }
}
