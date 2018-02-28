# Kerbal-VR
An add-on for Kerbal Space Program (KSP) to enable the use of a virtual reality headset (HTC Vive, Oculus Rift, etc), as supported by OpenVR. This mod is currently only tested with the HTC Vive, in "seated" mode.

### FOLLOW the installation instructions below, this is not like other KSP mods

**And if these instructions look too complicated, maybe some pictures will help: [Install Guide](https://github.com/Vivero/Kerbal-VR/wiki/Install-Guide)**

----

**Built for KSP v1.3.1**

[Demonstration video](https://www.youtube.com/watch?v=DjQauN66rQA)

----

This is an early WIP mod to allow the use of the HTC Vive (and potentially any HMD supported by the OpenVR SDK) in KSP. The primary focus is for use in IVA. Head-tracking works, but displaying the game screen on the HMD currently has caveats.

**Currently testing with:**

- HTC Vive
- Windows 10 64-bit
- Intel Core i5-6600K (overclocked to 4.1GHz)
- GeForce GTX 1070 8GB GDDR5
- Nvidia Driver 388.31
- 32GB RAM
- SteamVR beta version 1519673958

This KerbalVR plugin captures the orientation information from the HMD and translates it into head-tracking movement (position & rotation). It will also render the IVA view directly into the HMD. However, rendering is slow, even on a decent rig that I'm testing with. Maybe needs some code re-work to reduce inefficiencies. But even with all graphics settings set to low, I can only get about 40-60 fps, which makes for a nauseating VR experience.

It's still fairly playable for short amounts of time. I'd also recommend installing the [RasterPropMonitor mod](http://forum.kerbalspaceprogram.com/index.php?/topic/105821-112-rasterpropmonitor-still-putting-the-a-in-iva-v0260-30-april-2016/) to make it easier to navigate your craft in IVA. However be warned, I've seen the game crash randomly while using VR. Not sure if it's KSP-specific issues, or VR-related issues.

While in VR, your viewpoint is not limited to being inside the cockpit. If you have the proper setup and space to move around irl, you can actually "walk" outside your craft and see it from outside (and it's way cooler than what I can describe here).

VR will only work with KSP using Direct3D 12, i.e. you need to use the "-force-d3d12" flag on the executable. It will not work if you try to run KSP normally. To enable the Direct3D 12 flag, create a Shortcut to either `KSP.exe` or `KSP_x64.exe`, and on the shortcut Properties, append "-force-d3d12" to the Target, e.g.:

```
Target: C:\Games\KSP_win\KSP_x64.exe -force-d3d12
```

----

## Installation

1. Download the [latest KerbalVR release](https://github.com/Vivero/Kerbal-VR/releases).
2. Copy the `KerbalVR` folder into your KSP `GameData` folder. The `KerbalVR` folder contains `KerbalVR.dll` and a `openvr` directory containing the `openvr.dll` libraries.

Should look something like this:

```
+-- Game Data
|   +-- KerbalVR
|   |   +-- KerbalVR.dll                     <-- MAKE SURE THIS IS HERE
|   |   +-- openvr                           <-- MAKE SURE THIS IS HERE
|   |   +-- openvr\win32                     <-- MAKE SURE THIS IS HERE
|   |   +-- openvr\win64                     <-- MAKE SURE THIS IS HERE
|   |   +-- openvr\win64\openvr.dll          <-- MAKE SURE THIS IS HERE
|   +-- Squad
|   +-- <other mods>
+-- KSP.exe
+-- KSP_x64.exe
+-- <other KSP files/directories>
```

## Usage

Instructions:

1. Start SteamVR
2. Start up KSP with the "-force-d3d12" flag
5. During flight, enter IVA, and press the 'Y' key to initialize the HMD
6. You can press 'Y' again to reset the default position
7. Any errors should come up in the Debug log (press Alt-F12)
