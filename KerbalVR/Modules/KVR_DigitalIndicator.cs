using UnityEngine;

namespace KerbalVR.Modules
{
    public class KVR_DigitalIndicator : InternalModule
    {
        #region KSP Config Fields
        [KSPField]
        public string displayTransform = string.Empty;
        [KSPField]
        public Vector3 displayOffset = Vector3.zero;
        [KSPField]
        public Vector2 displaySize = Vector2.zero;
        [KSPField]
        public Vector2 displayPivot = Vector2.zero;
        [KSPField]
        public float fontSize = 0.2f;
        [KSPField]
        public string font = "LiberationSans SDF";
        #endregion

        #region Private Members
        private GameObject displayGameObject;
        #endregion


        void Start() {
            // no setup needed in editor mode
            if (HighLogic.LoadedScene == GameScenes.EDITOR) return;

            // retrieve the display transform
            Transform displayTransform = internalProp.FindModelTransform(this.displayTransform);
            if (displayTransform != null) {
                displayGameObject = displayTransform.gameObject;
            } else {
                Utils.LogWarning("KVR_DigitalIndicator (" + gameObject.name + ") has no display transform \"" + this.displayTransform + "\"");
            }

            // create labels
            CreateLabels();
        }

        private void CreateLabels() {
            GameObject displayTextGameObject = CreateLabel(
                0, "00123", fontSize, TMPro.FontStyles.Normal,
                TMPro.TextAlignmentOptions.Left, displayGameObject.transform,
                displayOffset, Quaternion.identity, displayPivot, displaySize);
        }

        private GameObject CreateLabel(
            int id,
            string text,
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
            TMPro.TMP_FontAsset tmpFont = AssetLoader.Instance.GetFont(font);
            if (tmpFont != null) {
                tmpLabel.font = tmpFont;
            } else {
                Utils.LogWarning("KVR_DigitalIndicator font not found!");
            }

            return labelGameObject;
        }
    }
}
