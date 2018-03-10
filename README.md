# Kerbal-VR
An add-on for Kerbal Space Program (KSP) to enable the use of a virtual reality headset (HTC Vive, Oculus Rift, etc), as supported by OpenVR. This mod is currently only tested with the HTC Vive, in "seated" mode.

### FOLLOW the installation instructions below, this is not like other KSP mods

**And if these instructions look too complicated, maybe some pictures will help: [Install Guide](https://github.com/Vivero/Kerbal-VR/wiki/Install-Guide)**

----

**Built for KSP v1.4**

[Demonstration video](https://www.youtube.com/watch?v=DjQauN66rQA)

----

This is an early WIP mod to allow the use of the HTC Vive (and potentially any HMD supported by the OpenVR SDK) in KSP. The primary focus is for use in IVA.

**Currently testing with:**

- HTC Vive
- Windows 10 64-bit
- Intel Core i5-6600K (overclocked to 4.1GHz)
- GeForce GTX 1070 8GB GDDR5
- Nvidia Driver 388.31
- 32GB RAM
- SteamVR beta version 1520469824

This KerbalVR plugin captures the orientation information from the HMD and translates it into head-tracking movement (position & rotation). It will also render the IVA view directly into the HMD. It is possible to get 90 FPS by following the **Performance Tips** below.

The [RasterPropMonitor mod](http://forum.kerbalspaceprogram.com/index.php?/topic/105821-112-rasterpropmonitor-still-putting-the-a-in-iva-v0260-30-april-2016/) makes it easier to navigate your craft in IVA, but I've noticed the game takes a performance hit while using it (lose about 20 or 30 fps).

**You may experience random crashes while using the mod. You have been warned.**

While in VR, your viewpoint is not limited to being inside the cockpit. If you have the space to physically move around, you can actually "walk" outside your craft and see it from outside (and it's way cooler than what I can describe here).

VR will only work with KSP using Direct3D 12, i.e. you need to use the `-force-d3d12` flag on the executable. It will not work if you try to run KSP normally. To enable the Direct3D 12 flag, create a Shortcut to either `KSP.exe` or `KSP_x64.exe`, and on the shortcut Properties, append `-force-d3d12` to the Target. Follow the [Install Guide](https://github.com/Vivero/Kerbal-VR/wiki/Install-Guide) if you're having trouble.

----

## Installation

Please see the [Install Guide](https://github.com/Vivero/Kerbal-VR/wiki/Install-Guide).

## Usage

Instructions:

1. Start SteamVR
2. Start up KSP with the `-force-d3d12` flag
3. During flight, enter IVA, and press the 'Y' key to initialize the HMD
4. You can press 'Y' again to turn off VR
5. Any errors should come up in the Debug log (press Alt-F12)

**NOTE:** While in IVA using VR, the game screen on your PC will appear locked up. This is by design and intended to improve the performance (higher FPS). Once you exit VR (by pressing 'Y'), the regular game screen *should* return to normal.

## Performance Tips

For best performance, go to the Settings menu in Kerbal Space Program, and under the Graphics tab, set:

- **Screen Resolution:** 1024 x 768
- **Full Screen:** unchecked
- **V-Sync:** Don't Sync
- **Frame Limit:** 100 FPS (or higher)

It is important to set V-Sync and Frame Limit as described above so that the Vive is able to render the game at 90 FPS. Experiment with the rest of the settings as you see fit.
