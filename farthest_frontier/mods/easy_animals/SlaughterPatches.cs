using System;
using HarmonyLib;

namespace EasyAnimals
{
    // How the game blocks butchering:
    // LivestockBuilding.CanSlaughterAnotherAnimal only slaughters when no carcass is waiting in the
    // building. Turning a carcass into meat/hides/tallow is a normal manufacturing job, and
    // Building.CanProduceManufactureDefinition refuses that job when ANY output is at its production
    // limit or the building's output stock is full. The carcass then sits forever, no more animals
    // are slaughtered, and the herd overpopulates and gets sick.
    //
    // The fix: always allow carcass processing in livestock buildings, and when the job finishes,
    // throw away whatever output has no room (or is at its production limit) instead of storing it.
    internal static class Slaughter
    {
        // The livestock building currently finishing a carcass-processing work order, if any.
        internal static Building completingBuilding;

        internal static bool IsSlaughterDef(Building building, ManufactureDefinition def)
        {
            if (!(building is LivestockBuilding) || def == null || def.sourceItems == null)
            {
                return false;
            }
            foreach (SourceItemDefinition source in def.sourceItems)
            {
                Item item = source.item;
                if (item is ItemHealthyCarcass || item is ItemUnhealthyCarcass || item is ItemSicklyCarcass)
                {
                    return true;
                }
            }
            return false;
        }

        // How many of the produced items fit; the rest are discarded.
        internal static int AmountToKeep(Building building, ProducedItemDefinition producedItemDef, int produced, out bool atProductionLimit)
        {
            ResourceManager resourceManager = UnitySingleton<GameManager>.Instance?.resourceManager;
            ResourceManager.ItemInfo itemInfo = resourceManager?.GetItemInfo(producedItemDef.item);
            atProductionLimit = itemInfo != null && itemInfo.isProductionLimited;
            if (atProductionLimit)
            {
                return 0;
            }
            if (building.storage == null)
            {
                return produced;
            }
            int room = producedItemDef.capacity - (int)building.storage.GetNumberOfUnreservedItems(producedItemDef.item);
            return Math.Max(0, Math.Min(produced, room));
        }
    }

    [HarmonyPatch(typeof(Building), nameof(Building.CanProduceManufactureDefinition))]
    internal static class CanProduceManufactureDefinitionPatch
    {
        private static void Postfix(Building __instance, ManufactureDefinition manufactureDef,
            ref int numCanProduce, ref bool cantProduceDueToSpace, ref bool __result)
        {
            if (__result || !Slaughter.IsSlaughterDef(__instance, manufactureDef))
            {
                return;
            }
            // Re-check the reasons unrelated to output space or production limits.
            if (!__instance.isWorkEnabled || __instance.manufacturingStorage == null || !__instance.GetManufactureDefStatus(manufactureDef))
            {
                return;
            }
            int num = int.MaxValue;
            foreach (SourceItemDefinition source in manufactureDef.sourceItems)
            {
                if (source.numSourceItemsNeeded > 0)
                {
                    num = Math.Min(num, (int)__instance.manufacturingStorage.GetNumberOfUnreservedItems(source.item) / source.numSourceItemsNeeded);
                }
            }
            if (num == int.MaxValue || num <= 0)
            {
                return;
            }
            numCanProduce = num;
            cantProduceDueToSpace = false;
            __result = true;
        }
    }

    // Used when the building picks which job to offer next.
    [HarmonyPatch(typeof(Building), "IsManuDefProductionLimited")]
    internal static class IsManuDefProductionLimitedPatch
    {
        private static bool Prefix(Building __instance, ManufactureDefinition manufactureDefinition, ref bool __result)
        {
            if (!Slaughter.IsSlaughterDef(__instance, manufactureDefinition))
            {
                return true;
            }
            __result = false;
            return false;
        }
    }

    // The barn's "Livestock cannot be butchered ... due to production limits" warning is driven only by
    // LivestockBuilding.OnProductionLimited / OnProductionLimitRemoved, which do nothing but set that UI flag.
    // Butchering is no longer blocked by limits, so keep the warning off.
    [HarmonyPatch(typeof(LivestockBuilding), nameof(LivestockBuilding.OnProductionLimited))]
    internal static class OnProductionLimitedPatch
    {
        private static bool Prefix(LivestockBuilding __instance)
        {
            ClearWarning(__instance);
            return false;
        }

        internal static void ClearWarning(LivestockBuilding building)
        {
            if (building.widgetBlackboard is LivestockBuildingWidgetBlackboard blackboard)
            {
                blackboard.livestockProductionLimited = false;
            }
        }
    }

    [HarmonyPatch(typeof(LivestockBuilding), nameof(LivestockBuilding.OnProductionLimitRemoved))]
    internal static class OnProductionLimitRemovedPatch
    {
        private static bool Prefix(LivestockBuilding __instance)
        {
            OnProductionLimitedPatch.ClearWarning(__instance);
            return false;
        }
    }

    [HarmonyPatch(typeof(Building), nameof(Building.OnWorkOrderCompleted))]
    internal static class OnWorkOrderCompletedPatch
    {
        private static void Prefix(Building __instance, ManufactureWorkOrder workOrder)
        {
            Slaughter.completingBuilding = Slaughter.IsSlaughterDef(__instance, workOrder?.manufactureDefinition) ? __instance : null;
        }

        private static void Finalizer()
        {
            Slaughter.completingBuilding = null;
        }
    }

    [HarmonyPatch(typeof(Building), "ProduceItemsFromProducedItemDefinition")]
    internal static class ProduceItemsFromProducedItemDefinitionPatch
    {
        private static readonly Action<Building, Item, uint> produceItems =
            AccessTools.MethodDelegate<Action<Building, Item, uint>>(AccessTools.Method(typeof(Building), "ProduceItems"));

        private static bool Prefix(Building __instance, ProducedItemDefinition producedItemDef)
        {
            if (Slaughter.completingBuilding != __instance || producedItemDef?.item == null)
            {
                return true;
            }
            int produced = Math.Max(0, producedItemDef.numItemsProduced);
            int keep = Slaughter.AmountToKeep(__instance, producedItemDef, produced, out bool atProductionLimit);
            if (keep >= produced)
            {
                return true;
            }
            if (keep > 0)
            {
                produceItems(__instance, producedItemDef.item, (uint)keep);
            }
            DiscardPopup.Show(__instance, producedItemDef.item, produced - keep, atProductionLimit);
            if (EasyAnimalsMod.LogDiscards.Value)
            {
                EasyAnimalsMod.Log.Msg($"{__instance.name}: discarded {produced - keep} {producedItemDef.item.name} (full or at production limit).");
            }
            return false;
        }
    }
}
