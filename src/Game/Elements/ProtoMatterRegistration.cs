using System;
using System.Collections;
using System.Collections.Generic;
using ForbiddenTechnologyPack.Core;
using HarmonyLib;
using UnityEngine;

namespace ForbiddenTechnologyPack.Game.Elements {
    public static class ProtoMatterRegistration {
        private const string SubstanceKey = "baiyeforbiddenprotomatter";

        public static readonly SimHashes Hash = (SimHashes)global::Hash.SDBMLower(ModIdentity.ProtoMatterId);
        public static readonly Tag Tag = new Tag(ModIdentity.ProtoMatterId);

        public static void RegisterSubstance(ref Hashtable substanceList,
                Dictionary<string, SubstanceTable> substanceTablesByDlc) {
            if (substanceList == null) {
                substanceList = new Hashtable();
            }
            if (substanceList.ContainsKey(SubstanceKey)) {
                return;
            }

            SubstanceTable vanillaTable;
            if (substanceTablesByDlc == null ||
                    !substanceTablesByDlc.TryGetValue(DlcManager.VANILLA_ID, out vanillaTable)) {
                Debug.LogWarning("[ForbiddenTechnologyPack] Vanilla substance table was unavailable; Proto-Matter was not registered.");
                return;
            }

            var purple = new Color32(177, 77, 255, 255);
            var substance = ModUtil.CreateSubstance(SubstanceKey, Element.State.Solid,
                Assets.GetAnim(new HashedString("tungsten_kanim")), vanillaTable.solidMaterial,
                purple, purple, purple);
            substanceList.Add(SubstanceKey, substance);
            vanillaTable.GetList().Add(substance);
        }
    }

    [HarmonyPatch(typeof(ElementLoader), "Load")]
    public static class ProtoMatterElementLoaderPatch {
        [HarmonyPrefix]
        public static void Prefix(ref Hashtable substanceList,
                Dictionary<string, SubstanceTable> substanceTablesByDlc) {
            ProtoMatterRegistration.RegisterSubstance(ref substanceList, substanceTablesByDlc);
        }
    }
}
