$DocumentsPath = [System.Environment]::GetFolderPath([Environment+SpecialFolder]::MyDocuments)
Copy-Item -ErrorAction SilentlyContinue "$DocumentsPath\steamvr\input\application.generated.unity.kerbalvr_unitysteamvr.exe_knuckles.json" ..\KerbalVR_Mod\KerbalVR\Assets\Input\bindings_knuckles.json
Copy-Item -ErrorAction SilentlyContinue "$DocumentsPath\steamvr\input\application.generated.unity.kerbalvr_unitysteamvr.exe_oculus_touch.json" ..\KerbalVR_Mod\KerbalVR\Assets\Input\bindings_oculus_touch.json
Copy-Item -ErrorAction SilentlyContinue "$DocumentsPath\steamvr\input\application.generated.unity.kerbalvr_unitysteamvr.exe_vive_controller.json" ..\KerbalVR_Mod\KerbalVR\Assets\Input\bindings_vive_controller.json
Copy-Item .\Assets\SteamVR_Input\*.cs ..\KerbalVR_Mod\KerbalVR\SteamVR_Input\.
Copy-Item .\Assets\SteamVR_Input\ActionSetClasses\*.cs ..\KerbalVR_Mod\KerbalVR\SteamVR_Input\ActionSetClasses\.
