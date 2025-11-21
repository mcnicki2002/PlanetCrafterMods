import random
import os

#
# Requirements:
# - Python 3 (tested with Python 3.13, should world with any higher version)
#
# README / How to use:
# - You can only reliably merge saves which haven't visited the other planet. Use one Prime and one Humble save.
# - Fill primaryFileName and secondaryFileName in the confic section of this script with the file name of the save files that should be combined.
#       How to get to the save file location:
#           1: press F1 in the main menu
#           2: open "%USERPROFILE%\AppData\LocalLow\MijuGames\Planet Crafter" in an explorer window
# - Load both worlds in the new version and save them there to convert them to the new save file format.
# - Close all portals.
# - Place the player inventory and equipment of secondaryFileName in a chest (player inventories aren't merged).
# - Run this script with python 3. 
#       How to run the script: 
#           - Shift-right-click the explorer window that shows the folder this script is in. 
#           - Click on 'open [command/powershell] window here'
#           - Type 'python merge.py' and press enter. if this doesn't work, try 'py merge.py', 'python3 merge.py' or 'py3 merge.py'.
#       The output file will be called "[name of primary save]-merged.json" and therefore shouldn't override existing saves.
# - The in-game name of the merged file will be "merged_[previous save file name]". 
#       To change the name, edit the save file, search for "saveDisplayName" and change the text in "" behind it.
#       E.g.: "saveDisplayName":"merged_Survival-1" -> "saveDisplayName":"MyNewSaveFileName"
# 
# Restrictions/Warnings: 
# - The script can take several minutes for huge save files. Put the bigger save file in primaryFileName.
# - Blue crates, explodable rocks and other objects might respawn
# - Mod config items (e.g. id=4000 for akarnokd's uihotbar) from secondaryFileName might loose their config behaviour.
# - Player Inventories aren't merged as discussed above. 
# - "worldSeed" (current known effect on: not generated wrecks in world, animals in world, random ores, tree position, something 'spawned on floor') 
#     as well as the settings from secondaryFileName can't be merged.
# - "Message_YouAreAConvict" might appear twice.
# - PCLayers from the second save file are only merged if they are from a merged planet. Duplicated layers are a problem.
# - This script will not work if you placed more than 100000 containers/Inventories. 
# - There will be a few junk items in your save. They won't show in-game and shouldn't create any problems. 
#       The script can't really filter them out though.
# - As of v3, the script can merge the progress of planets. This does NOT mean that it can fully merge planets. Doing so is still not recommended.
# - Toxic water depletion (-> 'count' property) isn't merged (added in v1.6XX). Possible TODO, but no support for planet merging is intended.
#     If one save file has cleaned lakes, use that one as the primaryFileName to carry that progress over to the merged file.
# - NOTE: This script was created for the moons update (v1.518 - v1.526) and it will break when the save file format changes.
# 
# Last tested game version: v1.610
#
# Script author: Nicki0
# Version: 6
# 
# Changelog v2:
# - fixed drone inventories not being merged properly
# - added sanitization
#
# Changelog v3:
# - planet progress will be merged
#
# Changelog v4:
# - layer merge fixed
#
# Changelog v5:
# - save files are loaded with UTF-8 encoding to support characters from other languages
#
# Changelog v6:
# - added support to merge unitPurificationLevel for Toxicity
#

# - start config
primaryFileName = "Survival-1.json" # replace with first file name, preferably use the bigger save file
secondaryFileName = "Survival-2.json" # replace with second file name
# - end config

# Do not change anything below if you don't know what you are doing.
# If you know what you are doing, I promise that I usually use better names for variables.

primaryFilePath = os.environ['LOCALAPPDATA'] + "low/MijuGames/Planet Crafter/" + primaryFileName
secondaryFilePath = os.environ['LOCALAPPDATA'] + "low/MijuGames/Planet Crafter/" + secondaryFileName

if not os.path.exists(primaryFilePath):
	print("File " + primaryFileName + " not found")
	exit()
if not os.path.exists(secondaryFilePath):
	print("File " + secondaryFileName + " not found")
	exit()

with open(primaryFilePath, 'r', encoding="utf8") as filePrimary:
	sectionsP = filePrimary.read().replace("\r", "").replace("\n", "").split('@')
with open(secondaryFilePath, 'r', encoding="utf8") as fileSecundary:
	sectionsS = fileSecundary.read().replace("\r", "").replace("\n", "").split('@')

if not "{\"terraTokens\":" in sectionsP[0]:
	print(primaryFileName + " is not converted. Please save it again in the new version.")
	exit()
if not "{\"terraTokens\":" in sectionsS[0]:
	print(secondaryFileName + " is not converted. Please save it again in the new version.")
	exit()

if not "\"openedInstanceSeed\":0," in sectionsS[0]:
	print(secondaryFileName + " has an open portal. Please close it before merging save files.")
	exit()

