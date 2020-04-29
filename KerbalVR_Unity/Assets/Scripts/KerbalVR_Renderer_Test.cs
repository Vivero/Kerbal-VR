using System;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;
using Valve.VR;

public class KerbalVR_Renderer_Test : MonoBehaviour {

    // import KerbalVR_Renderer plugin functions
    [DllImport("KerbalVR_Renderer")]
    private static extern IntPtr GetRenderEventFunc();

    [DllImport("KerbalVR_Renderer")]
    private static extern void SetTextureFromUnity(
        int textureIndex,
        System.IntPtr textureHandle,
        float boundsUMin,
        float boundsUMax,
        float boundsVMin,
        float boundsVMax);

    // store camera components, one per eye
    public GameObject VrCameraLeft;
    public GameObject VrCameraRight;

    protected Camera vrCameraLeftComponent;
    protected Camera vrCameraRightComponent;

    protected RenderTexture vrCameraLeftRT;
    protected RenderTexture vrCameraRightRT;

    // store the tracked device poses
    protected TrackedDevicePose_t[] devicePoses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
    protected TrackedDevicePose_t[] renderPoses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
    protected TrackedDevicePose_t[] gamePoses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];


    // Start is called before the first frame update
    IEnumerator Start() {
        // check if HMD is connected on the system
        if (!OpenVR.IsHmdPresent()) {
            Debug.LogError("HMD not found on this system");
        }

        // check if SteamVR runtime is installed
        if (!OpenVR.IsRuntimeInstalled()) {
            Debug.LogError("SteamVR runtime not found on this system");
        }

        // init openVR
        EVRInitError hmdInitErrorCode = EVRInitError.None;
        OpenVR.Init(ref hmdInitErrorCode, EVRApplicationType.VRApplication_Scene);
        if (hmdInitErrorCode != EVRInitError.None) {
            Debug.LogError("OpenVR error: " + OpenVR.GetStringForHmdError(hmdInitErrorCode));
        }

        // get HMD render target size
        uint renderTextureWidth = 0;
        uint renderTextureHeight = 0;
        OpenVR.System.GetRecommendedRenderTargetSize(ref renderTextureWidth, ref renderTextureHeight);
        Debug.Log("OpenVR texture size: " + renderTextureWidth + " x " + renderTextureHeight);

        // create the render textures that will be passed to the KerbalVR_Renderer plugin
        vrCameraLeftRT = new RenderTexture((int)renderTextureWidth, (int)renderTextureHeight, 24, RenderTextureFormat.ARGB32);
        vrCameraLeftRT.Create();
        vrCameraLeftComponent = VrCameraLeft.GetComponent<Camera>();
        vrCameraLeftComponent.targetTexture = vrCameraLeftRT;

        vrCameraRightRT = new RenderTexture((int)renderTextureWidth, (int)renderTextureHeight, 24, RenderTextureFormat.ARGB32);
        vrCameraRightRT.Create();
        vrCameraRightComponent = VrCameraRight.GetComponent<Camera>();
        vrCameraRightComponent.targetTexture = vrCameraRightRT;

        // set projection matrices accordingly
        HmdMatrix44_t projectionMatrixL = OpenVR.System.GetProjectionMatrix(EVREye.Eye_Left, vrCameraLeftComponent.nearClipPlane, vrCameraLeftComponent.farClipPlane);
        HmdMatrix44_t projectionMatrixR = OpenVR.System.GetProjectionMatrix(EVREye.Eye_Right, vrCameraRightComponent.nearClipPlane, vrCameraRightComponent.farClipPlane);

        vrCameraLeftComponent.projectionMatrix = Matrix4x4_OpenVr2UnityFormat(ref projectionMatrixL);
        vrCameraRightComponent.projectionMatrix = Matrix4x4_OpenVr2UnityFormat(ref projectionMatrixR);

        // send the textures to the plugin
        SetTextureFromUnity(0, vrCameraLeftRT.GetNativeTexturePtr(), 0f, 1f, 1f, 0f);
        SetTextureFromUnity(1, vrCameraRightRT.GetNativeTexturePtr(), 0f, 1f, 1f, 0f);

        yield return StartCoroutine("CallPluginAtEndOfFrames");
    }

    IEnumerator CallPluginAtEndOfFrames() {
        while (true) {
            // Wait until all frame rendering is done
            yield return new WaitForEndOfFrame();

            float secondsToPhotons = CalculatePredictedSecondsToPhotons();
            OpenVR.System.GetDeviceToAbsoluteTrackingPose(ETrackingUniverseOrigin.TrackingUniverseSeated, secondsToPhotons, devicePoses);

            HmdMatrix34_t vrLeftEyeTransform = OpenVR.System.GetEyeToHeadTransform(EVREye.Eye_Left);
            HmdMatrix34_t vrRightEyeTransform = OpenVR.System.GetEyeToHeadTransform(EVREye.Eye_Right);

            EVRCompositorError vrCompositorError = OpenVR.Compositor.GetLastPoses(renderPoses, gamePoses);
            // EVRCompositorError vrCompositorError = OpenVR.Compositor.WaitGetPoses(renderPoses, gamePoses);
            if (vrCompositorError != EVRCompositorError.None) {
                Debug.LogError("Compositor error: " + vrCompositorError.ToString());
                continue;
            }

            var hmdTransform = new SteamVR_Utils.RigidTransform(gamePoses[OpenVR.k_unTrackedDeviceIndex_Hmd].mDeviceToAbsoluteTracking);
            SteamVR_Utils.RigidTransform[] hmdEyeTransform = new SteamVR_Utils.RigidTransform[2];
            hmdEyeTransform[0] = new SteamVR_Utils.RigidTransform(vrLeftEyeTransform);
            hmdEyeTransform[1] = new SteamVR_Utils.RigidTransform(vrRightEyeTransform);

            Vector3[] positionToEye = new Vector3[2];
            positionToEye[0] = hmdTransform.pos + hmdTransform.rot * hmdEyeTransform[0].pos;
            positionToEye[1] = hmdTransform.pos + hmdTransform.rot * hmdEyeTransform[1].pos;

            VrCameraLeft.transform.position = positionToEye[0];
            VrCameraLeft.transform.rotation = hmdTransform.rot;
            VrCameraRight.transform.position = positionToEye[1];
            VrCameraRight.transform.rotation = hmdTransform.rot;

            // Issue a plugin event with arbitrary integer identifier.
            // The plugin can distinguish between different
            // things it needs to do based on this ID.
            // For our simple plugin, it does not matter which ID we pass here.
            GL.IssuePluginEvent(GetRenderEventFunc(), 1);
        }
    }

    protected void OnDestroy() {
        Debug.Log("Shutting down OpenVR");
        OpenVR.Shutdown();
    }


    //
    // Utility functions
    //

    protected Matrix4x4 Matrix4x4_OpenVr2UnityFormat(ref HmdMatrix44_t mat44_openvr) {
        Matrix4x4 mat44_unity = Matrix4x4.identity;
        mat44_unity.m00 = mat44_openvr.m0;
        mat44_unity.m01 = mat44_openvr.m1;
        mat44_unity.m02 = mat44_openvr.m2;
        mat44_unity.m03 = mat44_openvr.m3;
        mat44_unity.m10 = mat44_openvr.m4;
        mat44_unity.m11 = mat44_openvr.m5;
        mat44_unity.m12 = mat44_openvr.m6;
        mat44_unity.m13 = mat44_openvr.m7;
        mat44_unity.m20 = mat44_openvr.m8;
        mat44_unity.m21 = mat44_openvr.m9;
        mat44_unity.m22 = mat44_openvr.m10;
        mat44_unity.m23 = mat44_openvr.m11;
        mat44_unity.m30 = mat44_openvr.m12;
        mat44_unity.m31 = mat44_openvr.m13;
        mat44_unity.m32 = mat44_openvr.m14;
        mat44_unity.m33 = mat44_openvr.m15;
        return mat44_unity;
    }

    public float CalculatePredictedSecondsToPhotons() {
        float secondsSinceLastVsync = 0f;
        ulong frameCounter = 0;
        OpenVR.System.GetTimeSinceLastVsync(ref secondsSinceLastVsync, ref frameCounter);

        float displayFrequency = GetFloatTrackedDeviceProperty(ETrackedDeviceProperty.Prop_DisplayFrequency_Float);
        float vsyncToPhotons = GetFloatTrackedDeviceProperty(ETrackedDeviceProperty.Prop_SecondsFromVsyncToPhotons_Float);
        float frameDuration = 1f / displayFrequency;

        return frameDuration - secondsSinceLastVsync + vsyncToPhotons;
    }

    public float GetFloatTrackedDeviceProperty(ETrackedDeviceProperty property, uint device = OpenVR.k_unTrackedDeviceIndex_Hmd) {
        ETrackedPropertyError propertyError = ETrackedPropertyError.TrackedProp_Success;
        float value = OpenVR.System.GetFloatTrackedDeviceProperty(device, property, ref propertyError);
        if (propertyError != ETrackedPropertyError.TrackedProp_Success) {
            throw new Exception("Failed to obtain tracked device property \"" +
                property + "\", error: (" + (int)propertyError + ") " + propertyError.ToString());
        }
        return value;
    }
}
