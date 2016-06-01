# Kerbal-VR
An add-on for Kerbal Space Program (KSP) to enable head-tracking through OpenVR (HTC Vive, Oculus Rift, etc)

**Built for KSP 1.1.2**

----

This is an early WIP mod to allow the use of the HTC Vive (and potentially any HMD supported by the OpenVR SDK) in KSP. The primary focus is for use in IVA. Head-tracking works, but displaying the game screen on the HMD currently has caveats.

**I'm currently only testing with the HTC Vive on Windows 10.**

I haven't been able to find a proper injector to use with KSP that is compatible with the Vive. VorpX and TriDef are not free, and Vireio currently does not support the HTC Vive.

The KerbalVR plugin simply captures the orientation information from the HMD and translates it into head-tracking movement (position & rotation).

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

**The best method I've found** to display KSP on the HTC Vive is to use [Virtual Desktop](http://store.steampowered.com/app/382110/). It's not free, but if you already dropped $800+ for a Vive, then the cost of this should be negligible. The following options seem to provide a good IVA experience:

- Screen Size: *240 degrees*
- Screen Distance: *0.80m*
- Screen Options: *Curved*

Instructions:

1. Start SteamVR. Only the Vive headset will be used (no controllers needed), and will be a Seated Experience (no Room-Scale required).
2. Sit down
3. Put on your Vive
4. Start Virtual Desktop
5. Start up KSP
6. During flight, enter IVA, and press the 'N' key to initialize the HMD
7. You can press 'N' again to reset the default position
8. Any errors should come up in the Debug log (press Alt-F12)
