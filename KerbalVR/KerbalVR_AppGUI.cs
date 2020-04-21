using System.IO;
using KSP.UI.Screens;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KerbalVR
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class AppGUILoader : MonoBehaviour //, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        #region Constants
        public static string AppButtonLogo {
            get {
                string path = Path.Combine(Globals.KERBALVR_TEXTURES_DIR, "app_button_logo");
                return path.Replace("\\", "/");
            }
        }

        private static readonly ApplicationLauncher.AppScenes APP_VISIBILITY =
#if DEBUG
            ApplicationLauncher.AppScenes.ALWAYS;
#else
            ApplicationLauncher.AppScenes.FLIGHT |
            ApplicationLauncher.AppScenes.VAB |
            ApplicationLauncher.AppScenes.SPH;
#endif
        #endregion

        #region Properties
        #endregion

        #region Private Members
        private ApplicationLauncherButton appButton = null;
        private static GameObject uiCanvas = null;
        #endregion

        private void Awake() {
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
            Utils.Log("NewGUILoader OnAppLauncherReady");

            // create new app button instance if it doesn't already exist
            if (appButton == null) {
                appButton = ApplicationLauncher.Instance.AddModApplication(
                    OnToggleTrue,
                    OnToggleFalse,
                    null, null, null, null,
                    APP_VISIBILITY,
                    GameDatabase.Instance.GetTexture(AppButtonLogo, false));
            }

            // load the UI prefab
            if (uiCanvas == null) {
                uiCanvas = Instantiate(KerbalVR.AssetLoader.Instance.GetGameObject("KVR_UI_MainPanel"));
                uiCanvas.transform.SetParent(MainCanvasUtil.MainCanvas.transform);
                uiCanvas.AddComponent<AppGUI>();
                uiCanvas.SetActive(false);
            }
        }

        /// <summary>
        /// This GameEvent is registered with GameEvents.onGUIApplicationLauncherDestroyed,
        /// at the time the plugin is loaded. It destroys the application button on the
        /// application launcher.
        /// </summary>
        public void OnAppLauncherDestroyed() {
            Utils.Log("NewGUILoader OnAppLauncherDestroyed");

            if (appButton != null) {
                OnToggleFalse();
                ApplicationLauncher.Instance.RemoveApplication(appButton);
            }
        }

        /// <summary>
        /// Callback when the application button is toggled on.
        /// </summary>
        public void OnToggleTrue() {
            Utils.Log("NewGUILoader OnToggleTrue");
            uiCanvas.SetActive(true);
        }

        /// <summary>
        /// Callback when the application button is toggled off.
        /// </summary>
        public void OnToggleFalse() {
            Utils.Log("NewGUILoader OnToggleFalse");
            uiCanvas.SetActive(false);
        }

        /// <summary>
        /// Callback when the game changes scenes.
        /// </summary>
        void OnSceneChange(GameEvents.FromToAction<GameScenes, GameScenes> fromToScenes) {
            // on scene change, command the button to toggle off, so the GUI closes
            appButton.SetFalse(true);
        }
    }


    public class AppGUI : MonoBehaviour, IBeginDragHandler, IDragHandler
    {
        #region Private Members
        private Vector2 mainPanelDragStart;
        private Vector2 mainPanelAltStart;

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
            vrEnableButtonComponent.onClick.AddListener(OnVrEnableButtonClicked);

            resetPositionButton = GameObject.Find("KVR_UI_ResetPosButton");
            Button resetPositionButtonComponent = resetPositionButton.GetComponent<Button>();
            resetPositionButtonComponent.interactable = KerbalVR.Core.CanResetSeatedPose();
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
            worldScaleSliderComponent.SetValueWithoutNotify(1f);
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

            GameObject handSizeScaleLabelObject = GameObject.Find("KVR_UI_HandSizeLabel");
            handSizeScaleLabel = handSizeScaleLabelObject.GetComponent<Text>();

            // create a callback to listen to the VR status
            KerbalVR.Events.HmdStatusUpdated.Listen(OnHmdStatusUpdated);
        }

        void OnVrEnableButtonClicked() {
            // toggle the VR enable
            if (KerbalVR.Core.HmdIsEnabled) {
                KerbalVR.Core.HmdIsEnabled = false;
            } else {
                KerbalVR.Core.HmdIsEnabled = true;
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
            worldScaleLabel.text = "World Scale: " + value.ToString("F1");
            if (value >= 0.5f && value <= 2f) {
                KerbalVR.Configuration.Instance.WorldScale = value;
                KerbalVR.Scene.Instance.WorldScale = value;
            }
        }

        void OnHandSizeScaleSliderChanged(float value) {
            handSizeScaleLabel.text = "Hand Size Scale: " + value.ToString("F1");
        }

        void OnHmdStatusUpdated(bool isRunning) {
            if (isRunning) {
                vrEnableButtonText.text = "DISABLE VR";
                vrStatusText.text = "ENABLED";
                vrStatusText.color = Color.green;
            } else {
                vrEnableButtonText.text = "ENABLE VR";
                vrStatusText.text = "DISABLED";
                vrStatusText.color = Color.red;
            }
            resetPositionButton.GetComponent<Button>().interactable = KerbalVR.Core.CanResetSeatedPose();
        }

        // this event fires when a drag event begins
        public void OnBeginDrag(PointerEventData data) {
            mainPanelDragStart = new Vector2(data.position.x - Screen.width * 0.5f, data.position.y - Screen.height * 0.5f);
            mainPanelAltStart = transform.position;
        }

        // this event fires while we're dragging. It's constantly moving the UI to a new position
        public void OnDrag(PointerEventData data) {
            Vector2 deltaPos = new Vector2(data.position.x - Screen.width * 0.5f, data.position.y - Screen.height * 0.5f);
            Vector2 dragVector = deltaPos - mainPanelDragStart;
            transform.position = mainPanelAltStart + dragVector;
        }
    }
}
