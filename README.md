# Kerbal-VR
An add-on for Kerbal Space Program (KSP) to enable head-tracking through OpenVR (HTC Vive, Oculus Rift, etc)

----

This is an early WIP mod to allow the use of the HTC Vive (and potentially any HMD supported by the OpenVR SDK) in KSP. The primary focus is for use in IVA. Head-tracking works, but displaying the game screen on the HMD currently has caveats.

**I'm currently only testing with the HTC Vive on Windows 10.**

The KerbalVR plugin captures the orientation information from the HMD and translates it into head-tracking movement (position & rotation).

** Now with 100% more display injection!!1**

The KerbalVR plugin now also renders the IVA camera directly into the HMD, for a more realistic experience. However, rendering is slow, probably needs some code re-work to reduce inefficiencies.

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
+-- KSP.exe
+-- KSP_x64.exe
+-- openvr_api.dll
```

## Usage

Instructions:

1. Start SteamVR. Only the Vive headset will be used (no controllers needed), and will be a Seated Experience (no Room-Scale required).
2. Sit down
3. Put on your Vive
4. Start up KSP
5. During flight, enter IVA, and press the 'N' key to initialize the HMD
6. You can press 'N' again to reset the default position
7. Any errors should come up in the Debug log (press Alt-F12)
