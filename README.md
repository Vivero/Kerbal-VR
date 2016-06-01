# Kerbal-VR
An add-on for Kerbal Space Program (KSP) to enable the use of a virtual reality headset (HTC Vive, Oculus Rift, etc), as supported by OpenVR.

----

This is an early WIP mod to allow the use of the HTC Vive (and potentially any HMD supported by the OpenVR SDK) in KSP. The primary focus is for use in IVA. Head-tracking works, but displaying the game screen on the HMD currently has caveats.

**Currently testing with:**

- HTC Vive
- Windows 10
- Intel Core i5-6600K
- EVGA GeForce GTX 970 4GB GDDR5 (04G-P4-3975-KR)
- 16GB DDR4 (PC4-17000)
- SteamVR / OpenVR SDK

This KerbalVR plugin captures the orientation information from the HMD and translates it into head-tracking movement (position & rotation). It will also render the IVA view directly into the HMD. However, rendering is slow, even on a decent rig that I'm testing with. Maybe needs some code re-work to reduce inefficiencies. But even with all graphics settings set to low, I can only get about 30-50 fps, which makes for a nauseating VR experience.

While in VR, your viewpoint is not limited to being inside the cockpit. If you have the proper setup and space to move around irl, you can actually "walk" outside your craft and see it from outside (and it's way cooler than what I can describe here).

----

## Installation

1. Copy the `KerbalVR` folder into your KSP `GameData` folder.
2. Copy `openvr_api.dll` into the **root of the KSP directory**, i.e. `openvr_api.dll` should be next to KSP.exe and KSP_x64.exe. Use either the 32- or 64-bit version of `openvr_api.dll`, according to your bit-version of Windows.

Should look something like this:

```
+-- Game Data
|   +-- KerbalVR
|   |   +-- KerbalVR.dll
|   +-- Squad
|   +-- <other mods>
+-- KSP.exe
+-- KSP_x64.exe
+-- openvr_api.dll
+-- <other KSP files/directories>
```

## Usage

Instructions:

1. Start SteamVR. Only the Vive headset will be used (no controllers needed), and it will be a Seated Experience (no Room-Scale required).
2. Sit down
3. Put on your Vive
4. Start up KSP
5. During flight, enter IVA, and press the 'N' key to initialize the HMD
6. You can press 'N' again to reset the default position
7. Any errors should come up in the Debug log (press Alt-F12)
