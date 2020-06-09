using LibNoise;
using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Vectrosity;

namespace KerbalVR.InternalModules {
    public class TouchDisplay : InternalModule {

        #region KSP Config Fields
        [KSPField]
        public string screenTransformName = "";

        [KSPField]
        public string textureLayerID = "";

        [KSPField]
        public int screenPixelWidth = 1024;

        [KSPField]
        public int screenPixelHeight = 1024;

        [KSPField]
        public float refreshDrawRate = 0.1f;

        [KSPField]
        public Color emptyColor = Color.black;
        #endregion


        #region Private Members
        protected ConfigNode moduleConfigNode;
        protected float lastScreenUpdateTime;

        // screen display objects
        protected RenderTexture screenTexture;
        protected GameObject screenCanvasGameObject, screenCanvasCameraGameObject;
        protected Canvas screenCanvas;
        protected Camera screenCanvasCamera;

        protected static int nextKvrCanvasId = 1;
        protected int kvrCanvasId = 0;
        protected const float CANVAS_CAMERA_DEPTH = 10f;

        // HUD elements
        protected Image hudHeadingImage;
        protected const int NUM_HEADING_LABELS = 9;
        protected GameObject[] headingLabels = new GameObject[NUM_HEADING_LABELS];
        protected TextMeshPro[] headingLabelsTMP = new TextMeshPro[NUM_HEADING_LABELS];
        protected Quaternion headingImageAngle = Quaternion.identity;

        // screen text data
        protected TextMeshPro dataLabelVelocityValue, dataLabelVelocityUnits;
        protected TextMeshPro dataLabelAltitudeValue, dataLabelAltitudeUnits;
        protected TextMeshPro dataLabelRollValue, dataLabelRollUnits;
        protected TextMeshPro dataLabelPitchValue, dataLabelPitchUnits;
        #endregion


        // DEBUG ---
        int screenIdx = 0;
        VectorLine vLine;
        // ---------

        protected void Start() {
            // no setup needed in editor mode
            if (HighLogic.LoadedScene == GameScenes.EDITOR) return;

            // obtain module configuration
            moduleConfigNode = KerbalVR.ConfigUtils.GetModuleConfigNode(internalProp.name, moduleID);

            // screen refresh rate
            lastScreenUpdateTime = Time.time;

            // get the child object for the display screen
            Transform screenTransform = transform.Find(screenTransformName);
            if (screenTransform == null) {
                Utils.LogWarning("KerbalVR.InternalModules.TouchDisplay: screenTransform is null");
                return;
            }

            // get the mesh renderer for the screen
            GameObject screenGameObject = screenTransform.gameObject;
            MeshRenderer screenRenderer = screenGameObject.GetComponent<MeshRenderer>();
            if (screenRenderer == null) {
                Utils.LogWarning("KerbalVR.InternalModules.TouchDisplay: screenRenderer is null");
                return;
            }
            if (textureLayerID == null) {
                Utils.LogWarning("KerbalVR.InternalModules.TouchDisplay: textureLayerID is null or empty");
                return;
            }

            // create a texture we can draw on, on the screen
            screenTexture = new RenderTexture(screenPixelWidth, screenPixelHeight, 32, RenderTextureFormat.ARGB32);
            screenTexture.Create();
            screenRenderer.material.SetTexture(textureLayerID, screenTexture);

            // create a UI Canvas for this screen.
            // place it far away from other KSP UI cameras.
            kvrCanvasId = nextKvrCanvasId++;
            screenCanvasCameraGameObject = new GameObject("KVR_TouchDisplay_CanvasCamera_" + internalProp.propID);
            screenCanvasCameraGameObject.transform.position = new Vector3(4000f, 4000f, 4000f + CANVAS_CAMERA_DEPTH * kvrCanvasId);
            screenCanvasCamera = screenCanvasCameraGameObject.AddComponent<Camera>();
            screenCanvasCamera.clearFlags = CameraClearFlags.SolidColor;
            screenCanvasCamera.backgroundColor = emptyColor;
            screenCanvasCamera.orthographic = true;
            screenCanvasCamera.targetTexture = screenTexture;
            screenCanvasCamera.cullingMask = (1 << 5); // layer: UI
            screenCanvasCamera.orthographicSize = screenPixelHeight * 0.5f;
            screenCanvasCamera.nearClipPlane = CANVAS_CAMERA_DEPTH * 0.2f;
            screenCanvasCamera.farClipPlane = CANVAS_CAMERA_DEPTH * 0.5f;

            screenCanvasGameObject = new GameObject("KVR_TouchDisplay_Canvas_" + internalProp.propID);
            screenCanvas = screenCanvasGameObject.AddComponent<Canvas>();
            screenCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            screenCanvas.pixelPerfect = false;
            screenCanvas.worldCamera = screenCanvasCamera;
            screenCanvas.planeDistance = CANVAS_CAMERA_DEPTH * 0.25f;
            screenCanvasGameObject.AddComponent<CanvasScaler>();

            // screen coordinate system:
            //   (0,0) is dead-center
            //   (screenPixelWidth * 0.5f, screenPixelHeight * 0.5f) is top-right
            //   (screenPixelWidth * -0.5f, screenPixelHeight * -0.5f) is bottom-left
            //

            CreateScreenUI();
            CreateDebugObjects();

            // set layer for this object to 20 (Internal Space)
            Utils.SetLayer(screenCanvasGameObject, 5);
            Utils.SetLayer(this.gameObject, 20);
        }

