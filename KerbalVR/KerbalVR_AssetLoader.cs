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
        public static bool IsReady { get; private set; } = false;

        public GameObject glove;

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

            LoadAssets();

            IsReady = true;
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

        private void LoadAssets() {
            // get path to asset bundle
            string assetBundlePath = KSPUtil.ApplicationRootPath +
                "GameData/" + Globals.KERBALVR_ASSETS_DIR +
                "kerbalvr.ksp";
            Utils.Log("assetbundle path = " + assetBundlePath);

            // load asset bundle
            AssetBundle bundle = AssetBundle.LoadFromFile(assetBundlePath);
            if (bundle == null) {
                Utils.LogError("Error loading asset bundle from: " + assetBundlePath);
                return;
            }

            // enumerate assets
            string[] assetNames = bundle.GetAllAssetNames();
            for (int i = 0; i < assetNames.Length; i++) {
                Utils.Log("Asset: " + assetNames[i]);
            }

            /*glove = bundle.LoadAsset<GameObject>("assets/prefabs/glovel.prefab");
            if (glove != null) {
                DontDestroyOnLoad(glove);
                Utils.PrintGameObjectTree(glove);

                int numChildren = glove.transform.childCount;
                for (int i = 0; i < numChildren; i++) {
                    if (glove.transform.GetChild(i).name == "Zero_Gravity_Glove_L") {
                        GameObject glovemesh = glove.transform.GetChild(i).gameObject;
                        SkinnedMeshRenderer skinmesh = glovemesh.GetComponent<SkinnedMeshRenderer>();
                        skinmesh.material = new Material(Shader.Find("KSP/Diffuse"));
                        skinmesh.material.color = Color.cyan;

                        Utils.Log("skinmesh.sharedMesh = " + skinmesh.sharedMesh.name + ", " + skinmesh.sharedMesh.vertexCount);
                        Utils.Log("skinmesh.material = " + skinmesh.material);
                        break;
                    }
                }
            } else {
                Utils.LogError("glove null!");
            }*/
        }
    }
}
