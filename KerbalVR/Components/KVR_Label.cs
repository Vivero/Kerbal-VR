using System;
using UnityEngine;

namespace KerbalVR.Components
{
    public class KVR_Label
    {
        #region Properties
        public GameObject LabelGameObject { get; private set; }
        public string Text { get; private set; } = "";
        public Transform ParentTransform { get; private set; }
        public Vector3 PositionOffset { get; private set; } = Vector3.zero;
        public Quaternion RotationOffset { get; private set; } = Quaternion.identity;
        public TMPro.TMP_FontAsset Font { get; private set; }
        public float FontSize { get; private set; } = 0.1f;
        public TMPro.FontStyles FontStyle { get; private set; } = TMPro.FontStyles.Normal;
        public TMPro.TextAlignmentOptions TextAlignment { get; private set; } = TMPro.TextAlignmentOptions.Center;
        public Vector2 RectPivot { get; private set; } = new Vector2(0.5f, 0.5f);
        public Vector2 RectSize { get; private set; } = new Vector2(0.2f, 0.2f);
        #endregion

        #region Constructors
        public KVR_Label(InternalProp prop, ConfigNode configuration) {
            // label text
            string text = "";
            bool success = configuration.TryGetValue("text", ref text);
            if (success) Text = text;

            // label transform (where to place the label)
            ParentTransform = ConfigUtils.GetTransform(prop, configuration, "parentTransformName");

            // position offset from the transform
            Vector3 positionOffset = Vector3.zero;
            success = configuration.TryGetValue("positionOffset", ref positionOffset);
            if (success) PositionOffset = positionOffset;

            // rotation offset from the transform
            Quaternion rotationOffset = Quaternion.identity;
            success = configuration.TryGetValue("rotationOffset", ref rotationOffset);
            if (success) RotationOffset = rotationOffset;

            // font
            string fontName = "";
            success = configuration.TryGetValue("fontName", ref fontName);
            if (success) {
                Font = AssetLoader.Instance.GetFont(fontName);
                if (Font == null) {
                    Utils.LogWarning("KVR_Label: font \"" + fontName + "\" not found!");
                }
            }

            // font size
            float fontSize = 0.1f;
            success = configuration.TryGetValue("fontSize", ref fontSize);
            if (success) FontSize = fontSize;

            // font style (bold, italics, etc)
            TMPro.FontStyles fontStyle = TMPro.FontStyles.Normal;
            success = configuration.TryGetEnum("fontStyle", ref fontStyle, TMPro.FontStyles.Normal);
            if (success) FontStyle = fontStyle;

            // font alignment
            TMPro.TextAlignmentOptions textAlignment = TMPro.TextAlignmentOptions.Center;
            success = configuration.TryGetEnum("textAlignment", ref textAlignment, TMPro.TextAlignmentOptions.Center);
            if (success) TextAlignment = textAlignment;

            // canvas pivot (anchor point on the canvas)
            Vector2 rectPivot = new Vector2(0.5f, 0.5f);
            success = configuration.TryGetValue("rectPivot", ref rectPivot);
            if (success) RectPivot = rectPivot;

            // size of the label canvas
            Vector2 rectSize = new Vector2(0.2f, 0.2f);
            success = configuration.TryGetValue("rectSize", ref rectSize);
            if (success) RectSize = rectSize;

            // create the label
            string gameObjectName = prop.name + "-" + prop.propID + "-" + configuration.id;
            LabelGameObject = CreateLabel(
                gameObjectName,
                Text,
                Font,
                FontSize,
                FontStyle,
                TextAlignment,
                ParentTransform,
                PositionOffset,
                RotationOffset,
                RectPivot,
                RectSize);
        }
        #endregion

        protected GameObject CreateLabel(
            string gameObjectName,
            string text,
            TMPro.TMP_FontAsset font,
            float fontSize,
            TMPro.FontStyles fontStyle,
            TMPro.TextAlignmentOptions textAlignment,
            Transform parentTransform,
            Vector3 positionOffset,
            Quaternion rotationOffset,
            Vector2 rectPivot,
            Vector2 rectSize) {

            GameObject labelGameObject = new GameObject(gameObjectName);
            labelGameObject.layer = 20;
            labelGameObject.transform.SetParent(parentTransform);

            TMPro.TextMeshPro tmpLabel = labelGameObject.AddComponent<TMPro.TextMeshPro>();
            tmpLabel.SetText(text);
            tmpLabel.fontSize = fontSize;
            tmpLabel.fontStyle = fontStyle;
            tmpLabel.alignment = textAlignment;

            tmpLabel.rectTransform.pivot = rectPivot;
            tmpLabel.rectTransform.localPosition = positionOffset;
            tmpLabel.rectTransform.localRotation = rotationOffset;
            tmpLabel.rectTransform.sizeDelta = rectSize;

            // find and set font
            if (font != null) {
                tmpLabel.font = font;
            }

            return labelGameObject;
        }
    }
}
