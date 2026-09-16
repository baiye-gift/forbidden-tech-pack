using UnityEngine;

namespace ForbiddenTechnologyPack.Game.Buildings.Common {
    internal static class FabricatorSupport {
        internal static Storage CreateSealedStorage(GameObject gameObject, float capacityKg,
                bool showInUi) {
            var storage = gameObject.AddComponent<Storage>();
            storage.capacityKg = capacityKg;
            storage.showInUI = showInUi;
            storage.SetDefaultStoredItemModifiers(Storage.StandardSealedStorage);
            return storage;
        }
    }
}