dictIdsP = {}
dictIdsSChange = {}
dictLiIdsSChange = {}

itemsP = []
for i in range(len(sectionsP)):
	itemsP.append(sectionsP[i].split('|'));
for i in range(len(itemsP[3])):
	a,b = itemsP[3][i].split(',', 1)
	dictIdsP[a] = b

ctr = 0
print("-----------")
itemsS = []
for i in range(len(sectionsS)):
	itemsS.append(sectionsS[i].split('|'));
# give colliding items new ids
for i in range(len(itemsS[3])):
	a,b = itemsS[3][i].split(',', 1)
	if a in dictIdsP:
		ctr += 1
		if "{\"id\":10" in a:
			print("collision", a, b)
			continue
		while True:
			newId = random.randint(200000000, 209999999)
			if ("{\"id\":" + str(newId)) not in dictIdsP:
				oldId = a.strip()[-9:]
				dictIdsSChange[oldId] = str(newId)
				itemsS[3][i] = itemsS[3][i].replace("{\"id\":" + oldId, "{\"id\":" + str(newId))
				break
# apply ids to inventory section
for i in range(len(itemsS[4])):
	for key in dictIdsSChange.keys():
		itemsS[4][i] = itemsS[4][i].replace(key, dictIdsSChange[key])
# apply ids to wreck section
for i in range(len(itemsS[10])):
	for key in dictIdsSChange.keys():
		itemsS[10][i] = itemsS[10][i].replace(key, dictIdsSChange[key])
# move inventors ids to 100000 to prevent collision
for i in range(len(itemsS[4])):
	a,_ = itemsS[4][i].split(',', 1)
	a = a.strip()
	if len(a) < 15:
		newSize = 100000 + int(a[6:])
		itemsS[4][i] = itemsS[4][i].replace(a, a[:6] + str(newSize))
		dictLiIdsSChange[a[6:]] = str(newSize)
# apply changed inv ids to liId
dictMod = {}
for i in range(len(itemsS[3])):
	for key in dictLiIdsSChange.keys():
		# "liId":537,"siIds":"538"
		if i in dictMod:
			continue
		if "\"liId\":" + key + "," in itemsS[3][i]:
			itemsS[3][i] = itemsS[3][i].replace("\"liId\":" + key + ",", "\"liId\":" + dictLiIdsSChange[key] + ",")
			dictMod[i] = True;
		elif "\"liId\":" + key + "}" in itemsS[3][i]: #{"id":208064323,"gId":"Drone2","liId":829}|
			itemsS[3][i] = itemsS[3][i].replace("\"liId\":" + key + "}", "\"liId\":" + dictLiIdsSChange[key] + "}")
			dictMod[i] = True;
# apply changed inv ids to siIds
dictMod = {}
for i in range(len(itemsS[3])):
	for key in dictLiIdsSChange.keys():
		# "liId":537,"siIds":"538"
		if i in dictMod:
			continue
		if "\"siIds\":\"" + key + "\"" in itemsS[3][i]:
			itemsS[3][i] = itemsS[3][i].replace("\"siIds\":\"" + key + "\"", "\"siIds\":\"" + dictLiIdsSChange[key] + "\"")
			dictMod[i] = True;
			
print("collision counter:", ctr)

# Combine Layers
itemsS9AfterLayersRemoved = []
layerPlanetIDsFoundInItemsP9 = set()
for i in range(len(itemsP[9])):
	if 'planet":' in itemsP[9][i]:
		planetIdOfLayer = itemsP[9][i].split('planet":', 1)[1].split(',', 1)[0]
		layerPlanetIDsFoundInItemsP9.add(planetIdOfLayer)
for i in range(len(itemsS[9])):
	if 'planet":' in itemsS[9][i]:
		planetIdOfLayer = itemsS[9][i].split('planet":', 1)[1].split(',', 1)[0]
		if planetIdOfLayer not in layerPlanetIDsFoundInItemsP9:
			itemsS9AfterLayersRemoved.append(itemsS[9][i])
itemsS[9] = itemsS9AfterLayersRemoved
	

items = []
for el in itemsP:
	items.append(el)
for i in [3,4,6,7,9,10]:
	itemsP[i][-1] = itemsP[i][-1].strip()
	items[i] = itemsP[i] + itemsS[i]

# Add TerraTokens
ttsplitP = itemsP[0][0].split(',', 2)
ttP = ttsplitP[0][15:]
tttotalP = ttsplitP[1][21:]
ttsplitS = itemsS[0][0].split(',', 2)
ttS = ttsplitS[0][15:]
tttotalS = ttsplitS[1][21:]
items[0][0] = items[0][0].replace(ttP, str(int(ttP) + int(ttS)))
items[0][0] = items[0][0].replace(tttotalP, str(int(tttotalP) + int(tttotalS)))

