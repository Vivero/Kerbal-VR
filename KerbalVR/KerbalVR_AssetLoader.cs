using System.IO;
using System.Collections.Generic;
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
        #region Constants
        /// <summary>
        /// Full path to the KerbalVR AssetBundle file.
        /// </summary>
        public static string KERBALVR_ASSET_BUNDLE_PATH {
            get {
                string gameDataPath = Path.Combine(KSPUtil.ApplicationRootPath, "GameData");
                string kvrAssetsPath = Path.Combine(gameDataPath, Globals.KERBALVR_ASSETS_DIR);
                return Path.Combine(kvrAssetsPath, "kerbalvr.ksp");
            }
        }
        #endregion

        public static bool IsReady { get; private set; } = false;

        private Dictionary<string, GameObject> gameObjectsDictionary;
        private Dictionary<string, TMPro.TMP_FontAsset> fontsDictionary;

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
            // Utils.Log("Found " + fonts.Length + " fonts");
            for (int i = 0; i < fonts.Length; i++) {
                TMPro.TMP_FontAsset font = fonts[i];
                fontsDictionary.Add(font.name, font);
            }
        }

        private void LoadAssets() {
            // load asset bundle
            AssetBundle bundle = AssetBundle.LoadFromFile(KERBALVR_ASSET_BUNDLE_PATH);
            if (bundle == null) {
                Utils.LogError("Error loading asset bundle from: " + KERBALVR_ASSET_BUNDLE_PATH);
                return;
            }

            // enumerate assets
            string[] assetNames = bundle.GetAllAssetNames();
            for (int i = 0; i < assetNames.Length; i++) {
                string assetName = assetNames[i];

                // find prefabs
                if (assetName.EndsWith(".prefab")) {
                    Utils.Log("Loading \"" + assetName + "\"");
                    GameObject assetGameObject = bundle.LoadAsset<GameObject>(assetName);

                    Utils.Log("assetGameObject.name = " + assetGameObject.name);
                    gameObjectsDictionary.Add(assetGameObject.name, assetGameObject);
                }
            }
        }

        public TMPro.TMP_FontAsset GetFont(string fontName) {
            TMPro.TMP_FontAsset font = null;
            if (fontsDictionary.TryGetValue(fontName, out font)) {
                return font;
            }
            return null;
        }

        public GameObject GetGameObject(string gameObjectName) {
            GameObject obj = null;
            if (gameObjectsDictionary.TryGetValue(gameObjectName, out obj)) {
                return obj;
            }
            return null;
        }
    }
}
