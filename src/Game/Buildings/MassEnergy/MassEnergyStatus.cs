using System;
namespace ForbiddenTechnologyPack.Game.Buildings.MassEnergy {
    public sealed class MassEnergyStatus : KMonoBehaviour, ISim1000ms {
        private static StatusItem summary;
        private KSelectable selectable;
        private Guid handle;
        private ProtoAnnihilationReactor reactor;
        private EnergyMatterCondenser condenser;
        private string state = "", detail = "";
        protected override void OnSpawn() {
            base.OnSpawn(); reactor = GetComponent<ProtoAnnihilationReactor>(); condenser = GetComponent<EnergyMatterCondenser>();
            selectable = GetComponent<KSelectable>();
            if (summary == null) {
                summary = new StatusItem("BaiyeMassEnergyProcess", STRINGS.UI.MATTER.STATE, STRINGS.UI.MATTER.DETAIL,
                    "", StatusItem.IconType.Info, NotificationType.Neutral, false, OverlayModes.None.ID);
                summary.resolveStringCallback = (text, data) => { var owner = data as MassEnergyStatus;
                    return owner == null ? "" : text.Replace("{State}", owner.state).Replace("{Detail}", owner.detail); };
            }
            Sim1000ms(1); if (selectable != null) handle = selectable.AddStatusItem(summary, this);
        }
        public void Sim1000ms(float dt) {
            state = reactor != null ? reactor.StateText : condenser != null ? condenser.StateText : "";
            detail = reactor != null ? reactor.DetailText : condenser != null ? condenser.DetailText : "";
        }
        protected override void OnCleanUp() { if (selectable != null) selectable.RemoveStatusItem(handle); base.OnCleanUp(); }
    }
}
