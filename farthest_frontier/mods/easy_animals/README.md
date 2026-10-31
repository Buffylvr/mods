# Easy Animals

Steam Workshop: https://steamcommunity.com/sharedfiles/filedetails/?id=3830760426
(item 3830760426; installs as `Mods\EasyAnimals_FF.dll`)

Barns, goat barns, stables and chicken coops keep butchering when meat, hides or tallow
is full or at its production limit. The excess is discarded, and an in-game notification
reports it, so herds don't overpopulate and get sick.

## How it works

The game slaughters one animal at a time: `LivestockBuilding.CanSlaughterAnotherAnimal`
waits until the previous carcass is processed. Processing a carcass is a manufacturing
job, and `Building.CanProduceManufactureDefinition` refuses it when any output is at its
production limit or the building's output stock is full, so the carcass never gets
processed and slaughtering stops.

| File | What it patches |
| --- | --- |
| `SlaughterPatches.cs` | Allows carcass processing in livestock buildings regardless of output limits/space (`CanProduceManufactureDefinition`, `IsManuDefProductionLimited`); on completion keeps only what fits and discards the rest (`OnWorkOrderCompleted`, `ProduceItemsFromProducedItemDefinition`); turns off the barn's "cannot be butchered due to production limits" warning (`LivestockBuilding.OnProductionLimited` / `OnProductionLimitRemoved`). |
| `DiscardPopup.cs` | The discard notification, a copy of the game's "livestock animals were born" blurb with custom text (`BlurbContainer.GetLocalizedTitle` / `GetLocalizedDescription`). Discards merge into the popup while it is on screen. |
| `EasyAnimalsMod.cs` | Mod entry point and settings (`ShowPopups`, `LogDiscards`). |

Milk is deliberately not changed.

## Versions

- 0.1.0 - first Workshop release.
