# PlanetCrafterMods

## Mods
- [Auto-Logsitics]([https://github.com/mcnicki2002/PlanetCrafterMods/edit/main/README.md#auto-logistics--autoaddlogistic](https://github.com/mcnicki2002/PlanetCrafterMods/tree/main?tab=readme-ov-file#auto-logistics--autoaddlogistic))

## Scripts
- [merge]([https://github.com/mcnicki2002/PlanetCrafterMods/edit/main/README.md#merge](https://github.com/mcnicki2002/PlanetCrafterMods/tree/main?tab=readme-ov-file#merge))

## Auto-Logistics / AutoAddLogistic

Copy and paste (drone) logistic settings and automatically supply generated items.

### Features:
- Copy and paste
  - Copy by holding C (default key, config: 'copyLogisticsKey') while closing the logistics menu or when selecting an item group
  - Paste by holding V (default key, config: 'pasteLogisticsKey') while opening an inventory / a machine
  - Supported settings:
    - Supply settings
    - Demand settings
    - Priority settings
    - Selected item group (in e.g. T3 Ore Extractor, T2 Gas Extractor, Harvesting Robot and Planetary Delivery Depot)
	  - Note: Does not paste full logistic settings. Acts as if the group was selected.
    - Auto-Launch (in Trade Space Rocket and Interplanetary Exchange Shuttle) and auto-destroy settings (in Shredder Machine)
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
  - Configure demanded item groups
    - Uses:
	  - localized item names
	  - custom item names (config: `synonymes = name1:idOrName1,name2:idOrName2` e.g.: `N2:NitrogenCapsule1,O2:OxygenCapsule1,example:Uranium Rod`)
	  - ids
	  - predefined names (Element names: Fe -> iron)
    - List all demanded item groups separated with commas
	- Append '>' to select all item groups containing this string in their name or id
	- Use `all` or `everything` (<- localized) in the text field to demand everything
  - Configure priority
    - Uses:
	  - localized priority name
	  - custom priority name (config: `priorityGroups = name1:value1,name2:value2` e.g.: `storage:2,s:2,AC:0`)
	  - value
	- Appended after the demanded item group list with a '+' or ':'
  - Examples:
    - `Fe, uranium rod :2` demands iron and uranium rod at priority 2 "Very high"
    - `iron +AC` demands iron at custom-configured priority "AC"
    - `SUPER ALLOY>` demands super alloy and super alloy rod, does not change priority
    - `+storage` sets priority to custom-configured priority "storage", does not change demanded item groups
  - Allow 1000 characters in the name field of storage container by setting config 'allowLongNames' to 'true'
- Update 'Supply all' logistic settings if new item groups were added, e.g. by an update or a mod (config: updateSupplyAll)

### Config:
- allowAnyValue:
  - Allows to paste-select any group (in e.g. T3 Ore Extractor), even if it isn't produced at that location
  - Allows to paste-/text-demand any group, including Cocoa Seed or Ore Crushers. Be careful with this setting
  - Allows priorities below lowest (-3) and above 5
- enableNotification: Receive notifications for copy and paste operations or invalid text inputs

## Merge

Merge save files with different planet into one combined save file

### Requirements:
 - Python 3 (tested with Python 3.13, should world with any higher version)

### README / How to use:
- You can only merge saves which haven't visited the other planet. Use one Prime and one Humble save.
- Fill primaryFileName and secondaryFileName in the confic section of this script with the file name of the save files that should be combined.
  - How to get to the save file location:
    - 1: press F1 in the main menu
    - 2: open "%USERPROFILE%\AppData\LocalLow\MijuGames\Planet Crafter" in an explorer window
- Load both worlds in the new version and save them there to convert them to the new save file format.
- Close all portals.
- Place the player inventory and equipment of secondaryFileName in a chest (player inventories aren't merged).
- Run this script with python 3. 
  - How to run the script: 
    - Shift-right-click the explorer window that shows the folder this script is in. 
    - Click on 'open [command/powershell] window here'
    - Type 'python merge.py' and press enter. if this doesn't work, try 'python3 merge.py', 'py merge.py' or 'py3 merge.py'.
  - The output file will be called "[name of primary save] - merged.json" and therefore shouldn't override existing saves.
- The in-game name of the merged file will be "merged_[previous save file name]". 
  - To change the name, edit the save file, search for "saveDisplayName" and change the text in "" behind it.
  - Example: "saveDisplayName":"merged_Survival-1" -> "saveDisplayName":"MyNewSaveFileName"

### Restrictions/Warnings: 
 - The script can take several minutes for huge save files. Put the bigger save file in primaryFileName.
 - Blue crates, explodable rocks and other objects might respawn
 - Mod config items (e.g. id=4000 for akarnokd's uihotbar) from secondaryFileName might loose their config behaviour.
 - Player Inventories aren't merged as discussed above. 
 - "worldSeed" (current known effect on: not generated wrecks in world, animals in world, random ores, tree position, something 'spawned on floor') 
     as well as the settings from secondaryFileName can't be merged.
 - "Message_YouAreAConvict" might appear twice.
 - PCLayers might get duplicated. Effect not tested.
 - This script will not work if you placed more than 100000 containers/Inventories. 
 - There will be a few junk items in your save. They won't show in-game and shouldn't create any problems. 
       The script can't really filter them out though.
