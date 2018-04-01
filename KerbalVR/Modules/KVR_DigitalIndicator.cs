using UnityEngine;

namespace KerbalVR.Modules
{
    public class KVR_DigitalIndicator : InternalModule
    {
        #region KSP Config Fields

        [KSPField]
        public string labelDisplayText = string.Empty;
        [KSPField]
        public string labelDisplayTransform = string.Empty;
        [KSPField]
        public Vector3 labelDisplayOffset = Vector3.zero;
        [KSPField]
        public Vector2 labelDisplaySize = Vector2.zero;
        [KSPField]
        public string labelDisplayFontStyle = string.Empty;
        [KSPField]
        public Vector2 labelDisplayPivot = Vector2.zero;
        [KSPField]
        public float labelDisplayFontSize = 0.1f;
        [KSPField]
        public string labelDisplayFont = "LiberationSans SDF";

        [KSPField]
        public string digitDisplayTransform = string.Empty;
        [KSPField]
        public Vector3 digitDisplayOffset = Vector3.zero;
        [KSPField]
        public Vector2 digitDisplaySize = Vector2.zero;
        [KSPField]
        public Vector2 digitDisplayPivot = Vector2.zero;
        [KSPField]
        public float digitDisplayFontSize = 0.2f;
        [KSPField]
        public string digitDisplayFont = "JD-LCD_rounded SDF";

        [KSPField]
        public string inputSignal = string.Empty;

        #endregion


        #region Private Members

        private ConfigNode moduleConfigNode;

        private GameObject labelDisplayGameObject;
        private TMPro.TextMeshPro labelDisplayTextLabel;

        private GameObject digitDisplayGameObject;
        private TMPro.TextMeshPro digitDisplayTextLabel;

        private Events.Action avionicsUpdatedAction;

        #endregion

        void Awake() {
            if (!string.IsNullOrEmpty(inputSignal)) {
                avionicsUpdatedAction = KerbalVR.Events.AvionicsFloatAction(inputSignal, OnAvionicsInput);
            }
        }

        void Start() {
            // no setup needed in editor mode
            if (HighLogic.LoadedScene == GameScenes.EDITOR) return;

            // get configuration
            moduleConfigNode = ConfigUtils.GetModuleConfigNode(internalProp.name, moduleID);

            // retrieve the display transform
            Transform digitDisplayTransform = internalProp.FindModelTransform(this.digitDisplayTransform);
            if (digitDisplayTransform != null) {
                digitDisplayGameObject = digitDisplayTransform.gameObject;
            } else {
                Utils.LogWarning("KVR_DigitalIndicator (" + gameObject.name + ") has no display transform \"" + this.digitDisplayTransform + "\"");
            }

            Transform labelDisplayTransform = internalProp.FindModelTransform(this.labelDisplayTransform);
            if (labelDisplayTransform != null) {
                labelDisplayGameObject = labelDisplayTransform.gameObject;
            } else {
                Utils.LogWarning("KVR_DigitalIndicator (" + gameObject.name + ") has no label transform \"" + this.labelDisplayTransform + "\"");
            }

            // create labels
            CreateLabels();
        }

        void OnEnable() {
            if (avionicsUpdatedAction != null) {
                avionicsUpdatedAction.enabled = true;
            }
        }

        void OnDisable() {
            if (avionicsUpdatedAction != null) {
                avionicsUpdatedAction.enabled = false;
            }
        }

        void OnAvionicsInput(float input) {
            digitDisplayTextLabel.text = input.ToString("F0");
        }

        private void CreateLabels() {

            // font style (bold, italics, etc)
            TMPro.FontStyles labelDisplayFontStyle = TMPro.FontStyles.Normal;
            bool success = moduleConfigNode.TryGetEnum("labelDisplayFontStyle",
                ref labelDisplayFontStyle, TMPro.FontStyles.Normal);

            // label
            GameObject labelTextGameObject = CreateLabel(
                0, labelDisplayText, labelDisplayFont, labelDisplayFontSize, labelDisplayFontStyle,
                TMPro.TextAlignmentOptions.TopLeft, labelDisplayGameObject.transform,
                labelDisplayOffset, Quaternion.identity, labelDisplayPivot, labelDisplaySize);
            labelDisplayTextLabel = labelDisplayGameObject.GetComponent<TMPro.TextMeshPro>();

            // digit display
            GameObject displayTextGameObject = CreateLabel(
                1, "0.0", digitDisplayFont, digitDisplayFontSize, TMPro.FontStyles.Normal,
                TMPro.TextAlignmentOptions.TopLeft, digitDisplayGameObject.transform,
                digitDisplayOffset, Quaternion.identity, digitDisplayPivot, digitDisplaySize);
            digitDisplayTextLabel = displayTextGameObject.GetComponent<TMPro.TextMeshPro>();
        }

        private GameObject CreateLabel(
            int id,
            string text,
            string fontName,
            float fontSize,
            TMPro.FontStyles fontStyle,
            TMPro.TextAlignmentOptions alignment,
            Transform labelTransform,
            Vector3 labelPositionOffset,
            Quaternion labelRotationOffset,
            Vector2 rectPivot,
            Vector2 rectSize) {

            string labelName = internalProp.name + "-" + internalProp.propID + "-" + id;

            GameObject labelGameObject = new GameObject(labelName);
            labelGameObject.layer = 20;
            labelGameObject.transform.SetParent(labelTransform);

            TMPro.TextMeshPro tmpLabel = labelGameObject.AddComponent<TMPro.TextMeshPro>();
            tmpLabel.SetText(text);
            tmpLabel.fontSize = fontSize;
            tmpLabel.fontStyle = fontStyle;
            tmpLabel.alignment = alignment;

            tmpLabel.rectTransform.pivot = rectPivot;
            tmpLabel.rectTransform.localPosition = labelPositionOffset;
            tmpLabel.rectTransform.localRotation = labelRotationOffset;
            tmpLabel.rectTransform.sizeDelta = rectSize;

            // find and set font
            TMPro.TMP_FontAsset tmpFont = AssetLoader.Instance.GetFont(fontName);
            if (tmpFont != null) {
                tmpLabel.font = tmpFont;
            } else {
                Utils.LogWarning("KVR_DigitalIndicator font not found!");
            }

            return labelGameObject;
        }
    }
}
