using UnityEngine;
using KSP.UI.Screens;
using System.Text.RegularExpressions;
using System;

namespace KerbalVR
{
    public class AppGUI
    {
        private class TextFieldFloat {
            private string parameterName;
            private Action<float> parameterCallback;
            private string valueStr;

            public TextFieldFloat(string parameterName, float parameter, Action<float> parameterCallback) {
                this.parameterName = parameterName;
                this.parameterCallback = parameterCallback;
                valueStr = parameter.ToString("F3");
            }

            public void UpdateGUI() {
                GUILayout.BeginHorizontal();
                GUILayout.Label(parameterName + ":", HighLogic.Skin.label);
                valueStr = GUILayout.TextField(valueStr, HighLogic.Skin.textField);
                if (GUI.changed) {
                    bool parseSuccess = System.Single.TryParse(valueStr, out float updatedValue);
                    if (parseSuccess) {
                        parameterCallback(updatedValue);
                    }
                }
                GUILayout.EndHorizontal();
            }
        }

        #region Constants
        public static string AppButtonLogo {
            get {
                return Globals.KERBALVR_ASSETS_DIR + "app_button_logo";
            }
        }

        public static bool SceneAllowsAppGUI {
            get {
                return (
#if DEBUG
                    (HighLogic.LoadedScene == GameScenes.MAINMENU) ||
                    (HighLogic.LoadedScene == GameScenes.SPACECENTER) ||
                    (HighLogic.LoadedScene == GameScenes.TRACKSTATION) ||
#endif
                    (HighLogic.LoadedScene == GameScenes.FLIGHT) ||
                    (HighLogic.LoadedScene == GameScenes.EDITOR));
            }
        }

#if DEBUG
        private static readonly ApplicationLauncher.AppScenes APP_VISIBILITY = ApplicationLauncher.AppScenes.ALWAYS;
#else
        private static readonly ApplicationLauncher.AppScenes APP_VISIBILITY =
            ApplicationLauncher.AppScenes.FLIGHT |
            ApplicationLauncher.AppScenes.VAB |
            ApplicationLauncher.AppScenes.SPH;
#endif

        private static string BUTTON_STRING_ENABLE_VR = "Enable VR";
        private static string BUTTON_STRING_DISABLE_VR = "Disable VR";

        private static string LABEL_STRING_VR_ACTIVE = "ACTIVE";
        private static string LABEL_STRING_VR_INACTIVE = "INACTIVE";

        private static string BUTTON_STRING_ENABLE_MIRROR = "Enable Display Mirror";
        private static string BUTTON_STRING_DISABLE_MIRROR = "Disable Display Mirror";

        private static readonly int APP_GUI_ID = 186012;

        #endregion

        private ApplicationLauncherButton appButton;
        private bool appButtonGuiActive = false;
        private bool appButtonGuiActiveLastState = false;

        private Rect appGuiWindowRect = new Rect(Screen.width / 4, Screen.height / 4, 160, 100);

        // text fields
        private string worldScaleStr;


        /// <summary>
        /// This GameEvent is registered with GameEvents.onGUIApplicationLauncherReady,
        /// at the time the plugin is loaded. It instantiates a new button on the
        /// application launcher. Note that this callback can be called multiple times
        /// throughout the game.
        /// </summary>
        public void OnAppLauncherReady() {
            // define where should the app button be visible

            /*
            ApplicationLauncher.AppScenes appVisibility =
                ApplicationLauncher.AppScenes.SPACECENTER |
                ApplicationLauncher.AppScenes.FLIGHT |
                ApplicationLauncher.AppScenes.MAPVIEW |
                ApplicationLauncher.AppScenes.VAB |
                ApplicationLauncher.AppScenes.SPH |
                ApplicationLauncher.AppScenes.TRACKSTATION;
            */

            // create new app button instance if it doesn't already exist
            if (appButton == null) {

                // init variables
                worldScaleStr = Scene.Instance.WorldScale.ToString("F1");

                appButton = ApplicationLauncher.Instance.AddModApplication(
                    OnToggleTrue,
                    OnToggleFalse,
                    null, null, null, null,
                    APP_VISIBILITY,
                    GameDatabase.Instance.GetTexture(AppButtonLogo, false));

                // GUI is off at instantiation
                appButtonGuiActive = false;

                // register callbacks when AppLauncher shows/hides (i.e. during loading screens)
                ApplicationLauncher.Instance.AddOnShowCallback(OnShow);
                ApplicationLauncher.Instance.AddOnHideCallback(OnHide);
            }
        }

        void OnShow() {
            appButtonGuiActive = appButtonGuiActiveLastState;
        }

        void OnHide() {
            appButtonGuiActiveLastState = appButtonGuiActive;
            appButtonGuiActive = false;
        }

