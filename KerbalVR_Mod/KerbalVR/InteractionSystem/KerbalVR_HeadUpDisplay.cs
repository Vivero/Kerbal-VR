using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Valve.VR;
using TMPro;

namespace KerbalVR {
    /// <summary>
    /// The Hand component is applied to each of the two hand GameObjects.
    /// It handles all the interactions related to using the hands in VR.
    /// </summary>
    public class HeadUpDisplay : MonoBehaviour {

        #region Public Members
        /// <summary>
        /// How far away from your eyes to place the Head Up Display
        /// </summary>
        private float _distance = 1.2f;
        public float Distance {
            get {
                return _distance;
            }
            set {
                _distance = value;
                if (hudGameObject != null) {
                    hudGameObject.transform.position = VR_CAMERA_POSITION + new Vector3(0f, 0f, _distance);
                }
            }
        }
        #endregion


        #region Private Members
        // hud game object
        protected GameObject hudGameObject;
        protected MeshRenderer hudRenderer;
        protected GameObject hudCanvasGameObject;
        protected Canvas hudCanvas;
        protected GameObject hudCanvasCameraGameObject;
        protected Camera hudCanvasCamera;
        protected int hudPixelWidth = 1400;
        protected int hudPixelHeight = 1024;
        protected float hudHeight = 1f;
        protected readonly Vector3 HUD_CANVAS_POSITION = new Vector3(-4000f, -4000f, 4000f);

        // VR cameras to display HUD over everything
        protected GameObject[] vrCameraObjects = new GameObject[2];
        protected Camera[] vrCameras = new Camera[2];
        protected readonly Vector3 VR_CAMERA_POSITION = new Vector3(-4000f, 4000f, 4000f);

        // keep tracking of render state
        protected Types.ShiftRegister<bool> isRendering = new Types.ShiftRegister<bool>(2);

        protected TextMeshPro label;
        #endregion


        public void Initialize() {
            for (int eyeIdx = 0; eyeIdx < 2; ++eyeIdx) {
                EVREye eye = (EVREye)eyeIdx;
                string kvrCameraName = "KVR_Eye_Camera (" + eye.ToString() + ") (KVR_HeadUpDisplay)";
                vrCameraObjects[eyeIdx] = new GameObject(kvrCameraName);
                DontDestroyOnLoad(vrCameraObjects[eyeIdx]);
                vrCameras[eyeIdx] = vrCameraObjects[eyeIdx].AddComponent<Camera>();
                vrCameras[eyeIdx].enabled = false;
                vrCameras[eyeIdx].targetTexture = KerbalVR.Core.HmdEyeRenderTexture[eyeIdx];

                vrCameras[eyeIdx].clearFlags = CameraClearFlags.Nothing;
                vrCameras[eyeIdx].cullingMask = (1 << 5);
                vrCameras[eyeIdx].depth = 100f + (eyeIdx * 0.5f);
                vrCameras[eyeIdx].farClipPlane = 100f;
                vrCameras[eyeIdx].nearClipPlane = 0.01f;
                vrCameras[eyeIdx].orthographic = false;
                HmdMatrix44_t projectionMatrix = OpenVR.System.GetProjectionMatrix(
                    eye, vrCameras[eyeIdx].nearClipPlane, vrCameras[eyeIdx].farClipPlane);
                vrCameras[eyeIdx].projectionMatrix = MathUtils.Matrix4x4_OpenVr2UnityFormat(ref projectionMatrix);
            }

            // create the mesh for the HUD
            hudGameObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(hudGameObject.GetComponent<MeshCollider>());
            DontDestroyOnLoad(hudGameObject);
            hudGameObject.transform.localScale = new Vector3(((float)hudPixelWidth / hudPixelHeight) * hudHeight, hudHeight , 1f);
            hudGameObject.layer = 5;
            hudGameObject.transform.position = VR_CAMERA_POSITION + new Vector3(0f, 0f, Distance);
            hudGameObject.transform.rotation = Quaternion.identity;
        }

        protected void OnEnable() {
            // setup callback functions for events
            GameEvents.onGameSceneSwitchRequested.Add(OnGameSceneSwitchRequested);
        }

        protected void OnDisable() {
            // remove callback functions
            GameEvents.onGameSceneSwitchRequested.Remove(OnGameSceneSwitchRequested);
        }

        protected void Update() {
            // should we render the hands in the current scene?
            bool isRendering = false;
            if (KerbalVR.Core.IsVrRunning) {
                switch (HighLogic.LoadedScene) {
                    case GameScenes.MAINMENU:
                    case GameScenes.EDITOR:
                        isRendering = true;
                        break;

                    case GameScenes.FLIGHT:
                        if (KerbalVR.Scene.IsInEVA() || KerbalVR.Scene.IsInIVA()) {
                            isRendering = true;
                        }
                        else {
                            isRendering = false;
                        }
                        break;
                }
            }
            else {
                isRendering = false;
            }

            if (isRendering) {
                // position the HUD in front of the face
                // TODO: keep this around until needed
                /*
                Vector3 hmdPos = KerbalVR.Scene.Instance.DevicePoseToWorld(KerbalVR.Scene.Instance.HmdTransform.pos);
                Quaternion hmdRot = KerbalVR.Scene.Instance.DevicePoseToWorld(KerbalVR.Scene.Instance.HmdTransform.rot);
                this.transform.position = hmdPos + hmdRot * new Vector3(0f, 0f, Distance);
                this.transform.rotation = hmdRot;
                */

                for (int eyeIdx = 0; eyeIdx < 2; ++eyeIdx) {
                    vrCameraObjects[eyeIdx].transform.position = VR_CAMERA_POSITION + KerbalVR.Scene.Instance.HmdEyeTransform[eyeIdx].pos;
                    vrCameraObjects[eyeIdx].transform.rotation = Quaternion.identity;
                }

                if (label != null && FlightGlobals.ActiveVessel != null) {
                    Utils.HumanizeQuantity((float)FlightGlobals.ActiveVessel.altitude, "m", out float altitude, out string altitudeUnits);
                    label.text = "Altitude: " + altitude.ToString("F0") + " " + altitudeUnits;
                }
            }

            // makes changes as necessary
            this.isRendering.Push(isRendering);
            if (this.isRendering.IsChanged()) {
                if (hudRenderer != null) {
                    hudRenderer.enabled = this.isRendering.Value;
                }
                vrCameras[0].enabled = this.isRendering.Value;
                vrCameras[1].enabled = this.isRendering.Value;
            }
        }