# Combine Planets
planets = []
for i in range(len(itemsP[1])):
	planetId = itemsP[1][i].split('"planetId":"', 1)[1].split('"', 1)[0]
	unitOxygenLevel = itemsP[1][i].split('unitOxygenLevel":', 1)[1].split(',', 1)[0]
	unitHeatLevel = itemsP[1][i].split('unitHeatLevel":', 1)[1].split(',', 1)[0]
	unitPressureLevel = itemsP[1][i].split('unitPressureLevel":', 1)[1].split(',', 1)[0]
	unitPlantsLevel = itemsP[1][i].split('unitPlantsLevel":', 1)[1].split(',', 1)[0]
	unitInsectsLevel = itemsP[1][i].split('unitInsectsLevel":', 1)[1].split(',', 1)[0]
	unitAnimalsLevel = itemsP[1][i].split('unitAnimalsLevel":', 1)[1].split(',', 1)[0]
	unitPurificationLevel = itemsP[1][i].split('unitPurificationLevel":', 1)[1].split('}', 1)[0]
	planet = [planetId, unitOxygenLevel, unitHeatLevel, unitPressureLevel, unitPlantsLevel, unitInsectsLevel, unitAnimalsLevel, unitPurificationLevel]
	planets.append(planet)
for i in range(len(itemsS[1])):
	planetId = itemsS[1][i].split('"planetId":"', 1)[1].split('"', 1)[0]
	unitOxygenLevel = itemsS[1][i].split('unitOxygenLevel":', 1)[1].split(',', 1)[0]
	unitHeatLevel = itemsS[1][i].split('unitHeatLevel":', 1)[1].split(',', 1)[0]
	unitPressureLevel = itemsS[1][i].split('unitPressureLevel":', 1)[1].split(',', 1)[0]
	unitPlantsLevel = itemsS[1][i].split('unitPlantsLevel":', 1)[1].split(',', 1)[0]
	unitInsectsLevel = itemsS[1][i].split('unitInsectsLevel":', 1)[1].split(',', 1)[0]
	unitAnimalsLevel = itemsS[1][i].split('unitAnimalsLevel":', 1)[1].split(',', 1)[0]
	unitPurificationLevel = itemsS[1][i].split('unitPurificationLevel":', 1)[1].split('}', 1)[0]
	planet = [planetId, unitOxygenLevel, unitHeatLevel, unitPressureLevel, unitPlantsLevel, unitInsectsLevel, unitAnimalsLevel, unitPurificationLevel]
	planetExistsInPlanets = False
	for j in range(len(planets)):
		if planets[j][0] == planetId:
			planetExistsInPlanets = True
			print("Merging Planet Progress for Planet " + planetId)
			for k in range(1, len(planet)):
				if float(planets[j][k]) == -1 or float(planet[k]) == -2:
					planets[j][k] = str(-1) # for disabled units (e.g. Purification on Prime)
				else:
					planets[j][k] = str(float(planets[j][k]) + float(planet[k]))
	if not planetExistsInPlanets:
		planets.append(planet)
items[1] = ['{"planetId":"' + str(planets[i][0]) + '","unitOxygenLevel":' + str(planets[i][1]) + ',"unitHeatLevel":' + str(planets[i][2]) + ',"unitPressureLevel":' + str(planets[i][3]) + ',"unitPlantsLevel":' + str(planets[i][4]) + ',"unitInsectsLevel":' + str(planets[i][5]) + ',"unitAnimalsLevel":' + str(planets[i][6]) + ',"unitPurificationLevel":' + str(planets[i][7]) + '}' for i in range(len(planets))]

# Combine crafted item count
#{"craftedObjects":1043463
ciP = itemsP[5][0].split(',', 1)[0][18:]
ciS = itemsS[5][0].split(',', 1)[0][18:]
items[5][0] = items[5][0].replace(ciP, str(int(ciP) + int(ciS)))

# Combine unlockedGroups
unlockedGroupsPOrigToReplace = itemsP[0][0].split("unlockedGroups\":\"")[1].split('"')[0].strip()
unlockedGroupsP = unlockedGroupsPOrigToReplace.split(',')
unlockedGroupsS = itemsS[0][0].split("unlockedGroups\":\"")[1].split('"')[0].strip().split(',')
for e in unlockedGroupsS:
	if e not in unlockedGroupsP:
		unlockedGroupsP.append(e)
		print("Moved blueprint " + e)
itemsP[0][0] = itemsP[0][0].replace(unlockedGroupsPOrigToReplace, ','.join(unlockedGroupsP));

# rename merged save
#{"saveDisplayName":"
items[8][0] = items[8][0][:20] + "merged_" + items[8][0][20:]

for key in dictIdsP.keys():
	pass
	#print(key, dictIdsP[key])
with open(primaryFilePath[:-5] + "-merged.json", 'w', encoding="utf8") as outputFile:
	outputFile.write('\n@\n'.join(['|\n'.join(items[i]) for i in range(len(items))]))

print("done")
