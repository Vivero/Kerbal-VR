using UnityEngine;
using KerbalVR.Components;
using Valve.VR;

namespace KerbalVR.Modules
{
    public class KVR_Periscope : InternalModule
    {
        [KSPField]
        public string transformViewfinder = string.Empty;

        // viewfinder object (where you look through)
        private GameObject viewfinderGameObject;
        private Transform viewfinderTransform;

        // camera object for this scope
        private GameObject scopeCameraGameObject;
        private KVR_ExternalCamera scopeCamera;

        // camera positioning
        // private Vector3 cameraPos = new Vector3(-0.6091f, 0.5731f, -0.3286f);
        private Vector3 cameraPos = new Vector3(0f, 2f, 0f);
        private Quaternion cameraRot = Quaternion.Euler(270f, 90f, 270f);
        private GameObject crosshair;
        float parallaxFactor = 200f;
        float crosshairOffset = 0.25f; // meters
        // float crosshairOffset = 19f;
        float crosshairSize;

        void Start() {
            // create a camera
            scopeCameraGameObject = new GameObject(gameObject.name + " PeriscopeCamera");
            scopeCamera = scopeCameraGameObject.AddComponent<KVR_ExternalCamera>();
            scopeCamera.Magnification = 1.2f;
            crosshairSize = scopeCamera.GetFrustumHeight(crosshairOffset);

            // obtain the viewfinder
            viewfinderTransform = internalProp.FindModelTransform(transformViewfinder);
            if (viewfinderTransform != null) {
                viewfinderGameObject = viewfinderTransform.gameObject;
                Vector3 lensPos = viewfinderGameObject.transform.localPosition;
                lensPos.y = 0.04f;
                //viewfinderGameObject.transform.localPosition = lensPos;
                MeshRenderer viewfinderMeshRenderer = viewfinderGameObject.GetComponent<MeshRenderer>();
                
                Material viewfinderMaterial = new Material(Shader.Find("KSP/Unlit"));
                viewfinderMaterial.mainTexture = scopeCamera.CameraRenderTexture;

                viewfinderMeshRenderer.sharedMaterial = viewfinderMaterial;
            } else {
                Utils.LogWarning("KVR_Periscope (" + gameObject.name + ") has no viewfinder \"" + transformViewfinder + "\"");
            }

            crosshair = GameObject.CreatePrimitive(PrimitiveType.Plane);
            Destroy(crosshair.GetComponent<MeshCollider>());
            crosshair.transform.localScale = Vector3.one * 0.1f * crosshairSize * 1f;
            crosshair.layer = 0;

            MeshRenderer rend = crosshair.GetComponent<MeshRenderer>();
            Material mat = new Material(Shader.Find("KSP/Specular (Transparent)"));
            mat.color = Color.cyan;
            rend.sharedMaterial = mat;
            Texture2D tex = GameDatabase.Instance.GetTexture("KerbalVR/Assets/Props/Periscope/periscope_crosshair", false);
            if (tex != null) {
                mat.mainTexture = tex;
            }

            // DEBUG
            // Utils.PrintGameObjectTree(gameObject);
            // cameraGizmo = Utils.CreateGizmo();
            // cameraGizmo.transform.localScale = Vector3.one * 2f;
        }

        public void OnUpdate2() {
            // calculate scope repositioning. when your head moves down, the image moves down
            // x - right
            // y - forward
            // z - downwards

            /*Vector3 viewfinderDeltaPositionL = viewfinderTransform.InverseTransformPoint(
                Scene.Instance.HmdEyePosition[(int)EVREye.Eye_Left]);
            Vector3 viewfinderDeltaPositionR = viewfinderTransform.InverseTransformPoint(
                Scene.Instance.HmdEyePosition[(int)EVREye.Eye_Right]);
            Vector3 viewfinderDeltaPosition =
                (viewfinderDeltaPositionL.sqrMagnitude < viewfinderDeltaPositionR.sqrMagnitude) ?
                viewfinderDeltaPositionL : viewfinderDeltaPositionR;*/
            Vector3 viewfinderDeltaPosition = viewfinderTransform.InverseTransformPoint(
                Scene.Instance.HmdEyePosition[(int)EVREye.Eye_Right]);

            Vector3 scopeDeltaPosition = cameraRot * new Vector3(
                viewfinderDeltaPosition.x * parallaxFactor, viewfinderDeltaPosition.z * parallaxFactor, 0f);

            Vector3 newScopePosition = cameraPos + scopeDeltaPosition;

            scopeCameraGameObject.transform.position = InternalSpace.InternalToWorld(newScopePosition);
            scopeCameraGameObject.transform.rotation = InternalSpace.InternalToWorld(cameraRot);

            // crosshair positioning
            crosshair.transform.position = InternalSpace.InternalToWorld(newScopePosition + cameraRot *
                new Vector3(0f, 0f, crosshairOffset));
            crosshair.transform.rotation = InternalSpace.InternalToWorld(cameraRot * Quaternion.Euler(-90f, 0f, 0f));

            // DEBUG
            /*if (cameraGizmo == null) {
                cameraGizmo = Utils.CreateGizmo();
                cameraGizmo.transform.localScale = Vector3.one * 0.75f;
                DontDestroyOnLoad(cameraGizmo);
            }
            cameraGizmo.transform.position = cameraPos;
            cameraGizmo.transform.rotation = cameraRot;*/
        }

        public override void OnUpdate() {

            // camera position/rotation in internal space
            // Vector3 internalCameraPos = Scene.Instance.HmdEyePosition[(int)EVREye.Eye_Right] + Scene.Instance.HmdEyeRotation[(int)EVREye.Eye_Right] * new Vector3(0f, 0f, 0.3f);
            Vector3 internalCameraPos = Scene.Instance.HmdEyePosition[(int)EVREye.Eye_Right];

            Quaternion internalCameraRot = Quaternion.LookRotation(viewfinderTransform.position -
                internalCameraPos, new Vector3(0f, 0f, -1f));

            // offset to the view position on the craft
            Vector3 offsetCameraPos = new Vector3(0f, 2f, 0f);

            internalCameraPos += offsetCameraPos;

            Vector3 worldCameraPos = InternalSpace.InternalToWorld(internalCameraPos);
            Quaternion worldCameraRot = InternalSpace.InternalToWorld(internalCameraRot);

            scopeCameraGameObject.transform.position = worldCameraPos;
            scopeCameraGameObject.transform.rotation = worldCameraRot;

            // crosshair positioning
            Vector3 crosshairRelPosition = new Vector3(0f, 0f, crosshairOffset);
            Vector3 crosshairPosition = internalCameraPos + internalCameraRot * crosshairRelPosition;
            Quaternion crosshairRotation = internalCameraRot * Quaternion.Euler(-90f, 0f, 0f);

            crosshair.transform.position = InternalSpace.InternalToWorld(crosshairPosition);
            crosshair.transform.rotation = InternalSpace.InternalToWorld(crosshairRotation);
            // crosshair.transform.position = InternalSpace.InternalToWorld(new Vector3(0f, 24f, 0f));
            // crosshair.transform.rotation = InternalSpace.InternalToWorld(Quaternion.Euler(180f, 0f, 0f));

            /*if (cameraGizmo == null) {
                cameraGizmo = Utils.CreateGizmo();
                cameraGizmo.transform.localScale = Vector3.one * 0.75f;
                DontDestroyOnLoad(cameraGizmo);
            }
            cameraGizmo.transform.position = internalCameraPos;
            cameraGizmo.transform.rotation = internalCameraRot;*/
        }
    }
}
