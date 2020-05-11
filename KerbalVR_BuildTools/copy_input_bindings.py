import re, os, shutil

# copy the output files to the KerbalVR_Mod folder

# start with input json files
input_files_path = os.path.join('..', 'KerbalVR_UnitySteamVR', 'Assets', 'StreamingAssets', 'SteamVR')
output_files = [f for f in os.listdir(input_files_path) if os.path.isfile(os.path.join(input_files_path, f)) and f.endswith('.json')]
output_path = os.path.join('..', 'KerbalVR_Mod', 'KerbalVR', 'Assets', 'Input')

for filename in output_files:
    input_full_path = os.path.abspath(os.path.join(input_files_path, filename))
    output_full_path = os.path.abspath(os.path.join(output_path, filename))

    shutil.copy(input_full_path, output_full_path)

# copy the generated input classes
input_files_path = os.path.join('..', 'KerbalVR_UnitySteamVR', 'Assets', 'SteamVR_Input')
output_files = [f for f in os.listdir(input_files_path) if os.path.isfile(os.path.join(input_files_path, f)) and f.endswith('.cs')]
output_path = os.path.join('..', 'KerbalVR_Mod', 'KerbalVR', 'SteamVR_Input')

for filename in output_files:
    input_full_path = os.path.abspath(os.path.join(input_files_path, filename))
    output_full_path = os.path.abspath(os.path.join(output_path, filename))

    shutil.copy(input_full_path, output_full_path)

# copy the generated action set classes
input_files_path = os.path.join('..', 'KerbalVR_UnitySteamVR', 'Assets', 'SteamVR_Input', 'ActionSetClasses')
output_files = [f for f in os.listdir(input_files_path) if os.path.isfile(os.path.join(input_files_path, f)) and f.endswith('.cs')]
output_path = os.path.join('..', 'KerbalVR_Mod', 'KerbalVR', 'SteamVR_Input', 'ActionSetClasses')

for filename in output_files:
    input_full_path = os.path.abspath(os.path.join(input_files_path, filename))
    output_full_path = os.path.abspath(os.path.join(output_path, filename))

    shutil.copy(input_full_path, output_full_path)