        /// <summary>
        /// This GameEvent is registered with GameEvents.onGUIApplicationLauncherDestroyed,
        /// at the time the plugin is loaded. It destroys the application button on the
        /// application launcher.
        /// </summary>
        public void OnAppLauncherDestroyed() {
            if (appButton != null) {
                OnToggleFalse();
                ApplicationLauncher.Instance.RemoveApplication(appButton);
                ApplicationLauncher.Instance.RemoveOnShowCallback(OnShow);
                ApplicationLauncher.Instance.RemoveOnHideCallback(OnHide);
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
            if (SceneAllowsAppGUI && appButtonGuiActive) {
                appGuiWindowRect = GUILayout.Window(
                    APP_GUI_ID,
                    appGuiWindowRect,
                    GenerateGUI,
                    Globals.KERBALVR_NAME,
                    HighLogic.Skin.window);
            }
        }

        private void GenerateGUI(int windowId) {
            string buttonStringToggleVr = BUTTON_STRING_ENABLE_VR;
            string labelStringVrActive = LABEL_STRING_VR_INACTIVE;
            string buttonStringToggleMirror = BUTTON_STRING_ENABLE_MIRROR;
            GUIStyle labelStyleVrActive = new GUIStyle(HighLogic.Skin.label);
            labelStyleVrActive.normal.textColor = Color.red;

            if (Core.HmdIsRunning) {
                buttonStringToggleVr = BUTTON_STRING_DISABLE_VR;
                labelStringVrActive = LABEL_STRING_VR_ACTIVE;
                labelStyleVrActive.normal.textColor = Color.green;
            }

            if (Core.RenderHmdToScreen) {
                buttonStringToggleMirror = BUTTON_STRING_DISABLE_MIRROR;
            }

            GUILayout.BeginVertical();

            // VR toggle button
            //------------------------------------------------------------------
            UnityEngine.GUI.enabled = Scene.Instance.SceneAllowsVR();
            if (GUILayout.Button(buttonStringToggleVr, HighLogic.Skin.button)) {
                if (Core.HmdIsEnabled) {
                    Core.HmdIsEnabled = false;
                } else {
                    Core.HmdIsEnabled = true;
                }
            }

            if (Core.CanResetSeatedPose()) {
                if (GUILayout.Button("Reset Headset Position", HighLogic.Skin.button)) {
                    Core.ResetInitialHmdPosition();
                }
            }

            if (Core.HmdIsRunning) {
                if (GUILayout.Button(buttonStringToggleMirror, HighLogic.Skin.button)) {
                    if (Core.RenderHmdToScreen) {
                        Core.RenderHmdToScreen = false;
                    } else {
                        Core.RenderHmdToScreen = true;
                    }
                }
            }
            UnityEngine.GUI.enabled = true;

            // VR status
            //------------------------------------------------------------------
            GUILayout.BeginHorizontal();
            GUILayout.Label("VR Status:", HighLogic.Skin.label);
            GUILayout.Label(labelStringVrActive, labelStyleVrActive);
            GUILayout.EndHorizontal();

            // settings
            //------------------------------------------------------------------
            GUIStyle labelStyleHeader = new GUIStyle(HighLogic.Skin.label);
            labelStyleHeader.fontStyle = FontStyle.Bold;
            GUILayout.Label("Options", labelStyleHeader);

            // manipulator size (VR "hands")
#if DEBUG
            GUILayout.BeginHorizontal();
            GUILayout.Label("Hand Size:", HighLogic.Skin.label);
            int handSizeScale = (int)(DeviceManager.Instance.ManipulatorSize * 100f + 0.5f);
            string handSizeStr = handSizeScale.ToString();
            handSizeStr = GUILayout.TextField(handSizeStr, HighLogic.Skin.textField);
            if (GUI.changed) {
                bool parseSuccess = System.Int32.TryParse(handSizeStr, out handSizeScale);
                if (parseSuccess &&
                    handSizeScale >= 1 &&
                    handSizeScale <= 100) {
                    DeviceManager.Instance.ManipulatorSize = handSizeScale * 0.01f;
                } else {
                    DeviceManager.Instance.ManipulatorSize = 0.45f;
                }
            }
            GUILayout.EndHorizontal();
#endif

            // world scale
            GUILayout.BeginHorizontal();
            GUILayout.Label("World Scale:", HighLogic.Skin.label);
            worldScaleStr = GUILayout.TextField(worldScaleStr, HighLogic.Skin.textField);
            if (GUI.changed) {
                bool parseSuccess = System.Single.TryParse(worldScaleStr, out float worldScale);
                if (parseSuccess &&
                    worldScale >= 0.1 &&
                    worldScale <= 10) {
                    Scene.Instance.WorldScale = worldScale;
                }
            }
            GUILayout.EndHorizontal();


            //------------------------------------------------------------------
            GUILayout.EndVertical();

            // allow dragging the window
            UnityEngine.GUI.DragWindow();
        }
    }
}
