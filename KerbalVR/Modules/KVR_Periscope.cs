using UnityEngine;
using KerbalVR.Components;

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

        // DEBUG
        private Vector3 cameraPos = new Vector3(-0.6091f, 0.5731f, -0.3286f);
        private Quaternion cameraRot = Quaternion.Euler(270f, 90f, 270f);
        //private GameObject crosshair1;

        void Start() {
            // create a camera
            scopeCameraGameObject = new GameObject(gameObject.name + " PeriscopeCamera");
            scopeCamera = scopeCameraGameObject.AddComponent<KVR_ExternalCamera>();

            // obtain the viewfinder
            viewfinderTransform = internalProp.FindModelTransform(transformViewfinder);
            if (viewfinderTransform != null) {
                viewfinderGameObject = viewfinderTransform.gameObject;
                MeshRenderer viewfinderMeshRenderer = viewfinderGameObject.GetComponent<MeshRenderer>();
                
                Material viewfinderMaterial = new Material(Shader.Find("KSP/Unlit"));
                viewfinderMaterial.mainTexture = scopeCamera.CameraRenderTexture;

                viewfinderMeshRenderer.sharedMaterial = viewfinderMaterial;
            } else {
                Utils.LogWarning("KVR_Periscope (" + gameObject.name + ") has no viewfinder \"" + transformViewfinder + "\"");
            }

            // DEBUG
            /*crosshair1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(crosshair1.GetComponent<BoxCollider>());
            crosshair1.transform.localScale = new Vector3(0.02f, 0.02f, 0.2f);
            crosshair1.transform.position = cameraPos + new Vector3(0f, 1f, 0f);
            Material crosshair1Mat = new Material(Shader.Find("KSP/Unlit"));
            crosshair1Mat.color = Color.cyan;
            crosshair1.GetComponent<MeshRenderer>().sharedMaterial = crosshair1Mat;*/
        }

        public override void OnUpdate() {
            // calculate scope repositioning. when your head moves down, the image moves down
            // x - right
            // y - forward
            // z - upwards
            
            Vector3 viewfinderDeltaPosition = viewfinderTransform.position - Scene.HmdPosition;
            Vector3 newScopePosition = new Vector3(cameraPos.x + viewfinderDeltaPosition.x, cameraPos.y, cameraPos.z);

            scopeCameraGameObject.transform.position = InternalSpace.InternalToWorld(newScopePosition);
            scopeCameraGameObject.transform.rotation = InternalSpace.InternalToWorld(cameraRot);

            // DEBUG
            //crosshair1.transform.position = InternalSpace.InternalToWorld(cameraPos + new Vector3(0f, 1f, 0f));
            //crosshair1.transform.rotation = InternalSpace.InternalToWorld(Quaternion.identity);
        }
    }
}
