# PlanetCrafterMods
[![Github All Releases](https://img.shields.io/github/downloads/mcnicki2002/PlanetCrafterMods/total.svg)](https://github.com/mcnicki2002/PlanetCrafterMods/releases/latest)

The latest versions of the mods can be found in the releases https://github.com/mcnicki2002/PlanetCrafterMods/releases
## Mods
- [(Cheat) Custom Ore Randomization](#cheat-custom-ore-randomization)
- [(Cheat) Machine Config](#cheat-machine-config)
- [(Cheat) Store Toxins in Toxic Storage](#cheat-store-toxins-in-toxic-storage)
- [(Feat) Large Screens](#feat-large-screens)
- [(Feat) Planet Selector](#feat-planet-selector)
- [(Feat) Portal Travel](#feat-portal-travel)
- [(Feat) Space Station](#feat-space-station)
- [(Feat) Terrain Height Tool](#feat-terrain-height-tool)
- [(Feat) Underground Base](#feat-underground-base)
- [(Fix) Rocket Return](#fix-rocket-return)
- [(Fix) Water Wave Config](#fix-water-wave-config)
- [(Item) More Fuses](#item-more-fuses)
- [(Item) Wall Aquarium](#item-wall-aquarium)
- [(QoL) Auto-Logistics](#qol-auto-logistics)
- [(QoL) Meteor Debris Config](#qol-meteor-debris-config)
- [(QoL) Move Containers](#qol-move-containers)
- [(QoL) Skip Tutorial](#qol-skip-tutorial)
- [(UI) Change Music](#ui-change-music)
- [(UI) Confine Mouse](#ui-confine-mouse)
- [(UI) Move Units](#ui-move-units)
- [(UI) Sound Config](#ui-sound-config)
- [(Visual) Hide Objects](#visual-hide-objects)



## Scripts
- [merge](#merge)

## Mod Types:
- Requested: This mod was requested by someone and I will not actively test it. If something breaks, please contact me.
- Released: This mod should work to full extend. 
- Proof of concept: This mod works to some extend, but it has some rough edges. If something breaks, please contact me, but I do not promise to fix it.
- PROOFN'T OF CONCEPT: This mod probably never really worked. If something breaks, please contact me, but I do not promise to fix it.
- Not yet finished: This mod is not yet finished due to various possible reasons (e.g. lack of knowledge etc.)
- Discontinued: This mod is discontinued, probably because it is a native feature now. Will not receive updates.


## (Cheat) Custom Ore Randomization
### Features:
- When creating a save file with randomized ores, define which ores are located where.
- Only works when the mod is installed.
### Config:
- `OreConfig`: a Semicolon `;` separated list of `oreToReplace:replacementOre` pairs
  - Example: `Uranim:Iridium;Iridium:Uranim;Alloy:Alloy` changes uranium to iridium, iridium to uranium and keeps super alloy where it should be
### Type: Requested
### Multiplayer compatibility:
- Not yet tested


## (Cheat) Machine Config
### Features:
- Configure time and other properties of various machines
- The value `0` won't change any values (except for the drone speed config: `1` uses the default drone speed)
### Config:
- See config file
### Type: Released
### Multiplayer compatibility:
- Not yet tested

## (Cheat) Store Toxins in Toxic Storage
### Features:
- Store Toxins (and other items) in Toxic containers
### Config:
- `gIDsToStoreInToxicStorage`: list of IDs that can be stored in Toxic containers
### Type: Requested/Released
### Multiplayer compatibility:
- Not yet tested

## (Feat) Large Screens
### Features:
- The character limit for signs is set to the game's limit of 125 characters in a text box
- Prefix the text on a sign with `!NUMBER!`
  - Example: `!3! This is a square sign` will stretch the sign to a square size where the full text is readable
- FYI: Signs support (TMPro) Rich Text: https://docs.unity3d.com/Packages/com.unity.textmeshpro@4.0/manual/RichTextSupportedTags.html
### Config:
- `FontSize`: Font size
### Type: Requested/Released
### Multiplayer compatibility:
- Not yet tested


## (Feat) Planet Selector
Adds a UI to change the planet displayed above the Planet Viewer. 
Click on the Planet Viewer and select the planet that you want to see on it.
### Features:
- Select which planet is displayed in a Planet Viewer
### Config:
- `sunAngle`: Set at which angle the Planet Viewer is eluminated
### Type: Released
### Multiplayer compatibility:
- Not yet tested


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

### Type: Released

### Multiplayer compatibility:
- This mod is not fully multiplayer compatible. It only works for the host, but clients should not experience any problems. Clients need the mod as well, otherwise they are stuck in the loading screen for 2 minutes!

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

### Type: Not yet finished
- The features work, but the map is not nearly finished.

### Multiplayer compatibility:
- (Not yet tested)


## (Feat) Terrain Height Tool
Change the terrain height
### Features:
- Press `CTRL + T` to enable the terrain forming function
- Hold `CTRL` while pressing the left or right mouse button to increase or decrease the terrain height
- Warning: This will increase the save file size massively
### Config:
(/)
### Type: PROOFN'T OF CONCEPT
- The save file size, the lag and that other mountains/ores/... aren't moved is just not properly usable. 
### Multiplayer compatibility:
- Not yet tested

## (Feat) Underground Base
### Features:
- Build an underground base with the "Underground base ladder"
- Just build living compartments inside the terrain to deform the terrain
- Where possible, glass walls and doors are covered with rocks when they are below the ground, to create the illusion of being inside the ground
- Ladders below the ground that don't lead to livable areas won't work, to prevent that the player can get out of an underground base and fall off the map
### Config:
(/)
### Type: Proof of concept
- You will notice that you can look below the map at various places, but in general the mod should work. Consider it "Released" with the limitation that you can look below the ground at some places.
### Multiplayer compatibility:
- Not yet tested

## (Fix) Rocket Return
### Features:
- Fixes the problem of rockets not returning due to a floating point error in the code.
- For more infos about the bug, see https://discord.com/channels/635441508694097921/959021419595960330/1439608866005323776
### Config:
(/)
### Type: Requested
### Multiplayer compatibility:
- Not yet tested

## (Fix) Water Wave Config
### Features:
- Change the water height on Aqualis and the height of the waves
### Config:
- `aqualisOceanDisplacement`: Set the wave height on Aqualis
- `aqualisOceanHeightOffset`: Set the height of the water surface on Aqualis
### Type: Requested
### Multiplayer compatibility:
- Not yet tested

## (Item) More Fuses
### Features:
- Adds compressed fuses (9 fuses make a T2 fuse with the boosting power of 10 fuses etc.)
- (Adds 1000x rockets that must be edited in the save file; here for performance improvements due to item count reduction)
### Config:
(only by editing and recompiling the source code)
### Type: Released
- The rockets are for personal use, so it's best to ignore them.
### Multiplayer compatibility:
- Not yet tested

## (Item) Wall Aquarium
### Features:
- Aquarium wall
- Hold `CTRL` while clicking on the wall to open a color configurator UI where the (client-side) color for all windows in the world can be set.
### Config:
- `windowColor`: Changes the window color globally
- `lodMultiplier`: Changes how far away the components start to (dis-)appear
- `cleanMoss`: Removes the moss that is present in procedural wrecks
### Type: Discontinued
- It's now a native feature
### Multiplayer compatibility:
- Fully compatible

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

### Type: Released

### Multiplayer compatibility:
- Should work for the host.
- Does not work on client side. It is on my todo list.


## (QoL) Meteor Debris Config
### Features:
- Change how long the debris (rocks etc., NOT ores) of meteors persists in the world
### Config:
- `multiplier`: Multiplier for how long debris exists. Example: 0.1 => debris disappears 10 times faster.
### Type: Requested
### Multiplayer compatibility:
- Not yet tested


## (QoL) Move Containers
### Features:
- Hold the Accessibility Key (default: Ctrl) + Alt, click on a container, move and rotate the ghost and place it down to move the container.
### Config: (/)
### Type: Released/Proof of concept
### Multiplayer compatibility:
- Does not work


## (QoL) Skip Tutorial
### Features:
- Skip the tutorials.
### Config:
- `skipNewTutorial`: Skips new tutorials
- `skipBlueSkyTutorial`: Hides the Blue Sky tutorial steps
### Type: Released
### Multiplayer compatibility:
- Not yet tested


## (UI) Change Music
### Features:
- Load different music (in mp3/ogg/wav format) by placing the files next to the mod dll
- These are the four old (Early Access) music tracks (+links from where you can obtain a license for them):
  - Dark Fantasy Studio - Album: "The monster that lies within": https://darkfantasystudio.com/album?id=68d43c51fee48d66350ec8da 
    - Once upon a time
    - Until dawn
    - After dark
  - By Andrew Sitkov: https://www.gamedevmarket.net/asset/cosmos-music-pack
    - Unknown Terrain
### Config:
- `addInsteadOfReplace`: Add music instead of replacing it.
### Type: Requested
### Multiplayer compatibility:
- Not yet tested, but should be Client-side only


## (UI) Confine Mouse
### Features:
- Confine the mouse to the game window
- E.g. helpful with multiple monitors or when using compatibility layers that break mouse movement
### Config:
### Type: Requested
### Multiplayer compatibility:
- Not yet tested


## (UI) Move Units
### Features:
- Replace the units with your own
### Config:
- strings of the units displayed
### Type: Not yet finished
- It's more of a personal project
### Multiplayer compatibility:
- Not compatible, will likely lead to silent crashes when different configs are used


## (UI) Sound Config
### Features:
- Set the volume for several sounds indiviually, e.g. reduce the volume of the UiClose sound
### Config:
- Volume configs for the sounds: UiHover, UiMove, UiOpen, UiClose, UiSelectElement, AlertLow, AlertCritical, CheckTutorial, EnergyLack, EnergyRestored, DropObject, CantDo, Teleport
### Type: Requested
### Multiplayer compatibility:
- Not yet tested, but should be Client-side only


## (Visual) Hide Objects
### Features:
- Prevent invisible bases etc. in occlusion objects
- Occlude configurable machines at distances or only show them when a button is pressed.
### Config:
- `preventOcculsion`: Prevent occulsion in occulsion colliders (e.g. in the maze and the region north of it)
### Type: Released (/Requested)
- Distance/Button-enabled occlusion isn't actively tested
### Multiplayer compatibility:
- Not yet tested

## Merge

Merge save files with different planet(s) into one combined save file

### PLEASE OPEN THE MERGE.PY FILE TO CONFIGURE THE SCRIPT AND TO FIND THE README AT THE TOP OF THE FILE

## ()
### Features:
### Config:
### Type: [Requested/Released/Proof of concept/PROOFN'T OF CONCEPT/Not yet finished/Discontinued]
### Multiplayer compatibility:
- Not yet tested
