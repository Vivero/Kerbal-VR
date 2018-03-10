using UnityEngine;
using KSP.UI.Screens;

namespace KerbalVR
{
    public class KerbalVR_GUI
    {
        public static string AppButtonLogo {
            get {
                return Utils.KERBALVR_ASSETS_DIR + "app_button_logo";
            }
        }

        private ApplicationLauncherButton appButton;
        private bool appButtonGuiActive = false;

        private static readonly int appGuiId = 186012;
        private Rect appGuiWindowRect = new Rect(Screen.width / 4, Screen.height / 4, 400, 400);

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
                appGuiWindowRect = GUILayout.Window(appGuiId, appGuiWindowRect, GenerateGUI, "KerbalVR");
            }
        }

        private void GenerateGUI(int windowId) {
            GUILayout.BeginVertical("box");
            GUILayout.Label("KerbalVR PlaceHolder GUI");
            if (GUILayout.Button("Close this Window", GUILayout.Width(200f))) {
                OnToggleFalse();
            }
            GUILayout.Label("Can this display?");
            GUILayout.EndVertical();
            GUI.DragWindow();
        }
    }
}
