using System;
using System.IO;
using UnityEngine;

namespace KerbalVR
{
    /// <summary>
    /// Manage global configuration settings for KerbalVR.
    /// Reads/writes settings to/from a text file.
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
        protected bool _initOpenVrAtStartup;
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
        /// If true, the flight stick operates pitch and yaw,
        /// instead of pitch and roll.
        /// </summary>
        protected bool _swapYawRollControls;
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
        /// Enable debugging tools
        /// </summary>
        protected bool _debugEnabled;
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
        /// <summary>
        /// This is a singleton class, and there must be exactly one GameObject with this Component in the scene.
        /// </summary>
        protected static Configuration _instance;
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

        /// <summary>
        /// One-time initialization for this singleton class.
        /// </summary>
        protected void Initialize() {

            if (File.Exists(KERBALVR_SETTINGS_PATH)) {
                // load the settings file if it exists
                string kvrSettingsText = File.ReadAllText(KERBALVR_SETTINGS_PATH);
                Settings kvrSettings = JsonUtility.FromJson<Settings>(kvrSettingsText);

                // store the settings from file
                this._initOpenVrAtStartup = kvrSettings.initOpenVrAtStartup;
                this._swapYawRollControls = kvrSettings.swapYawRollControls;
#if DEBUG
                this._debugEnabled = true;
#else
                this._debugEnabled = kvrSettings.debugEnabled;
#endif

            } else {
                // if no settings file exists, create a default one
                Settings kvrSettings = new Settings();
                string kvrSettingsText = JsonUtility.ToJson(kvrSettings, true);

                // write to file
                File.WriteAllText(KERBALVR_SETTINGS_PATH, kvrSettingsText);
            }
        }

        /// <summary>
        /// Save settings to a JSON file on-disk.
        /// </summary>
        protected void SaveSettings() {
            Settings kvrSettings = new Settings();
            kvrSettings.initOpenVrAtStartup = this.InitOpenVrAtStartup;
            kvrSettings.swapYawRollControls = this.SwapYawRollControls;
            kvrSettings.debugEnabled = this.DebugEnabled;

            // write to file
            string kvrSettingsText = JsonUtility.ToJson(kvrSettings, true);
            File.WriteAllText(KERBALVR_SETTINGS_PATH, kvrSettingsText);
        }
    }

    /// <summary>
    /// A serializable class to contain KerbalVR configuration settings.
    /// </summary>
    [Serializable]
    public class Settings {
        public bool initOpenVrAtStartup = true;
        public bool swapYawRollControls = false;
#if DEBUG
        public bool debugEnabled = true;
#else
        public bool debugEnabled = false;
#endif
    }
}
