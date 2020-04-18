# KerbalVR

An add-on for Kerbal Space Program (KSP) to enable the use of a virtual reality headset
(HTC Vive, Valve Index, Windows MR, Oculus Rift, etc), as supported by OpenVR.
Supports in-flight IVA, and room-scale VAB / SPH.

### FOLLOW the [Install Guide](https://github.com/Vivero/Kerbal-VR/wiki/Install-Guide), as there is a little bit more setup compared to other KSP mods.

----

**Built for KSP v1.9.1**

[Demonstration video](https://www.youtube.com/watch?v=DjQauN66rQA)

----

This is a forever-WIP mod to allow the use of VR headsets in KSP. The primary focus is
for flight control in IVA, but can also be used for room-scale viewing inside the VAB or SPH.

IVA in VR puts you inside the cockpit of your craft. You can look around (and walk around if
you have the physical space around you). Currently, it is possible to interact with cockpit
instrumentation provided by the RPM mod (see note below); more immersive features for the
cockpit may come sometime in the future. The goal is to replace the use of the keyboard and
mouse entirely with interactive cockpit controls (buttons, switches, control sticks, throttles, etc.)

Room-scale VR in the VAB and SPH allows you to walk around inside the building and see your craft at a 1:1 scale.

Performance will vary wildly between setups. There is an infinite combination of PC specs,
video cards, VR rendering resolutions, headset refresh rates, etc. It is difficult to achieve
a smooth experience, but you may try the **Performance Tips** below.

**You may experience random crashes while using the mod. You have been warned.**

## Requirements

- [Module Manager](https://forum.kerbalspaceprogram.com/index.php?/topic/50533-140-16x-module-manager-402-february-3rd-2019-right-to-ludicrous-speed/)


## Installation & Usage

For installation instructions, see the [Install Guide](https://github.com/Vivero/Kerbal-VR/wiki/Install-Guide).

For instructions on how to use the mod, see the [User Guide](https://github.com/Vivero/Kerbal-VR/wiki/User-Guide).

For guidance on compiling this project and other project documentation, see the [Build Guide](https://github.com/Vivero/Kerbal-VR/wiki/Build-Guide)


## Known Issues

- *There's lots of stuttering while in VR!*

  Yea I dunno what to do about that. No guarantees, but try the **Performance Tips** below. This game
  wasn't designed with VR in mind, and there isn't a whole lot I can do from a modding perspective.

- *When I enable VR in the VAB/SPH, everything looks blue!*

  Yep. Dunno why. Have fun!


## RasterProp Monitor

There is experimental support for VR interaction with RPM. See the [User Guide](https://github.com/Vivero/Kerbal-VR/wiki/User-Guide) for more details. Note that using RPM may hinder performance in VR (lower FPS).


## VR Cockpits

A set of VR-ready cockpits is provided by the [KVR Pods](https://github.com/Vivero/KVR-Pods) mod.


## Performance Tips

You may need to tone down the graphics for best performance. Try these in the Settings menu, under the Graphics tab:

- **Screen Resolution:** 1024 x 768
- **Full Screen:** unchecked
- **V-Sync:** Don't Sync
- **Frame Limit:** 180 FPS

Something that seemed to help a lot was turning on **Legacy Reprojection Mode** in the SteamVR
per-application settings for Kerbal Space Program.

![SteamVR Settings](https://imgur.com/LqTbD2u.png)

Make sure you update your graphics drivers and SteamVR.
