# PlanetCrafterMods

## Mods
- [(QoL) Auto-Logistics](#qol-auto-logistics)
- [(Feat) Portal Travel](#feat-portal-travel)
- [(Feat) Space Station](#feat-space-station)

## Scripts
- [merge](#merge)

## (QoL) Auto-Logistics

Copy and paste (drone) logistic settings and automatically supply generated items.

### Features:
- Copy and paste:
  - Copy by holding C (default key, config: 'keyCopy') while closing the logistics menu or when selecting an item group
  - Paste by holding V (default key, config: 'keyPaste') while opening an inventory / a machine
  - Press the copy / paste button in the logistics menu (restriction: does not work with ore/gas extractors, harvesters etc.) (new v1.0.12.0)
  - Supported settings:
    - Supply settings
    - Demand settings
    - Priority settings
    - Selected item group (in e.g. T3 Ore Extractor, T2 Gas Extractor, Harvesting Robot and Planetary Delivery Depot)
      - Note: Does not paste full logistic settings. Acts as if the group was selected.
    - Auto-Launch (in Trade Space Rocket and Interplanetary Exchange Shuttle) and auto-destroy settings (in Shredder Machine)
    - Selected Planet (in Interplanetary Exchange Shuttle)
    - Text (of container)
  - Copied settings are shown in the upper-left corner in the pinned recipes list (works without pin-chip)
    - Note: Logistic settings aren't necessarily deleted/forgotten if they aren't shown in the upper-left corner anymore
  - Switch between per-machine/crate logistic setting copies and universally pasteable copy (config: copyLogisticsPerGroup)
    - Per-machine logistic setting copies allow a copied setting per storage object. 
      - E.g. T2 Locker and Trade Space Rocket have their own copied setting entry at the same time, but they can't be inserted into each others machine's logistic settings.
    - Universal copy allows to paste T2 Locker Storage logistic settings into Storage Crate logistic settings, 
          but also allows to paste Trade Space Rocket logistic settings into Shredder Machine logistic settings. Be careful.

- Automatically supply item groups for generators:
  - When placing a generating machine, it automatically selects it's produced item groups as supplied items (config: enableAddLogistics)
    Examples:
    - T2 Beehive automatically supplies Bee Larva and Honey
    - Lake Water Collector automatically supplies Water Bottle
    - ...
  - Shredder Machine is set to priority 'Lowest' / -3 (config: enableAddLogistics)
  - Auto-supply item group when selecting an item group to produce (in e.g. T3 Ore Extractor, ...)
  - Auto-supply crushed ingredients in the output inventory when selecting an item type to be demanded by the input inventory. (Ore Crusher and T2 Recycling Machine)
    - Example: demand Uraninite -> supply Super Alloy, Iridium, Uranium, Iron and Dolomite
    - Change config 'clearOutputOnInputChange' to 'false' to select more than one demand item group and not clear the supply list of the output inventory when selecting/removing a demanded item group 

- Set demanded items in storage lockers via text field:
  - Configure demanded (or supplied) item groups
    - Uses:
      - localized item names
      - custom item names (config: `synonymes = name1:idOrName1,name2:idOrName2` e.g.: `N2:NitrogenCapsule1,O2:OxygenCapsule1,example:Uranium Rod`)
      - ids
      - predefined names (Element names: Fe -> iron)
    - List all demanded item groups separated with commas
    - Append '>' to select all item groups containing this string in their name or id
      - Append `[substring]>` to exclude substrings from item names found by searching for the substring. Example: `larva>butterfly>` will only find common-, uncommon-, rare- and bee-larvae (added in v1.0.8)
  - Configure priority
    - Uses:
      - localized priority name
      - custom priority name (config: `priorityGroups = name1:value1,name2:value2` e.g.: `storage:2,s:2,AC:0`)
      - value
    - Appended after the demanded item group list with a '+' or ':'
    - Use `all` or `everything` (<- localized) in the text field to demand everything
    - Define lists to demand all groups from a list (config: lists).
    - Add `.`, `#` or `//` in front of the text field to ignore the text and not change the logistic settings (new v1.0.12.0)
    - Add `!` in front of the text to configure the supply item groups (instead of demand item groups) (new v1.0.12.0)
  - Examples:
    - `Fe, uranium rod :2` demands iron and uranium rod at priority 2 "Very high"
    - `iron +AC` demands iron at custom-configured priority "AC"
    - `SUPER ALLOY>` demands super alloy and super alloy rod, does not change priority
    - `+storage` sets priority to custom-configured priority "storage", does not change demanded item groups
  - Allow 125 characters in the name field of storage container by setting config 'allowLongNames' to 'true'
  - When selecting new demand groups, the text field is updated with the manually selected groups (config: enableSetDemandAsText) (new v1.0.19.0)

- Update 'Supply all' logistic settings if new item groups were added, e.g. by an update or a mod (config: updateSupplyAll)

- Show more item groups in the logistics menu:
  - Ignore lock conditions for hidden item groups (e.g. Common Larva (only displayed if blueprint is unlocked), ...) (config: logisticMenuIgnoreLockingConditions)
  - Custom list of item groups that would otherwise be hidden (config: logisticMenuAdditionalGroups)

- Prevent drones from delivering to shredders:
  - if the item is from a machine / container in dontDeliverToShredderFromMachines (<-configurable; default: Machines that only produce one item group + crafters + rockets (v1.526)) (config: dontDeliverFromProductionToShredder)
  - if the item is 'spawned' / from the ground (Example: eggplants, algae, ...) (config: dontDeliverSpawnedObjectsToShredder)

- Demand/Supply item groups that are contained in the inventory by holding `Left Ctrl` (config: addContainedGroupsModifierKey) while pressing the demand/supply selector button (new v1.0.12.0)

### Config:
- allowAnyValue:
  - Allows to paste-select any group (in e.g. T3 Ore Extractor), even if it isn't produced at that location
  - Allows to paste-/text-demand any group, including Cocoa Seed or Ore Crushers. Be careful with this setting
  - Allows priorities below lowest (-3) and above 5
  - Allows to set logistics for inventories that restrict the logistics settings (new v1.0.17.0)
- enablePotentialBuggedFeatures:
  - Allows to set logistics in inventories that have it disabled. This e.g. enables logistics in food growers, but also the extraction rocket, which has the logistics disabled and was bugged in previous game versions (new v1.018.0)
- enableNotification: Receive notifications for copy and paste operations or invalid text inputs

### Multiplayer compatibility:
- Should work for the host.
- Does not work on client side. It is on my todo list.
- 
## (Feat) Portal Travel
Adds the ability to travel from the portal of one planet to the portal of another planet without any cost.

### Features:
- Travel between fully terraformed planets without any cost (\*configurable) with the portal
  - How to use:
    - Press the lower button on the upper-left side in the portal generator display
    - Select the planet to travel to by pressig `Open`
    - Walk into the opened portal
  - Portals stay open (new v1.1.3.0) (config: keepPortalsOpen)
    - Portals are color coded (config: activateColoredPortals, portalDestinationColors)

Note: 
- Portals on moons, which shouldn't exist in the base game, will get turned into signs (new v1.2.10.0, previously the portals got removed) if the mod is removed and those signs are loaded as portal generators when the mod is installed again. This is a safety mechanism to prevent that procedural instances can be opened on the moons after the mod is removed/isn't functional anymore.
- Portal-traveling will close active portals

### Config
- configurable:
  - `requireFullTerraformation`: set to false: travel before full terraformation is reached
  - `disableOtherRequirements`: set to true: Other terraformation requirements (e.g. Purification on Toxicity) are disabled
  - `requireCost`: set to true: opening a portal costs 1 fusion energy cell
  - `costItems`: items that are required to open a portal (default: 1 fusion energy cell)

### Multiplayer compatibility:
- This mod is not fully multiplayer compatible. It only works for the host, but clients should not experience any problems.

## (Feat) Space Station
Adds a new "Planet": space
Build your own space station and terraform it's internal biosphere.

Note: 
- I am aware of the absurdity of terraforming space. Just imagine that you are terraforming the atmosphere in your space station.
- It isn't finished or balanced yet, but I currently don't have the time or knowledge to create what I imagined for this. 

### Features:
- Lower Gravity
- Higher Oxygen consumption
- Meteor resources can be harvested
- More meteors
- Free jetpack movement
- Everlasting Darkness
- High dependence on starting planet
- Many machines are disabled (e.g. drills, as it wouldn't make sense to *drill* from pressure and ores)
- Fly around with the vehicle
- and more!

### Multiplayer compatibility:
- (Not yet tested)

## Merge

Merge save files with different planet(s) into one combined save file

### PLEASE OPEN THE MERGE.PY FILE TO CONFIGURE THE SCRIPT AND TO FIND THE README AT THE TOP OF THE FILE
