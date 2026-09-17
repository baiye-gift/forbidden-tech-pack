using System;
using System.Collections.Generic;
using ForbiddenTechnologyPack.Core;
using ForbiddenTechnologyPack.Game.Elements;

namespace ForbiddenTechnologyPack.Game.Recipes {
    public static class ReconstructionRecipeAdapter {
        public static IReadOnlyList<string> SelectUnlockedIds(UnlockState unlockState) {
            if (unlockState == null) {
                throw new ArgumentNullException("unlockState");
            }

            var activeIds = new HashSet<string>(RecipeRegistry.ReconstructorRecipes.Keys,
                StringComparer.Ordinal);
            return CompilerRecipeFilter.SelectIds(ElementCatalogAdapter.Rules.Values,
                unlockState, activeIds);
        }
    }
}