        protected void CreateScreenUI() {
            // create a static reticle in the center
            GameObject reticleImageGO = new GameObject("Reticle");
            reticleImageGO.AddComponent<CanvasRenderer>();
            Image reticleImage = reticleImageGO.AddComponent<Image>();
            string texPath = Path.Combine(Globals.KERBALVR_TEXTURES_DIR, "hud_reticle").Replace("\\", "/");
            Shader texShader = Shader.Find("Unlit/Transparent");
            Material reticleMat = new Material(texShader);
            reticleMat.mainTexture = GameDatabase.Instance.GetTexture(texPath, false);
            reticleImage.material = reticleMat;
            reticleImageGO.transform.SetParent(screenCanvasGameObject.transform, false);
            reticleImage.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            reticleImage.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            reticleImage.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            reticleImage.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, screenPixelWidth);
            reticleImage.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, screenPixelHeight);
            reticleImage.rectTransform.localPosition = new Vector3(0f, 0f);

            // create a background for heading labels
            GameObject hudHeadingGameObject = new GameObject("HeadingImage");
            hudHeadingGameObject.AddComponent<CanvasRenderer>();
            hudHeadingImage = hudHeadingGameObject.AddComponent<Image>();
            texPath = Path.Combine(Globals.KERBALVR_TEXTURES_DIR, "hud_heading").Replace("\\", "/");
            Material headingImageMat = new Material(texShader);
            headingImageMat.mainTexture = GameDatabase.Instance.GetTexture(texPath, false);
            hudHeadingImage.material = headingImageMat;
            hudHeadingGameObject.transform.SetParent(screenCanvasGameObject.transform, false);
            hudHeadingImage.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            hudHeadingImage.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            hudHeadingImage.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            hudHeadingImage.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, screenPixelWidth * 1.5f);
            hudHeadingImage.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, screenPixelHeight * 1.5f);
            hudHeadingImage.rectTransform.localPosition = new Vector3(0f, 0f);

            // create an flight data table
            GameObject panel = new GameObject("KVR_TouchDisplay_MainPanel");
            panel.AddComponent<CanvasRenderer>();
            GridLayoutGroup panelGrid = panel.AddComponent<GridLayoutGroup>();
            panelGrid.padding = new RectOffset(0, 0, 0, 0);
            panelGrid.cellSize = new Vector2((screenCanvas.pixelRect.width * screenCanvas.transform.localScale.x - 20f) / 6, 100f);
            panelGrid.spacing = new Vector2(5f, 0f);
            panelGrid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            panelGrid.startAxis = GridLayoutGroup.Axis.Horizontal;
            panelGrid.childAlignment = TextAnchor.UpperCenter;
            panelGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            panelGrid.constraintCount = 6;
            panel.transform.SetParent(screenCanvasGameObject.transform, false);
            RectTransform panelRectTransform = panel.GetComponent<RectTransform>();
            panelRectTransform.anchorMin = panelRectTransform.anchorMax = panelRectTransform.pivot = new Vector2(0f, 1f);
            panelRectTransform.localPosition = new Vector3(-screenPixelWidth * 0.5f, screenPixelHeight * 0.5f, 0f);
            panelRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, screenPixelWidth);
            panelRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, screenPixelHeight * 0.5f);

            TMP_FontAsset font = KerbalVR.AssetLoader.Instance.GetTmpFont("SpaceMono_Regular");
            for (int y = 0; y < 2; ++y) {
                for (int x = 0; x < 2; ++x) {
                    GameObject labelName = new GameObject("Label_" + x + "_" + y + "_Name");
                    TextMeshPro labelNameTMP = labelName.AddComponent<TextMeshPro>();
                    labelNameTMP.text = "L" + x.ToString() + y.ToString();
                    labelNameTMP.font = font;
                    labelNameTMP.fontSize = 400f;
                    labelNameTMP.color = Color.white;
                    labelNameTMP.alignment = TextAlignmentOptions.TopLeft;
                    labelName.transform.SetParent(panel.transform, false);
                    if (x == 0 && y == 0) labelNameTMP.text = "Vel:";
                    if (x == 1 && y == 0) labelNameTMP.text = "Alt:";
                    if (x == 0 && y == 1) labelNameTMP.text = "Rol:";
                    if (x == 1 && y == 1) labelNameTMP.text = "Pch:";

                    GameObject labelValue = new GameObject("Label_" + x + "_" + y + "_Value");
                    TextMeshPro labelValueTMP = labelValue.AddComponent<TextMeshPro>();
                    labelValueTMP.text = "V" + x.ToString() + y.ToString();
                    labelValueTMP.font = font;
                    labelValueTMP.fontSize = 400f;
                    labelValueTMP.color = Color.white;
                    labelValueTMP.alignment = TextAlignmentOptions.TopRight;
                    labelValue.transform.SetParent(panel.transform, false);
                    if (x == 0 && y == 0) dataLabelVelocityValue = labelValueTMP;
                    if (x == 1 && y == 0) dataLabelAltitudeValue = labelValueTMP;
                    if (x == 0 && y == 1) dataLabelRollValue = labelValueTMP;
                    if (x == 1 && y == 1) dataLabelPitchValue = labelValueTMP;

                    GameObject labelUnits = new GameObject("Label_" + x + "_" + y + "_Units");
                    TextMeshPro labelUnitsTMP = labelUnits.AddComponent<TextMeshPro>();
                    labelUnitsTMP.text = "U" + x.ToString() + y.ToString();
                    labelUnitsTMP.font = font;
                    labelUnitsTMP.fontSize = 400f;
                    labelUnitsTMP.color = Color.white;
                    labelUnitsTMP.alignment = TextAlignmentOptions.TopLeft;
                    labelUnits.transform.SetParent(panel.transform, false);
                    if (x == 0 && y == 0) dataLabelVelocityUnits = labelUnitsTMP;
                    if (x == 1 && y == 0) dataLabelAltitudeUnits = labelUnitsTMP;
                    if (x == 0 && y == 1) dataLabelRollUnits = labelUnitsTMP;
                    if (x == 1 && y == 1) dataLabelPitchUnits = labelUnitsTMP;
                }
            }

            TMP_FontAsset headingsFont = AssetLoader.Instance.GetTmpFont("Futura_Medium_BT");
            for (int i = 0; i < NUM_HEADING_LABELS; ++i) {
                headingLabels[i] = new GameObject("Heading_Label_" + i);
                headingLabelsTMP[i] = headingLabels[i].AddComponent<TextMeshPro>();
                headingLabelsTMP[i].text = i.ToString();
                headingLabelsTMP[i].font = headingsFont;
                headingLabelsTMP[i].fontSize = 300f;
                headingLabelsTMP[i].color = Color.white;
                headingLabelsTMP[i].alignment = TextAlignmentOptions.Center;
                headingLabelsTMP[i].transform.SetParent(screenCanvasGameObject.transform, false);
                headingLabelsTMP[i].rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                headingLabelsTMP[i].rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                headingLabelsTMP[i].rectTransform.pivot = new Vector2(0.5f, 0.5f);
                headingLabelsTMP[i].rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 300f);
                headingLabelsTMP[i].rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 100f);
            }
        }

        protected void CreateDebugObjects() {
            List<Vector2> linePoints = new List<Vector2>(5);
            linePoints.Add(new Vector2(0f, 0f));
            linePoints.Add(new Vector2(100f, 20f));
            linePoints.Add(new Vector2(110f, 30f));
            linePoints.Add(new Vector2(110f, 60f));
            linePoints.Add(new Vector2(150f, 0f));
            vLine = new VectorLine("KVR_Line", linePoints, 8f, LineType.Continuous, Joins.Weld);
            vLine.SetCanvas(screenCanvas, false);
            vLine.Draw();
        }

        protected void Update() {
            // do screen updates that need to be done every frame
            RedrawScreenFast();

            // redraw the screen at the specified refresh rate
            float currentTime = Time.time;
            if (currentTime > (lastScreenUpdateTime + refreshDrawRate)) {
                lastScreenUpdateTime = currentTime;
                RedrawScreenSlow();
            }
        }

        protected void RedrawScreenFast() {
            Vessel activeVessel = FlightGlobals.ActiveVessel;
            if (activeVessel != null) {
                // rotate the heading background according to roll
                float rollAngle = KerbalVR.Components.AvionicsComputer.Instance.RollAngle;
                headingImageAngle = Quaternion.Euler(0f, 0f, rollAngle);
                hudHeadingImage.rectTransform.localRotation = headingImageAngle;

                UpdateHeadingLabels();
            }
        }

        protected void UpdateHeadingLabels() {
            float yawAngle = KerbalVR.Components.AvionicsComputer.Instance.YawAngle;
            float headingRangeDegrees = 18f;
            float headingRangePixels = screenPixelWidth * 1.2f;
            float headingPositionEnd = headingRangePixels * 0.5f;
            float headingBucketSizeDegrees = headingRangeDegrees / NUM_HEADING_LABELS;
            float headingBucketSizePixels = headingRangePixels / NUM_HEADING_LABELS;

            float headingOffset = yawAngle % headingBucketSizeDegrees;
            float headingOffsetInt = Mathf.Floor(yawAngle / headingBucketSizeDegrees);
            float headingOffsetPixels = MathUtils.Map(headingOffset,
                0f, headingBucketSizeDegrees,
                0f, headingBucketSizePixels);

            for (int i = 0; i < NUM_HEADING_LABELS; ++i) {
                float headingLabelPosX = headingPositionEnd - headingBucketSizePixels * (i + 0.5f) - headingOffsetPixels;
                Vector3 headingLabelPos = headingImageAngle * new Vector3(headingLabelPosX, 0f, 0f);
                headingLabels[i].transform.localPosition = headingLabelPos;
                headingLabels[i].transform.localRotation = headingImageAngle;

                float headingValue = (headingOffsetInt + (NUM_HEADING_LABELS >> 1) - i) * headingBucketSizeDegrees;
                if (headingValue >= 360f) headingValue -= 360f;
                if (headingValue < 0f) headingValue += 360f;
                headingLabelsTMP[i].text = ((int)headingValue).ToString();
                // headingLabelsTMP[i].text = headingValue.ToString("F2");

                // modulate the font size
                float fontSizeAbs = MathUtils.Map(Mathf.Abs(headingLabelPosX), 0f, headingPositionEnd, 100f, 400f);
                headingLabelsTMP[i].fontSize = fontSizeAbs;

                // modulate the opacity
                float fontAlpha = MathUtils.Map(Mathf.Abs(headingLabelPosX), headingRangePixels * 0.1f, headingRangePixels * 0.6f, 0f, 1f);
                headingLabelsTMP[i].color = new Color(1f, 1f, 1f, fontAlpha);
            }
        }

        protected void RedrawScreenSlow() {
            Vessel activeVessel = FlightGlobals.ActiveVessel;
            if (activeVessel != null) {
                // get flight data
                float yawAngle = KerbalVR.Components.AvionicsComputer.Instance.YawAngle;
                float pitchAngle = KerbalVR.Components.AvionicsComputer.Instance.PitchAngle;
                float rollAngle = KerbalVR.Components.AvionicsComputer.Instance.RollAngle;

                Utils.HumanizeQuantity(activeVessel.GetSrfVelocity().magnitude, "m/s", out float velocity, out string velocityUnits);
                dataLabelVelocityValue.text = velocity.ToString("F0");
                dataLabelVelocityUnits.text = velocityUnits;

                Utils.HumanizeQuantity((float)activeVessel.altitude, "m", out float altitude, out string altitudeUnits);
                dataLabelAltitudeValue.text = altitude.ToString("F0");
                dataLabelAltitudeUnits.text = altitudeUnits;

                dataLabelRollValue.text = rollAngle.ToString("F1");
                dataLabelRollUnits.text = "\xF8";

                dataLabelPitchValue.text = yawAngle.ToString("F1");
                dataLabelPitchUnits.text = "\xF8";
            }
        }
    }
}
