using System;
using System.IO;
using UnityEngine;

namespace KerbalVR
{
    public class Configuration : MonoBehaviour
    {
        #region Constants
        /// <summary>
        /// Full path to the KerbalVR configuration settings file.
        /// </summary>
        public static string KERBALVR_SETTINGS_PATH {
            get {
                string gameDataPath = Path.Combine(KSPUtil.ApplicationRootPath, "GameData");
                string kvrAssetsPath = Path.Combine(gameDataPath, Globals.KERBALVR_ASSETS_DIR);
                return Path.Combine(kvrAssetsPath, "Settings.json");
            }
        }
        #endregion


        #region Properties
        private bool _initOpenVrAtStartup;
        public bool InitOpenVrAtStartup {
            get {
                return _initOpenVrAtStartup;
            }
            set {
                _initOpenVrAtStartup = value;
                SaveSettings();
            }
        }
        #endregion


        #region Private Members
        #endregion


        #region Singleton
        // this is a singleton class, and there must be one Configuration in the scene
        private static Configuration _instance;
        public static Configuration Instance {
            get {
                if (_instance == null) {
                    _instance = FindObjectOfType<Configuration>();
                    if (_instance == null) {
                        Utils.LogError("The scene needs to have one active GameObject with a Configuration script attached!");
                    } else {
                        _instance.Initialize();
                    }
                }
                return _instance;
            }
        }

        // first-time initialization for this singleton class
        private void Initialize() {

            if (File.Exists(KERBALVR_SETTINGS_PATH)) {
                // load the settings file if it exists
                string kvrSettingsText = File.ReadAllText(KERBALVR_SETTINGS_PATH);
                Settings kvrSettings = JsonUtility.FromJson<Settings>(kvrSettingsText);

                // store the settings
                this._initOpenVrAtStartup = kvrSettings.initOpenVrAtStartup;

#if DEBUG
                Utils.Log("Loaded Configuration:");
                Utils.Log("initOpenVrAtStartup = " + kvrSettings.initOpenVrAtStartup);
#endif

            } else {
                // if no settings file exists, create a default one
                Settings kvrSettings = new Settings();
                string kvrSettingsText = JsonUtility.ToJson(kvrSettings, true);

                // write to file
                File.WriteAllText(KERBALVR_SETTINGS_PATH, kvrSettingsText);
            }
        }
#endregion

        private void SaveSettings() {
            Settings kvrSettings = new Settings();
            kvrSettings.initOpenVrAtStartup = this.InitOpenVrAtStartup;

            // write to file
            string kvrSettingsText = JsonUtility.ToJson(kvrSettings, true);
            File.WriteAllText(KERBALVR_SETTINGS_PATH, kvrSettingsText);
        }
    }

    [Serializable]
    public class Settings {
        public bool initOpenVrAtStartup = false;
    }
}
