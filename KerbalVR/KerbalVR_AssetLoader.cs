using System.IO;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace KerbalVR
{
    /// <summary>
    /// The AssetLoader plugin should load at the Main Menu,
    /// when all the asset bundles have been loaded.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class AssetLoader : MonoBehaviour
    {
        #region Constants
        /// <summary>
        /// Full path to the KerbalVR AssetBundle file.
        /// </summary>
        public static string[] KERBALVR_ASSET_BUNDLE_PATHS {
            get {
                string kvrAssetBundlesPath = Path.Combine(KSPUtil.ApplicationRootPath, "GameData", Globals.KERBALVR_ASSETBUNDLES_DIR);

                string[] assetBundlePaths = new string[2];
                assetBundlePaths[0] = Path.Combine(kvrAssetBundlesPath, "kerbalvr.ksp");
                assetBundlePaths[1] = Path.Combine(kvrAssetBundlesPath, "kerbalvr_ui.dat");
                return assetBundlePaths;
            }
        }
        #endregion

        public static bool IsReady { get; private set; } = false;

        private Dictionary<string, GameObject> gameObjectsDictionary;
        private Dictionary<string, TMPro.TMP_FontAsset> fontsDictionary;

        #region Singleton
        // this is a singleton class, and there must be one AssetLoader in the scene
        private static AssetLoader _instance;
        public static AssetLoader Instance {
            get {
                if (_instance == null) {
                    _instance = FindObjectOfType<AssetLoader>();
                    if (_instance != null) {
                        _instance.Initialize();
                    }
                }
                return _instance;
            }
        }

        // first-time initialization for this singleton class
        private void Initialize() {
            if (gameObjectsDictionary == null) {
                gameObjectsDictionary = new Dictionary<string, GameObject>();
            }
            if (fontsDictionary == null) {
                fontsDictionary = new Dictionary<string, TMPro.TMP_FontAsset>();
            }
        }
        #endregion

        void Awake() {
            // keep this object around forever
            DontDestroyOnLoad(this);

            Initialize();

            // load KerbalVR asset bundles
            LoadFonts();
            LoadAssets();

            IsReady = true;
        }

        private void LoadFonts() {
            TMPro.TMP_FontAsset[] fonts = Resources.FindObjectsOfTypeAll(typeof(TMPro.TMP_FontAsset)) as TMPro.TMP_FontAsset[];

            for (int i = 0; i < fonts.Length; i++) {
                TMPro.TMP_FontAsset font = fonts[i];
                fontsDictionary.Add(font.name, font);
            }
        }

        private void LoadAssets() {
            // load asset bundles
            foreach (var path in KERBALVR_ASSET_BUNDLE_PATHS) {
                AssetBundle bundle = AssetBundle.LoadFromFile(path);
                if (bundle == null) {
                    Utils.LogError("Error loading asset bundle from: " + path);
                    return;
                }

                // enumerate assets
                Utils.Log("Inspecting asset bundle: " + path);
                string[] assetNames = bundle.GetAllAssetNames();
                for (int i = 0; i < assetNames.Length; i++) {
                    string assetName = assetNames[i];

                    // find prefabs
                    if (assetName.EndsWith(".prefab")) {
                        Utils.Log("Loading \"" + assetName + "\"");
                        GameObject assetGameObject = bundle.LoadAsset<GameObject>(assetName);
                        gameObjectsDictionary.Add(assetGameObject.name, assetGameObject);
                    }
                }
            }
        }

        public TMPro.TMP_FontAsset GetFont(string fontName) {
            if (fontsDictionary.TryGetValue(fontName, out TMPro.TMP_FontAsset font)) {
                return font;
            }
            return null;
        }

        public GameObject GetGameObject(string gameObjectName) {
            if (gameObjectsDictionary.TryGetValue(gameObjectName, out GameObject obj)) {
                return obj;
            }
            return null;
        }
    }
}
