# KerbalVR Developer Notes

These are a collection of notes aimed at developers working with the KerbalVR codebase.

## Organization

As of KerbalVR version 4.x.x, the structure of the codebase is organized into
4 projects.

- **KerbalVR_Mod**

  This is the Kerbal Space Program mod. A standard C# project that implements
  all of the mod features, structured like any other KSP mod.

- **KerbalVR_Renderer**

  This is a custom-built native plugin for Unity written in C++. It's sole
  purpose is to interface with Unity's multi-threaded Direct3D11 renderer.
  It receives a Unity RenderTexture and submits the texture to the OpenVR
  API, to be displayed on the VR headset. These OpenVR calls must happen on
  Unity's render thread, hence why we must use this plugin in the first place.
  This is the only available mechanism by which we can communicate with Unity's
  render thread to run custom code.

  This plugin is intentionally made very concise, very compact, with a single
  purpose: to submit images to the VR headset. It is best not to add any
  further functionality. KISS.

  Creating this plugin has averted the instability of trying to render
  images in the headset from the main Unity thread. Trying to submit images
  from the main thread causes endless game crashes (Access Violation errors)
  due to other D3D11 threads trying to read/write to the Camera RenderTextures.

  I just spent 3 paragraphs describing my baby. It's a beautiful plugin.
  Let me have this moment.

- **KerbalVR_Unity**

  This is a Unity project used to export the app toolbar UI assets, and other
  custom VR-related assets. It includes the KSP PartTools package to export
  asset bundles that are loaded by KSP at runtime.

- **KerbalVR_UnitySteamVR**

  This is a Unity project with the SteamVR project. Currently it is only used
  to facilitate exporting SteamVR input bindings.

## Compiling

- **KerbalVR_Mod**

  Open in **Visual Studio 2019**. It assumes that the directory
  *&lt;KerbalVR_root&gt;\KerbalVR_Mod\ksp_lib* exists and contains everything
  from the *&lt;KSP_root&gt;\KSP_x64_Data\Managed* folder. To compile,
  hit **Build > Build Solution**.

  This project automatically copies files into **C:\KSP_win64\GameData**.
  Modify the *KerbalVR.csproj* file if you want to change this behavior.

  If you clone the *KerbalVR* repo, you can build this project alone **without
  the having to first build the other projects**. The repo will contain the
  latest binaries generated from the other projects, including *KerbalVR_Renderer.dll*
  and other Unity asset bundles.

- **KerbalVR_Renderer**

  Open in **Visual Studio 2019**. It will automatically copy the output file
  *KerbalVR_Renderer.dll* into the appropriate directory in the *KerbalVR_Mod*
  project.

- **KerbalVR_Unity**

  Open in **Unity 2019.2.2f1**. Use *PartTools* to export the asset bundles
  for KSP. Run *&lt;KerbalVR_root&gt;\KerbalVR_Unity\export_asset_bundles.cmd*
  to copy the asset bundles from this project into the appropriate directory
  in the *KerbalVR_Mod* project.

- **KerbalVR_UnitySteamVR**

  Open in **Unity 2019.2.2f1**. Go to **Window > SteamVR Input**. Make any
  modifications to actions as needed. Click **Save and Generate**. To make
  changes to controller bindings, click **Open binding UI**. Edit the
  *Local Changes* configuration for a selected controller. When finished,
  click the **Replace Default Binding** button on the bottom-right.

  To export all the changes, run
  *&lt;KerbalVR_root&gt;\KerbalVR_UnitySteamVR\export_input_bindings.cmd*,
  which will place all the input bindings into the appropriate directory
  in the *KerbalVR_Mod* project.

## Other Thoughts

You may be wondering, hmm, why aren't the two Unity projects just one single
project? And I would argue, the *KerbalVR_Unity* project intentionally
excludes the SteamVR code, because when I export assets like the hand gloves
from the SteamVR package, I don't want it to export the C# scripts that are
associated with those assets, because KSP does not -- in fact -- include those
SteamVR scripts in the first place. So the asset will not correctly load at
runtime if it needs SteamVR scripts. Instead, I incorporate *some* of the
SteamVR scripts into the *KerbalVR_Mod* project, on an as-needed basis. You may
even notice that some of the *SteamVR* scripts in the *KerbalVR_Mod* have
been modified by me, because many of these scripts make assumptions about the
Unity project that we -- in the KSP world -- cannot make. For example, we cannot
use **SteamVR.cs** because it can only run under the assumption that the game's
Unity settings have enabled XR. The KSP Unity project does not in fact have
the XR setting enabled, because the game is not built for virtual reality
in the first place.

Are there a lot of redundant files in this project? Yea probably. There are like
three different subsets of the same SteamVR scripts among the various projects.
I say to you, whatever, this is my project, I do what I want. #kthxbye
