using UnityEngine;
using KSP.UI.Screens;
using System.Text.RegularExpressions;

namespace KerbalVR
{
    public class KerbalVR_GUI
    {
        // CONSTANTS
        //
        public static string AppButtonLogo {
            get {
                return Utils.KERBALVR_ASSETS_DIR + "app_button_logo";
            }
        }

        private static string BUTTON_STRING_ENABLE_VR = "Enable VR";
        private static string BUTTON_STRING_DISABLE_VR = "Disable VR";

        private static string LABEL_STRING_VR_ACTIVE = "ACTIVE";
        private static string LABEL_STRING_VR_INACTIVE = "INACTIVE";

        private static readonly int APP_GUI_ID = 186012;

        // store the interface to the KerbalVR plugin
        private KerbalVR_Plugin kerbalVr;

        private ApplicationLauncherButton appButton;
        private bool appButtonGuiActive = false;

        private Rect appGuiWindowRect = new Rect(Screen.width / 4, Screen.height / 4, 160, 140);


        public KerbalVR_GUI(KerbalVR_Plugin kerbalVr) {
            this.kerbalVr = kerbalVr;
        }

        /// <summary>
        /// This GameEvent is registered with GameEvents.onGUIApplicationLauncherReady,
        /// at the time the plugin is loaded. It instantiates a new button on the
        /// application launcher.
        /// </summary>
        public void OnAppLauncherReady() {
            // define where should the app button be visible
            ApplicationLauncher.AppScenes appVisibility =
                ApplicationLauncher.AppScenes.SPACECENTER |
                ApplicationLauncher.AppScenes.FLIGHT |
                ApplicationLauncher.AppScenes.MAPVIEW |
                ApplicationLauncher.AppScenes.VAB |
                ApplicationLauncher.AppScenes.SPH |
                ApplicationLauncher.AppScenes.TRACKSTATION;
            
            // create new app button instance
            if (HighLogic.LoadedScene == GameScenes.SPACECENTER &&
                appButton == null) {
                appButton = ApplicationLauncher.Instance.AddModApplication(
                    OnToggleTrue,
                    OnToggleFalse,
                    null, null, null, null,
                    appVisibility,
                    GameDatabase.Instance.GetTexture(AppButtonLogo, false));
            }

            // GUI is off at instantiation
            appButtonGuiActive = false;
        }

        /// <summary>
        /// This GameEvent is registered with GameEvents.onGUIApplicationLauncherDestroyed,
        /// at the time the plugin is loaded. It destroys the application button on the
        /// application launcher.
        /// </summary>
        public void OnAppLauncherDestroyed() {
            if (appButton != null) {
                ApplicationLauncher.Instance.RemoveApplication(appButton);
            }
        }

        /// <summary>
        /// Callback when the application button is toggled on.
        /// </summary>
        public void OnToggleTrue() {
            appButtonGuiActive = true;
        }

        /// <summary>
        /// Callback when the application button is toggled off.
        /// </summary>
        public void OnToggleFalse() {
            appButtonGuiActive = false;
        }

        public void OnGUI() {
            if (appButtonGuiActive) {
                appGuiWindowRect = GUILayout.Window(
                    APP_GUI_ID,
                    appGuiWindowRect,
                    GenerateGUI,
                    "KerbalVR",
                    HighLogic.Skin.window);
            }
        }

        //int poseDelayMS = 0;

        private void GenerateGUI(int windowId) {
            string buttonStringToggleVr = BUTTON_STRING_ENABLE_VR;
            string labelStringVrActive = LABEL_STRING_VR_INACTIVE;
            GUIStyle labelStyleVrActive = new GUIStyle(HighLogic.Skin.label);
            labelStyleVrActive.normal.textColor = Color.red;

            if (kerbalVr.HmdIsEnabled) {
                buttonStringToggleVr = BUTTON_STRING_DISABLE_VR;
                labelStringVrActive = LABEL_STRING_VR_ACTIVE;
                labelStyleVrActive.normal.textColor = Color.green;
            }

            GUILayout.BeginVertical();

            // VR toggle button
            GUI.enabled = kerbalVr.HmdIsAllowed;
            if (GUILayout.Button(buttonStringToggleVr, HighLogic.Skin.button)) {
                if (kerbalVr.HmdIsEnabled) {
                    kerbalVr.HmdIsEnabled = false;
                } else {
                    kerbalVr.HmdIsEnabled = true;
                }
            }

            if (GUILayout.Button("Reset Headset Position", HighLogic.Skin.button)) {
                kerbalVr.ResetInitialHmdPosition();
            }
            GUI.enabled = true;

            // VR status
            GUILayout.BeginHorizontal();
            GUILayout.Label("VR Status:", HighLogic.Skin.label);
            GUILayout.Label(labelStringVrActive, labelStyleVrActive);
            GUILayout.EndHorizontal();

            // settings
            GUIStyle labelStyleHeader = new GUIStyle(HighLogic.Skin.label);
            labelStyleHeader.fontStyle = FontStyle.Bold;
            GUILayout.Label("Options", labelStyleHeader);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Pose Delay (ms):", HighLogic.Skin.label);
            int poseDelayMS = (int)(kerbalVr.PoseDelay * 1000f);
            string poseDelayStr = poseDelayMS.ToString();
            poseDelayStr = GUILayout.TextField(poseDelayStr, HighLogic.Skin.textField);
            if (GUI.changed) {
                if (System.Int32.TryParse(poseDelayStr, out poseDelayMS)) {
                    kerbalVr.PoseDelay = poseDelayMS * 0.001f;
                } else {
                    kerbalVr.PoseDelay = 0f;
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            // allow dragging the window
            GUI.DragWindow();
        }
    }
}
