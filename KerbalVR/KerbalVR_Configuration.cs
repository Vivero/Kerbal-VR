using System;
using System.IO;
using UnityEngine;

namespace KerbalVR
{
    /// <summary>
    /// A class to manage global configuration settings for KerbalVR.
    /// </summary>
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
        /// <summary>
        /// If true, initializes OpenVR as soon as KSP starts. Otherwise, OpenVR
        /// initializes on the first time VR is enabled.
        /// </summary>
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

        /// <summary>
        /// If true, the control stick controls craft pitch and yaw, instead of
        /// pitch and roll.
        /// </summary>
        private bool _swapYawRollControls;
        public bool SwapYawRollControls {
            get {
                return _swapYawRollControls;
            }
            set {
                _swapYawRollControls = value;
                SaveSettings();
            }
        }

        /// <summary>
        /// Scale of the world while in VR
        /// </summary>
        private float _worldScale;
        public float WorldScale {
            get {
                return _worldScale;
            }
            set {
                _worldScale = value;
                SaveSettings();
            }
        }

        /// <summary>
        /// Enable debugging tools
        /// </summary>
        private bool _debugEnabled;
        public bool DebugEnabled {
            get {
                return _debugEnabled;
            }
            set {
                _debugEnabled = value;
                SaveSettings();
            }
        }
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
        #endregion

        // first-time initialization for this singleton class
        private void Initialize() {

            if (File.Exists(KERBALVR_SETTINGS_PATH)) {
                // load the settings file if it exists
                string kvrSettingsText = File.ReadAllText(KERBALVR_SETTINGS_PATH);
                Settings kvrSettings = JsonUtility.FromJson<Settings>(kvrSettingsText);

                // store the settings from file
                this._initOpenVrAtStartup = kvrSettings.initOpenVrAtStartup;
                this._swapYawRollControls = kvrSettings.swapYawRollControls;
                this._worldScale = kvrSettings.worldScale;

            } else {
                // if no settings file exists, create a default one
                Settings kvrSettings = new Settings();
                string kvrSettingsText = JsonUtility.ToJson(kvrSettings, true);

                // write to file
                File.WriteAllText(KERBALVR_SETTINGS_PATH, kvrSettingsText);
            }
        }

        private void SaveSettings() {
            Settings kvrSettings = new Settings();
            kvrSettings.initOpenVrAtStartup = this.InitOpenVrAtStartup;
            kvrSettings.swapYawRollControls = this.SwapYawRollControls;
            kvrSettings.worldScale = this.WorldScale;
            kvrSettings.debugEnabled = this.DebugEnabled;

            // write to file
            string kvrSettingsText = JsonUtility.ToJson(kvrSettings, true);
            File.WriteAllText(KERBALVR_SETTINGS_PATH, kvrSettingsText);
        }
    }

    /// <summary>
    /// A serializable class to contain KerbalVR settings.
    /// </summary>
    [Serializable]
    public class Settings {
        public bool initOpenVrAtStartup = true;
        public bool swapYawRollControls = false;
        public float worldScale = 1f;
        public bool debugEnabled = true;
    }
}
