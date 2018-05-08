# KerbalVR

An add-on for Kerbal Space Program (KSP) to enable the use of a virtual reality headset (HTC Vive, Windows MR, Oculus Rift, etc), as supported by OpenVR. Supports in-flight IVA, and room-scale VAB / SPH.

### FOLLOW the [Install Guide](https://github.com/Vivero/Kerbal-VR/wiki/Install-Guide), as there is a little bit more setup compared to other KSP mods.

----

**Built for KSP v1.4.2**

[Demonstration video](https://www.youtube.com/watch?v=DjQauN66rQA)

----

This is an early WIP mod to allow the use of the HTC Vive (and potentially any HMD supported by the OpenVR SDK) in KSP. The primary focus is for use in IVA, and can also be used to walk around inside the VAB or SPH (room-scale).

IVA in VR puts you inside the cockpit of your craft. You can look around (and walk around if you have the physical space around you). Currently, it is possible to interact with cockpit instrumentation provided by the RPM mod (see note below); I am actively working on adding more immersive features for the cockpit (more **coming soon!**). The goal is to replace the use of the keyboard and mouse entirely with interactive cockpit controls (buttons, switches, control sticks, throttles, etc.)

Room-scale VR in the VAB and SPH allows you to walk around inside the building and see your craft at a 1:1 scale.

It is possible to get 90 FPS by following the **Performance Tips** below.

**You may experience random crashes while using the mod. You have been warned.**

VR will only work with KSP using Direct3D 12, i.e. you need to use the `-force-d3d12` flag on the executable. It will not work if you try to run KSP normally. To enable the Direct3D 12 flag, create a Shortcut to either `KSP.exe` or `KSP_x64.exe`, and on the shortcut Properties, append `-force-d3d12` to the Target. Follow the [Install Guide](https://github.com/Vivero/Kerbal-VR/wiki/Install-Guide) if you're having trouble.


## Installation & Usage

For installation instructions, see the [Install Guide](https://github.com/Vivero/Kerbal-VR/wiki/Install-Guide).

For instructions on how to use the mod, see the [User Guide](https://github.com/Vivero/Kerbal-VR/wiki/User-Guide).

For guidance on compiling this project and other project documentation, see the [Build Guide](https://github.com/Vivero/Kerbal-VR/wiki/Build-Guide).

**NOTE:** When VR is enabled, the game screen on your PC will appear locked up. This is by design and intended to improve the performance (higher FPS). Once you disable VR, the regular game screen *should* return to normal.


## RasterProp Monitor

There is experimental support for VR interaction with RPM. See the [User Guide](https://github.com/Vivero/Kerbal-VR/wiki/User-Guide) for more details. Note that using RPM may hinder performance in VR (lower FPS).


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

| Headset     | Controllers  | OS        | CPU                          | GPU                  |
|-------------|--------------|-----------|------------------------------|----------------------|
| HTC Vive    | Vive Wands   | Win10 x64 | Intel Core i5-4590           | GeForce GTX 970      |
| Oculus Rift | Oculus Touch |           | Intel Core i5-6600K (4.4GHz) | GeForce GTX 1070 8GB |
| Windows MR  |              |           | Intel Core i7-4790K          | GeForce GTX 1080     |
|             |              |           | Intel Core i7-6700K          | Radeon RX 480        |

### Drivers

| Nvidia | SteamVR         |
|--------|-----------------|
| 391.35 | beta 1523560849 |