        protected void OnGameSceneSwitchRequested(GameEvents.FromToAction<GameScenes, GameScenes> fromToAction) {
            // when finished loading, set up the HUD canvas
            if (fromToAction.from == GameScenes.LOADING) {
                // create a texture we can draw on the HUD
                Material hudMaterial = new Material(Shader.Find("KSP/Alpha/Translucent Additive"));
                RenderTexture hudTexture = new RenderTexture(hudPixelWidth, hudPixelHeight, 32, RenderTextureFormat.ARGB32);
                hudTexture.Create();
                hudMaterial.mainTexture = hudTexture;

                // create a UI Canvas for this screen.
                // place it far away from other KSP UI cameras.
                hudCanvasCameraGameObject = new GameObject("KVR_HeadUpDisplay_CanvasCamera");
                DontDestroyOnLoad(hudCanvasCameraGameObject);
                hudCanvasCameraGameObject.transform.position = HUD_CANVAS_POSITION;
                hudCanvasCamera = hudCanvasCameraGameObject.AddComponent<Camera>();
                hudCanvasCamera.clearFlags = CameraClearFlags.Color;
                hudCanvasCamera.backgroundColor = Color.clear;
                hudCanvasCamera.orthographic = true;
                hudCanvasCamera.targetTexture = hudTexture;
                hudCanvasCamera.cullingMask = (1 << 5); // layer: UI
                hudCanvasCamera.orthographicSize = hudPixelHeight * 0.5f;
                hudCanvasCamera.nearClipPlane = 1f;
                hudCanvasCamera.farClipPlane = 10f;

                hudCanvasGameObject = new GameObject("KVR_HeadUpDisplay_Canvas");
                DontDestroyOnLoad(hudCanvasGameObject);
                hudCanvas = hudCanvasGameObject.AddComponent<Canvas>();
                hudCanvas.renderMode = RenderMode.ScreenSpaceCamera;
                hudCanvas.pixelPerfect = false;
                hudCanvas.worldCamera = hudCanvasCamera;
                hudCanvas.planeDistance = 2f;
                hudCanvasGameObject.AddComponent<CanvasScaler>();

                // assign the material to the HUD
                hudRenderer = hudGameObject.GetComponent<MeshRenderer>();
                hudRenderer.material = hudMaterial;

                // create UI elements
                CreateHeadUpDisplayUI();
                Utils.SetLayer(hudCanvasGameObject, 5); // UI layer
            }
        }

        protected void CreateHeadUpDisplayUI() {
            // create a static reticle in the center
            GameObject reticleImageGO = new GameObject("Reticle");
            reticleImageGO.AddComponent<CanvasRenderer>();
            Image reticleImage = reticleImageGO.AddComponent<Image>();
            string texPath = Path.Combine(Globals.KERBALVR_TEXTURES_DIR, "hud_heading").Replace("\\", "/");
            // string texPath = Path.Combine(Globals.KERBALVR_TEXTURES_DIR, "test_image").Replace("\\", "/");
            Shader texShader = Shader.Find("Unlit/Transparent");
            Material reticleMat = new Material(texShader);
            reticleMat.mainTexture = GameDatabase.Instance.GetTexture(texPath, false);
            reticleImage.material = reticleMat;
            reticleImageGO.transform.SetParent(hudCanvasGameObject.transform, false);
            reticleImage.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            reticleImage.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            reticleImage.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            reticleImage.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, hudPixelHeight);
            reticleImage.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, hudPixelHeight);
            reticleImage.rectTransform.localPosition = new Vector3(0f, 0f);

            // information text labels
            TMP_FontAsset font = KerbalVR.AssetLoader.Instance.GetTmpFont("Futura_Medium_BT");
            GameObject altitudeLabel = new GameObject("AltitudeLabel");
            altitudeLabel.AddComponent<CanvasRenderer>();
            label = altitudeLabel.AddComponent<TextMeshPro>();
            label.text = "Altitude: ";
            label.font = font;
            label.fontSize = 600f;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.TopLeft;
            label.transform.SetParent(hudCanvasGameObject.transform, false);
            label.rectTransform.anchorMin = new Vector2(0f, 1f);
            label.rectTransform.anchorMax = new Vector2(0f, 1f);
            label.rectTransform.pivot = new Vector2(0f, 1f);
            label.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 1000);
            label.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 300);
            label.rectTransform.localPosition = new Vector3(-hudPixelWidth * 0.45f, hudPixelHeight * 0.4f);
        }
    }
}
