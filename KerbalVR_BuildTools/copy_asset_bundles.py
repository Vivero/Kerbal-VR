import re, os, shutil

# copy the output files to the KerbalVR_Mod folder
input_files_path = os.path.join('..', 'KerbalVR_Unity', 'AssetBundles')
output_files = [f for f in os.listdir(input_files_path) if os.path.isfile(os.path.join(input_files_path, f)) and f.endswith('.ksp')]
output_path = os.path.join('..', 'KerbalVR_Mod', 'KerbalVR', 'Assets', 'AssetBundles')

for filename in output_files:
    output_filename = re.sub(r'\.ksp$', '.dat', filename)
    input_full_path = os.path.abspath(os.path.join(input_files_path, filename))
    output_full_path = os.path.abspath(os.path.join(output_path, output_filename))

    shutil.copy(input_full_path, output_full_path)
