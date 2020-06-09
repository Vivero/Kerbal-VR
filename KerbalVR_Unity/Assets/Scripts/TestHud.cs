using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TestHud : MonoBehaviour
{
    public Canvas canvas;

    float yawAngle = 0f;

    const int NUM_HEADING_LABELS = 9;
    GameObject[] headingLabels = new GameObject[NUM_HEADING_LABELS];
    TextMeshPro[] headingLabelsTMP = new TextMeshPro[NUM_HEADING_LABELS];
    TextMeshPro yawAngleLabelTMP;

    void Start()
    {
        GameObject yawAngleLabel = new GameObject("YawLabel");
        yawAngleLabelTMP = yawAngleLabel.AddComponent<TextMeshPro>();
        yawAngleLabelTMP.fontSize = 300f;
        yawAngleLabelTMP.color = Color.black;
        yawAngleLabelTMP.alignment = TextAlignmentOptions.Top;
        yawAngleLabelTMP.transform.SetParent(canvas.transform, false);
        yawAngleLabelTMP.rectTransform.anchorMin = new Vector2(0.5f, 1f);
        yawAngleLabelTMP.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        yawAngleLabelTMP.rectTransform.pivot = new Vector2(0.5f, 1f);
        yawAngleLabelTMP.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 200f);
        yawAngleLabelTMP.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 100f);

        for (int i = 0; i < NUM_HEADING_LABELS; ++i) {
            headingLabels[i] = new GameObject("HeadingLabel_" + i);
            headingLabelsTMP[i] = headingLabels[i].AddComponent<TextMeshPro>();
            headingLabelsTMP[i].text = i.ToString();
            headingLabelsTMP[i].fontSize = 200f;
            headingLabelsTMP[i].color = Color.black;
            headingLabelsTMP[i].alignment = TextAlignmentOptions.Center;
            headingLabelsTMP[i].transform.SetParent(canvas.transform, false);
            headingLabelsTMP[i].rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            headingLabelsTMP[i].rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            headingLabelsTMP[i].rectTransform.pivot = new Vector2(0.5f, 0.5f);
            headingLabelsTMP[i].rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 100f);
            headingLabelsTMP[i].rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 100f);
        }
    }

    // Update is called once per frame
    void Update()
    {
        float step = 0.08f;
        if (Input.GetKey(KeyCode.D)) {
            yawAngle += step;
        }
        if (Input.GetKey(KeyCode.A)) {
            yawAngle -= step;
        }
        if (Input.GetKeyDown(KeyCode.S)) {
            yawAngle = 0f;
        }
        if (yawAngle > 360f) {
            yawAngle -= 360f;
        }
        if (yawAngle < 0f) {
            yawAngle += 360f;
        }

        yawAngleLabelTMP.text = "Yaw: " + yawAngle.ToString("F2");
        int screenPixelWidth = 530;

        float headingRangeDegrees = 18f;
        float headingRangePixels = screenPixelWidth * 2f;
        float headingBucketSizeDegrees = headingRangeDegrees / NUM_HEADING_LABELS;
        float headingBucketSizePixels = headingRangePixels / NUM_HEADING_LABELS;

        float headingOffset = yawAngle % headingBucketSizeDegrees;
        float headingOffsetInt = Mathf.Floor(yawAngle / headingBucketSizeDegrees);
        float headingOffsetPixels = Map(headingOffset,
            0f, headingBucketSizeDegrees,
            0f, headingBucketSizePixels);

        for (int i = 0; i < NUM_HEADING_LABELS; ++i) {
            float headingLabelPosX = 0f - (i - (NUM_HEADING_LABELS >> 1)) * headingBucketSizePixels - headingOffsetPixels;
            Vector3 headingLabelPos = new Vector3(headingLabelPosX, 0f, 0f);
            headingLabels[i].transform.localPosition = headingLabelPos;

            float headingValue = (headingOffsetInt + (NUM_HEADING_LABELS >> 1) - i) * headingBucketSizeDegrees;
            if (headingValue >= 360f) headingValue -= 360f;
            if (headingValue < 0f) headingValue += 360f;
            // headingLabelsTMP[i].text = ((int)(headingValue)).ToString();
            headingLabelsTMP[i].text = headingValue.ToString("F2");
        }
    }

    public float Map(float value, float fromMin, float fromMax, float toMin, float toMax) {
        if (value <= fromMin) {
            return toMin;
        }
        else if (value >= fromMax) {
            return toMax;
        }
        return (value - fromMin) / (fromMax - fromMin) * (toMax - toMin) + toMin;
    }
}
