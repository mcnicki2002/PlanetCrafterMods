
folder = "[Insert/Path/To]/ExportedProject/Assets/Scripts/Assembly-CSharp/SpaceCraft"

import os
for filename in os.listdir(folder):
	if os.path.isfile(folder + "/" + filename):
		with open(folder + "/" + filename, "r+") as f:
			data = f.read()
			
			output = data.replace("target.__rpc_exec_stage", "((" + filename[:-3] + ")target).__rpc_exec_stage")
			
			f.seek(0)
			f.write(output)
			f.truncate()
