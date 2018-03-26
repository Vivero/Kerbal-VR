using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KerbalVR
{
    /// <summary>
    /// The AssetLoader plugin should load at the Main Menu,
    /// when all the asset bundles have been loaded.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class AssetLoader : MonoBehaviour
    {
        private FontLoader fontLoader;

        void Awake() {
            // keep this object around forever
            DontDestroyOnLoad(this);

            // load KerbalVR asset bundles
            LoadAssets();
        }

        private void LoadAssets() {

        }
    }
}
