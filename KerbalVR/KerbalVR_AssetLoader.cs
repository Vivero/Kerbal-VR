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
        private Dictionary<string, TMPro.TMP_FontAsset> fontDictionary;

        #region Singleton
        // this is a singleton class, and there must be one DeviceManager in the scene
        private static AssetLoader _instance;
        public static AssetLoader Instance {
            get {
                if (_instance == null) {
                    _instance = FindObjectOfType<AssetLoader>();
                    if (_instance == null) {
                        Utils.LogError("The scene needs to have one active GameObject with a AssetLoader script attached!");
                    } else {
                        _instance.Initialize();
                    }
                }
                return _instance;
            }
        }

        // first-time initialization for this singleton class
        private void Initialize() {
            if (fontDictionary == null) {
                fontDictionary = new Dictionary<string, TMPro.TMP_FontAsset>();
            }
        }
        #endregion

        void Awake() {
            // keep this object around forever
            DontDestroyOnLoad(this);

            Initialize();

            // load KerbalVR asset bundles
            LoadFonts();
        }

        private void LoadFonts() {
            TMPro.TMP_FontAsset[] fonts = Resources.FindObjectsOfTypeAll(typeof(TMPro.TMP_FontAsset)) as TMPro.TMP_FontAsset[];
            // Utils.Log("Found " + fonts.Length + " fonts");
            for (int i = 0; i < fonts.Length; i++) {
                TMPro.TMP_FontAsset font = fonts[i];
                fontDictionary.Add(font.name, font);
            }
        }

        public TMPro.TMP_FontAsset GetFont(string fontName) {
            TMPro.TMP_FontAsset font = null;
            if (fontDictionary.TryGetValue(fontName, out font)) {
                return font;
            }
            return null;
        }
    }
}
