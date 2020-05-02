using System.IO;
using KSP.UI.Screens;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KerbalVR
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class AppGUILoader : MonoBehaviour
    {
        #region Constants
        public static string APP_BUTTON_LOGO {
            get {
                string path = Path.Combine(Globals.KERBALVR_TEXTURES_DIR, "app_button_logo");
                return path.Replace("\\", "/");
            }
        }

        public static string APP_BUTTON_LOGO_ALT {
            get {
                string path = Path.Combine(Globals.KERBALVR_TEXTURES_DIR, "app_button_logo_alt");
                return path.Replace("\\", "/");
            }
        }

        protected static readonly ApplicationLauncher.AppScenes APP_VISIBILITY =
            ApplicationLauncher.AppScenes.MAINMENU |
            ApplicationLauncher.AppScenes.SPACECENTER |
            ApplicationLauncher.AppScenes.TRACKSTATION |
            ApplicationLauncher.AppScenes.FLIGHT |
            ApplicationLauncher.AppScenes.MAPVIEW |
            ApplicationLauncher.AppScenes.VAB |
            ApplicationLauncher.AppScenes.SPH;
        #endregion


        #region Private Members
        protected ApplicationLauncherButton appMainButton = null;
        protected ApplicationLauncherButton appDebugButton = null;
        protected static GameObject uiMainCanvas = null;
        protected static GameObject uiDebugCanvas = null;
        #endregion

        protected void Awake() {
            // when ready for a GUI, load it
            GameEvents.onGUIApplicationLauncherReady.Add(OnAppLauncherReady);
            GameEvents.onGUIApplicationLauncherDestroyed.Add(OnAppLauncherDestroyed);

            // this creates a callback so that whenever the scene is changed we can turn off the UI
            GameEvents.onGameSceneSwitchRequested.Add(OnSceneChange);
        }

        /// <summary>
        /// This GameEvent is registered with GameEvents.onGUIApplicationLauncherReady,
        /// at the time the plugin is loaded. It instantiates a new button on the
        /// application launcher. Note that this callback can be called multiple times
        /// throughout the game.
        /// </summary>
        public void OnAppLauncherReady() {
            // create new app button instance if it doesn't already exist
            if (appMainButton == null) {
                appMainButton = ApplicationLauncher.Instance.AddModApplication(
                    OnMainToggleTrue,
                    OnMainToggleFalse,
                    null, null, null, null,
                    APP_VISIBILITY,
                    GameDatabase.Instance.GetTexture(APP_BUTTON_LOGO, false));
            }

            // create a debugging tools app button instance if it doesn't already exist
            if (appDebugButton == null) {
                appDebugButton = ApplicationLauncher.Instance.AddModApplication(
                    OnDebugToggleTrue,
                    OnDebugToggleFalse,
                    null, null, null, null,
#if DEBUG
                    ApplicationLauncher.AppScenes.ALWAYS,
#else
                    ApplicationLauncher.AppScenes.NEVER,
#endif
                    GameDatabase.Instance.GetTexture(APP_BUTTON_LOGO_ALT, false));
            }

            // load the UI prefab
            if (uiMainCanvas == null) {
                uiMainCanvas = Instantiate(KerbalVR.AssetLoader.Instance.GetGameObject("KVR_UI_MainPanel"));
                uiMainCanvas.name = "KVR_UI_MainPanel";
                uiMainCanvas.transform.SetParent(MainCanvasUtil.MainCanvas.transform);
                uiMainCanvas.AddComponent<AppMainGUI>();
                uiMainCanvas.SetActive(false);
            }

            // load the debugging UI prefab
            if (uiDebugCanvas == null) {
                uiDebugCanvas = Instantiate(KerbalVR.AssetLoader.Instance.GetGameObject("KVR_UI_DebugPanel"));
                uiMainCanvas.name = "KVR_UI_DebugPanel";
                uiDebugCanvas.transform.SetParent(MainCanvasUtil.MainCanvas.transform);
                uiDebugCanvas.AddComponent<AppDebugGUI>();
                uiDebugCanvas.SetActive(false);
            }
        }

        /// <summary>
        /// This GameEvent is registered with GameEvents.onGUIApplicationLauncherDestroyed,
        /// at the time the plugin is loaded. It destroys the application button on the
        /// application launcher.
        /// </summary>
        public void OnAppLauncherDestroyed() {
            if (appMainButton != null) {
                appMainButton.SetFalse(true);
                ApplicationLauncher.Instance.RemoveApplication(appMainButton);
            }
            if (appDebugButton != null) {
                appDebugButton.SetFalse(true);
                ApplicationLauncher.Instance.RemoveApplication(appDebugButton);
            }
        }

        /// <summary>
        /// Callback when the main application button is toggled on.
        /// </summary>
        public void OnMainToggleTrue() {
            uiMainCanvas.SetActive(true);
        }

        /// <summary>
        /// Callback when the debug application button is toggled on.
        /// </summary>
        public void OnDebugToggleTrue() {
            uiDebugCanvas.SetActive(true);
        }

        /// <summary>
        /// Callback when the main application button is toggled off.
        /// </summary>
        public void OnMainToggleFalse() {
            uiMainCanvas.SetActive(false);
        }

        /// <summary>
        /// Callback when the debug application button is toggled off.
        /// </summary>
        public void OnDebugToggleFalse() {
            uiDebugCanvas.SetActive(false);
        }

        /// <summary>
        /// Callback when the game changes scenes.
        /// </summary>
        protected void OnSceneChange(GameEvents.FromToAction<GameScenes, GameScenes> fromToScenes) {
            // on scene change, command the button to toggle off, so the GUI closes
            appMainButton.SetFalse(true);
            appDebugButton.SetFalse(true);
        }
    }


    public class AppGUI : MonoBehaviour, IBeginDragHandler, IDragHandler
    {
#region Private Members
        protected Vector2 panelDragStart;
        protected Vector2 panelAltStart;
#endregion

        // this event fires when a drag event begins
        public void OnBeginDrag(PointerEventData data) {
            panelDragStart = new Vector2(data.position.x - Screen.width * 0.5f, data.position.y - Screen.height * 0.5f);
            panelAltStart = transform.position;
        }

        // this event fires while we're dragging. It's constantly moving the UI to a new position
        public void OnDrag(PointerEventData data) {
            Vector2 deltaPos = new Vector2(data.position.x - Screen.width * 0.5f, data.position.y - Screen.height * 0.5f);
            Vector2 dragVector = deltaPos - panelDragStart;
            transform.position = panelAltStart + dragVector;
        }
    }


    public class AppMainGUI : AppGUI
    {
#region Private Members
        private GameObject vrEnableButton;
        private GameObject resetPositionButton;
        private GameObject initOpenVrAtStartupToggle;
        private GameObject swapYawRollControlsToggle;
        private GameObject worldScaleSlider;
        private GameObject handSizeScaleSlider;

        private Text vrEnableButtonText;
        private Text vrStatusText;
        private Text worldScaleLabel;
        private Text handSizeScaleLabel;
#endregion

        private void Awake() {
            // create callbacks for the buttons
            vrEnableButton = GameObject.Find("KVR_UI_EnableButton");
            Button vrEnableButtonComponent = vrEnableButton.GetComponent<Button>();
            vrEnableButtonComponent.interactable = KerbalVR.Scene.Instance.IsVrAllowed;
            vrEnableButtonComponent.onClick.AddListener(OnVrEnableButtonClicked);

            resetPositionButton = GameObject.Find("KVR_UI_ResetPosButton");
            Button resetPositionButtonComponent = resetPositionButton.GetComponent<Button>();
            resetPositionButtonComponent.interactable = true;
            resetPositionButtonComponent.onClick.AddListener(OnResetPositionButtonClicked);

            // set toggle states and create callbacks for toggle buttons
            initOpenVrAtStartupToggle = GameObject.Find("KVR_UI_InitToggle");
            Toggle initOpenVrAtStartupToggleComponent = initOpenVrAtStartupToggle.GetComponent<Toggle>();
            initOpenVrAtStartupToggleComponent.SetIsOnWithoutNotify(KerbalVR.Configuration.Instance.InitOpenVrAtStartup);
            initOpenVrAtStartupToggleComponent.onValueChanged.AddListener(OnInitOpenVrAtStartupToggleClicked);

            swapYawRollControlsToggle = GameObject.Find("KVR_UI_SwapControlsToggle");
            Toggle swapYawRollControlsToggleComponent = swapYawRollControlsToggle.GetComponent<Toggle>();
            swapYawRollControlsToggleComponent.SetIsOnWithoutNotify(KerbalVR.Configuration.Instance.SwapYawRollControls);
            swapYawRollControlsToggleComponent.onValueChanged.AddListener(OnSwapYawRollControlsClicked);

            // set slider states and create callbacks for sliders
            worldScaleSlider = GameObject.Find("KVR_UI_WorldScaleSlider");
            Slider worldScaleSliderComponent = worldScaleSlider.GetComponent<Slider>();
            worldScaleSliderComponent.onValueChanged.AddListener(OnWorldScaleSliderChanged);

            handSizeScaleSlider = GameObject.Find("KVR_UI_HandSizeSlider");
            Slider handSizeScaleSliderComponent = handSizeScaleSlider.GetComponent<Slider>();
            handSizeScaleSliderComponent.SetValueWithoutNotify(1f);
            handSizeScaleSliderComponent.onValueChanged.AddListener(OnHandSizeScaleSliderChanged);
            handSizeScaleSliderComponent.interactable = false; // TODO: implement hand size

            // get text label objects
            GameObject vrEnableButtonTextObject = GameObject.Find("KVR_UI_EnableButton_Text");
            vrEnableButtonText = vrEnableButtonTextObject.GetComponent<Text>();

            GameObject vrStatusTextObject = GameObject.Find("KVR_UI_StatusText");
            vrStatusText = vrStatusTextObject.GetComponent<Text>();

            GameObject worldScaleLabelObject = GameObject.Find("KVR_UI_WorldScaleLabel");
            worldScaleLabel = worldScaleLabelObject.GetComponent<Text>();
            worldScaleLabelObject.SetActive(false);

            GameObject handSizeScaleLabelObject = GameObject.Find("KVR_UI_HandSizeLabel");
            handSizeScaleLabel = handSizeScaleLabelObject.GetComponent<Text>();
            handSizeScaleLabelObject.SetActive(false);

            // create a callback to listen to the VR status
            KerbalVR.Events.HmdStatusUpdated.Listen(OnHmdStatusUpdated);

            // TODO: re-do these controls
#if !DEBUG
            GameObject worldScaleContainer = GameObject.Find("KVR_UI_WorldScaleContainer");
            worldScaleContainer.SetActive(false);
            GameObject handSizeContainer = GameObject.Find("KVR_UI_HandSizeContainer");
            handSizeContainer.SetActive(false);
#endif
        }

        private void Update() {
            // verify what buttons can be pressed
            vrEnableButton.GetComponent<Button>().interactable = KerbalVR.Scene.Instance.IsVrAllowed;
        }

        void OnVrEnableButtonClicked() {
            // toggle the VR enable
            if (KerbalVR.Core.IsVrEnabled) {
                KerbalVR.Core.IsVrEnabled = false;
            }
            else {
                KerbalVR.Core.IsVrEnabled = true;
            }
        }

        void OnResetPositionButtonClicked() {
            KerbalVR.Core.ResetInitialHmdPosition();
        }

        void OnInitOpenVrAtStartupToggleClicked(bool isOn) {
            // isOn is true if the checkbox is currently checked (in Unity it's in the "Toggle (Script)" as "Is On")
            KerbalVR.Configuration.Instance.InitOpenVrAtStartup = isOn;
        }

        void OnSwapYawRollControlsClicked(bool isOn) {
            KerbalVR.Configuration.Instance.SwapYawRollControls = isOn;
        }

        void OnWorldScaleSliderChanged(float value) {
            // TODO: re-do these controls
            worldScaleLabel.text = "World Scale: " + value.ToString("F1");
        }

        void OnHandSizeScaleSliderChanged(float value) {
            // TODO: re-do these controls
            handSizeScaleLabel.text = "Hand Size Scale: " + value.ToString("F1");
        }

        void OnHmdStatusUpdated(bool isRunning) {
            if (isRunning) {
                vrEnableButtonText.text = "DISABLE VR";
                vrStatusText.text = "ENABLED";
                vrStatusText.color = Color.green;
            }
            else {
                vrEnableButtonText.text = "ENABLE VR";
                vrStatusText.text = "DISABLED";
                vrStatusText.color = Color.red;
            }
        }
    }


    public class AppDebugGUI : AppGUI
    {
#region Private Members
        private Text debugText;
#endregion

        private void Awake() {
            // get text label objects
            GameObject debugTextObject = GameObject.Find("KVR_UI_DebugText");
            debugText = debugTextObject.GetComponent<Text>();
            debugText.fontSize = 12;

            SetText("Debug Content");
        }

        public void SetText(object obj) {
            debugText.text = obj.ToString();
        }
    }
}
