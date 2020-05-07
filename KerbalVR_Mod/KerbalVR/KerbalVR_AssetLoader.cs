using System.IO;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace KerbalVR
{
    /// <summary>
    /// Manage KerbalVR's asset bundles.
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

                string[] assetBundlePaths = {
                    Path.Combine(kvrAssetBundlesPath, "kerbalvr.dat"),
                    Path.Combine(kvrAssetBundlesPath, "kerbalvr_ui.dat"),
                };
                return assetBundlePaths;
            }
        }
        #endregion

        #region Properties
        /// <summary>
        /// Set to true when all asset bundles have been loaded.
        /// </summary>
        public static bool IsReady { get; private set; } = false;
        #endregion

        #region Private Members
        protected Dictionary<string, GameObject> gameObjectsDictionary;
        #endregion

        #region Singleton
        /// <summary>
        /// This is a singleton class, and there must be exactly one GameObject with this Component in the scene.
        /// </summary>
        protected static AssetLoader _instance;
        public static AssetLoader Instance {
            get {
                if (_instance == null) {
                    _instance = FindObjectOfType<AssetLoader>();
                    if (_instance == null) {
                        Utils.LogError("The scene needs to have one active GameObject with an AssetLoader script attached!");
                    } else {
                        _instance.Initialize();
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// One-time initialization for this singleton class.
        /// </summary>
        protected void Initialize() {
            gameObjectsDictionary = new Dictionary<string, GameObject>();

            // load KerbalVR asset bundles
            LoadAssets();

            IsReady = true;
        }
        #endregion

        /// <summary>
        /// Load prefabs from asset bundles.
        /// </summary>
        protected void LoadAssets() {
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

        /// <summary>
        /// Get an asset that was loaded from an asset bundle.
        /// </summary>
        /// <param name="gameObjectName">Name of the asset</param>
        /// <returns></returns>
        public GameObject GetGameObject(string gameObjectName) {
            if (gameObjectsDictionary.TryGetValue(gameObjectName, out GameObject obj)) {
                return obj;
            }
            return null;
        }
    }
}
