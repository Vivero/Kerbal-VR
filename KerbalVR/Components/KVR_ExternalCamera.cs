using UnityEngine;

namespace KerbalVR.Components
{
    public class KVR_ExternalCamera : MonoBehaviour
    {
        public static readonly int RENDER_TEXTURE_W = 512;
        public static readonly int RENDER_TEXTURE_H = 512;
        public static readonly int RENDER_TEXTURE_D = 24;
        public static readonly RenderTextureFormat RENDER_TEXTURE_FORMAT = RenderTextureFormat.ARGB32;

        // this camera renders the galaxy background
        public GameObject GalaxyCameraGameObject { get; private set; }
        public Camera GalaxyCamera { get; private set; }

        // this camera renders scaled space
        public GameObject ScaledSpaceCameraGameObject { get; private set; }
        public Camera ScaledSpaceCamera { get; private set; }

        // this camera renders worlds and vessels
        public GameObject MainCameraGameObject { get; private set; }
        public Camera MainCamera { get; private set; }

        // render target
        public RenderTexture CameraRenderTexture { get; private set; }

        // camera parameters
        public float _fieldOfView;
        public float FieldOfView {
            get { return _fieldOfView; }
            set {
                _fieldOfView = value;
                GalaxyCamera.fieldOfView = _fieldOfView;
                ScaledSpaceCamera.fieldOfView = _fieldOfView;
                MainCamera.fieldOfView = _fieldOfView;
            }
        }

        void Awake() {
            // initialize cameras
            GalaxyCameraGameObject = new GameObject("KVR_ExternalCamera_GalaxyCamera");
            GalaxyCameraGameObject.transform.SetParent(transform);
            GalaxyCameraGameObject.transform.localPosition = Vector3.zero;
            GalaxyCameraGameObject.transform.localRotation = Quaternion.identity;
            GalaxyCamera = GalaxyCameraGameObject.AddComponent<Camera>();
            GalaxyCamera.cullingMask = (1 << 18); // layer 18: SkySphere
            GalaxyCamera.depth = 60;
            GalaxyCamera.clearFlags = CameraClearFlags.Color;
            GalaxyCamera.backgroundColor = Color.red;
            GalaxyCamera.nearClipPlane = 0.1f;
            GalaxyCamera.farClipPlane = 20f;
            GalaxyCamera.enabled = true;

            ScaledSpaceCameraGameObject = new GameObject("KVR_ExternalCamera_ScaledSpaceCamera");
            ScaledSpaceCameraGameObject.transform.SetParent(transform);
            ScaledSpaceCameraGameObject.transform.localPosition = Vector3.zero;
            ScaledSpaceCameraGameObject.transform.localRotation = Quaternion.identity;
            ScaledSpaceCamera = ScaledSpaceCameraGameObject.AddComponent<Camera>();
            ScaledSpaceCamera.cullingMask =
                (1 << 9) | // layer 9: Atmosphere
                (1 << 10); // layer 10: Scaled Scenery
            ScaledSpaceCamera.depth = 61;
            ScaledSpaceCamera.clearFlags = CameraClearFlags.Nothing;
            ScaledSpaceCamera.nearClipPlane = 1f;
            ScaledSpaceCamera.farClipPlane = 30000000f;
            ScaledSpaceCamera.enabled = true;

            MainCameraGameObject = new GameObject("KVR_ExternalCamera_MainCamera");
            MainCameraGameObject.transform.SetParent(transform);
            MainCameraGameObject.transform.localPosition = Vector3.zero;
            MainCameraGameObject.transform.localRotation = Quaternion.identity;
            MainCamera = MainCameraGameObject.AddComponent<Camera>();
            MainCamera.cullingMask =
                (1 << 0) | // layer 0: Default
                (1 << 1) | // layer 1: TransparentFX
                (1 << 4) | // layer 4: Water
                (1 << 9) | // layer 9: Atmosphere
                (1 << 10) | // layer 10: Scaled Scenery
                (1 << 15) | // layer 15: Local Scenery
                (1 << 17) | // layer 17: EVA
                (1 << 19) | // layer 19: PhysicalObjects
                (1 << 23); // layer 23: AeroFXIgnore
            MainCamera.depth = 62; // draw on top of GalaxyCamera
            MainCamera.clearFlags = CameraClearFlags.Nothing;
            MainCamera.nearClipPlane = 0.21f;
            MainCamera.farClipPlane = 750000f;
            MainCamera.enabled = true;

            // initialize render target
            CameraRenderTexture = new RenderTexture(
                RENDER_TEXTURE_W, RENDER_TEXTURE_H, RENDER_TEXTURE_D, RENDER_TEXTURE_FORMAT);
            GalaxyCamera.targetTexture = CameraRenderTexture;
            ScaledSpaceCamera.targetTexture = CameraRenderTexture;
            MainCamera.targetTexture = CameraRenderTexture;

            // set camera parameters
            FieldOfView = 60f;
        }
    }
}
