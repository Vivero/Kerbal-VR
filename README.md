# KerbalVR

An add-on for Kerbal Space Program (KSP) to enable the use of a virtual reality headset (HTC Vive, Windows MR, Oculus Rift, etc), as supported by OpenVR. Supports in-flight IVA, and room-scale VAB / SPH.

### FOLLOW the [Install Guide](https://github.com/Vivero/Kerbal-VR/wiki/Install-Guide), as there is a little bit more setup compared to other KSP mods.

----

**Built for KSP v1.6.0**

[Demonstration video](https://www.youtube.com/watch?v=DjQauN66rQA)

----

This is a WIP mod to allow the use of the HTC Vive (and any other HMD supported by the OpenVR SDK) in KSP. The primary focus is for use in IVA, and can also be used to walk around inside the VAB or SPH (room-scale).

IVA in VR puts you inside the cockpit of your craft. You can look around (and walk around if you have the physical space around you). Currently, it is possible to interact with cockpit instrumentation provided by the RPM mod (see note below); I am actively working on adding more immersive features for the cockpit (more **coming soon!**). The goal is to replace the use of the keyboard and mouse entirely with interactive cockpit controls (buttons, switches, control sticks, throttles, etc.)

Room-scale VR in the VAB and SPH allows you to walk around inside the building and see your craft at a 1:1 scale.

It is possible to get 90 FPS by following the **Performance Tips** below.

**You may experience random crashes while using the mod. You have been warned.**

VR will only work with KSP using Direct3D 12, i.e. you need to use the `-force-d3d12` flag on the executable. It will not work if you try to run KSP normally. To enable the Direct3D 12 flag, create a Shortcut to either `KSP.exe` or `KSP_x64.exe`, and on the shortcut Properties, append `-force-d3d12` to the Target. Follow the [Install Guide](https://github.com/Vivero/Kerbal-VR/wiki/Install-Guide) if you're having trouble.


## Requirements

- [Module Manager](https://forum.kerbalspaceprogram.com/index.php?/topic/50533-141-module-manager-307-may-5th-2018-its-dangerous-to-go-alone-take-those-cats-with-you/)


## Installation & Usage

For installation instructions, see the [Install Guide](https://github.com/Vivero/Kerbal-VR/wiki/Install-Guide).

For instructions on how to use the mod, see the [User Guide](https://github.com/Vivero/Kerbal-VR/wiki/User-Guide).

For guidance on compiling this project and other project documentation, see the [Build Guide](https://github.com/Vivero/Kerbal-VR/wiki/Build-Guide)


## Known Issues

Actually there's a lot of [issues](https://github.com/Vivero/Kerbal-VR/issues), but this one is noteworthy: when I run KSP with KerbalVR, the part icons in the VAB/SPH appear blue! Hwo to fix!!1??1???

Well this problem is not my fault, but there's a fix. You need to download [this fix](https://drive.google.com/file/d/1sb2_qyvBsBPrQFyGldK5x7uW2UJb14et/view), and place it in your `GameData` folder like any other mod. You can read more about it [here](https://github.com/Vivero/Kerbal-VR/issues/41) and [here](https://forum.kerbalspaceprogram.com/index.php?/topic/168795-electrocutors-thread/). 


## RasterProp Monitor

There is experimental support for VR interaction with RPM. See the [User Guide](https://github.com/Vivero/Kerbal-VR/wiki/User-Guide) for more details. Note that using RPM may hinder performance in VR (lower FPS).


## VR Cockpits

A set of VR-ready cockpits is provided by the [KVR Pods](https://github.com/Vivero/KVR-Pods) mod.


## Performance Tips

For best performance, go to the Settings menu in Kerbal Space Program, and under the Graphics tab, set:

- **Screen Resolution:** 1024 x 768
- **Full Screen:** unchecked
- **V-Sync:** Don't Sync
- **Frame Limit:** 100 FPS (or higher)

It is important to set V-Sync and Frame Limit as described above so that the Vive is able to render the game at 90 FPS. Experiment with the rest of the settings as you see fit.

The **Render Quality** seems to have a large impact on performance; if it is set too high, you may experience erratic flickering while in VR (see [issue 21](https://github.com/Vivero/Kerbal-VR/issues/21)).


## Tested System Configurations

I'm developing this mod with a Vive / Core i5-6600K / GTX 1070. Other systems have been tested by users, as described below:


### Hardware

| Headset     | Controllers        | OS        | CPU                          | GPU                  |
|-------------|--------------------|-----------|------------------------------|----------------------|
| HTC Vive    | Vive Wands         | Win10 x64 | Intel Core i5-4590           | GeForce GTX 770 x2   |
| Oculus Rift | Oculus Touch       |           | Intel Core i5-6600K (4.4GHz) | GeForce GTX 970      |
| Windows MR  | Motion Controllers |           | Intel Core i5-7300HQ         | GeForce GTX 1050Ti   |
|             |                    |           | Intel Core i7-4790K          | GeForce GTX 1070     |
|             |                    |           | Intel Core i7-6700K          | GeForce GTX 1080     |
|             |                    |           | Intel Core i7-8700K          | Radeon RX 480        |
|             |                    |           | AMD Ryzen 7                  |                      |

### Drivers

| Nvidia | SteamVR                 |
|--------|-------------------------|
| 417.35 | beta 1.2.3 (1545247019) |
