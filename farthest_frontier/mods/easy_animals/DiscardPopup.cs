using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace EasyAnimals
{
    // Shows discards as a normal in-game notification ("blurb"), styled like "livestock animals were born".
    // While the popup is on screen, further discards are merged into it instead of stacking new ones.
    internal static class DiscardPopup
    {
        private static UIBlurbDisplay display;
        private static BlurbDefinition definition;

        internal static void Show(Building building, Item item, int count, bool atProductionLimit)
        {
            if (!EasyAnimalsMod.ShowPopups.Value || count <= 0)
            {
                return;
            }
            GameManager gameManager = UnitySingleton<GameManager>.Instance;
            if (gameManager == null)
            {
                return;
            }
            if (display == null)
            {
                display = Object.FindObjectOfType<UIBlurbDisplay>();
                definition = null;
            }
            if (display == null)
            {
                return;
            }
            if (definition == null)
            {
                // A private copy of the game's definition: same look and icon, but never consolidated with real birth popups.
                definition = Object.Instantiate(gameManager.assetMapManager.uiAssetMap.livestockAnimalBornBlurb);
            }
            display.InstantiateNewBlurb(new DiscardBlurbContainer(building, item, count, atProductionLimit, definition, display.findObjectModule));
        }
    }

    internal class DiscardBlurbContainer : BuildingBlurbContainer
    {
        private readonly bool atProductionLimit;
        private readonly Dictionary<Item, int> discarded = new Dictionary<Item, int>();

        public DiscardBlurbContainer(Building building, Item item, int count, bool atProductionLimit, BlurbDefinition definition, GameObject module)
            : base(building, BlurbType.None, BlurbConsolidationType.COMBINE, "EasyAnimals.Discard", definition, null, module)
        {
            this.atProductionLimit = atProductionLimit;
            discarded[item] = count;
        }

        public override bool Consolidate(BlurbContainer other)
        {
            if (!(other is DiscardBlurbContainer otherDiscard) || otherDiscard.atProductionLimit != atProductionLimit)
            {
                return false;
            }
            foreach (KeyValuePair<Item, int> entry in otherDiscard.discarded)
            {
                discarded.TryGetValue(entry.Key, out int existing);
                discarded[entry.Key] = existing + entry.Value;
            }
            // Keep every building so clicking the popup cycles through them.
            if (buildingList == null)
            {
                buildingList = new List<Building>();
                if (singleBuilding != null)
                {
                    buildingList.Add(singleBuilding);
                    singleBuilding = null;
                }
            }
            Building otherBuilding = otherDiscard.singleBuilding;
            if (otherBuilding != null && !buildingList.Contains(otherBuilding))
            {
                buildingList.Add(otherBuilding);
            }
            other.successfullyConsolidated = true;
            return true;
        }

        internal string Title => atProductionLimit ? "Production Limit Reached" : "Storage Full";

        internal string Description
        {
            get
            {
                LocalizationManager localizationManager = UnitySingleton<GameManager>.Instance?.localizationManager;
                List<string> parts = discarded.Select(entry => $"{entry.Value:N0} {ItemName(localizationManager, entry.Key, entry.Value)}").ToList();
                string items = parts.Count <= 1 ? parts.FirstOrDefault() : string.Join(", ", parts.Take(parts.Count - 1)) + " and " + parts.Last();
                string who = numBuildings > 1 ? "Butchers have" : "A butcher has";
                return $"{who} discarded {items}.";
            }
        }

        private static string ItemName(LocalizationManager localizationManager, Item item, int count)
        {
            string tag = count == 1 ? item.descriptionSingularLocTag : item.descriptionLocTag;
            return localizationManager != null && !string.IsNullOrEmpty(tag) ? localizationManager.Localize(tag) : item.name;
        }
    }

    // Blurb text normally comes from localization tags; supply ours directly.
    [HarmonyPatch(typeof(BlurbContainer), nameof(BlurbContainer.GetLocalizedTitle))]
    internal static class GetLocalizedTitlePatch
    {
        private static bool Prefix(BlurbContainer __instance, ref string __result)
        {
            if (!(__instance is DiscardBlurbContainer discard))
            {
                return true;
            }
            __result = discard.Title;
            return false;
        }
    }

    [HarmonyPatch(typeof(BlurbContainer), nameof(BlurbContainer.GetLocalizedDescription))]
    internal static class GetLocalizedDescriptionPatch
    {
        private static bool Prefix(BlurbContainer __instance, ref string __result)
        {
            if (!(__instance is DiscardBlurbContainer discard))
            {
                return true;
            }
            __result = discard.Description;
            return false;
        }
    }
}
