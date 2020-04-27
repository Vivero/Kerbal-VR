using System.IO;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace KerbalVR
{
    /// <summary>
    /// Manage the loading of KerbalVR's asset bundles.
    /// </summary>
    public class AssetLoader : MonoBehaviour
    {
        #region Constants
        /// <summary>
        /// List of AssetBundle file paths to load.
        /// </summary>
        public static string[] KERBALVR_ASSET_BUNDLE_PATHS {
            get {
                string kvrAssetBundlesPath = Path.Combine(KSPUtil.ApplicationRootPath, "GameData", Globals.KERBALVR_ASSETBUNDLES_DIR);

                string[] assetBundlePaths = new string[1];
                assetBundlePaths[0] = Path.Combine(kvrAssetBundlesPath, "kerbalvr_ui.dat");
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
            gameObjectsDictionary = new Dictionary<string, GameObject>();
            fontsDictionary = new Dictionary<string, TMPro.TMP_FontAsset>();

            // load KerbalVR asset bundles
            LoadAssets();

            IsReady = true;
        }
        #endregion

        private void LoadAssets() {
            // load asset bundles
            foreach (var path in KERBALVR_ASSET_BUNDLE_PATHS) {
                AssetBundle bundle = AssetBundle.LoadFromFile(path);
                if (bundle == null) {
                    Utils.LogError("Error loading asset bundle from: " + path);
                } else {
                    // enumerate assets
                    string[] assetNames = bundle.GetAllAssetNames();
                    for (int i = 0; i < assetNames.Length; i++) {
                        string assetName = assetNames[i];

                        // find prefabs
                        if (assetName.EndsWith(".prefab")) {
                            GameObject assetGameObject = bundle.LoadAsset<GameObject>(assetName);
                            gameObjectsDictionary.Add(assetGameObject.name, assetGameObject);
                            Utils.Log("Loaded \"" + assetGameObject.name + "\" from \"" + assetName + "\"");
                        }
                    }
                }
            }
        }

        public GameObject GetGameObject(string gameObjectName) {
            if (gameObjectsDictionary.TryGetValue(gameObjectName, out GameObject obj)) {
                return obj;
            }
            return null;
        }
    }
}
