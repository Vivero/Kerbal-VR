using System.Collections.Generic;
using UnityEngine;

namespace KerbalVR
{
    public class Types
    {
        // struct to keep track of Camera properties
        public struct CameraData
        {
            public Camera camera;
            public Matrix4x4 originalProjectionMatrix;
            public Matrix4x4 hmdProjectionMatrixL;
            public Matrix4x4 hmdProjectionMatrixR;
        }

        public struct VREyeCameraRig {
            public List<VREyeCamera> cameras;
        }

        public struct VREyeCamera {
            public GameObject cameraGameObject;
            public Camera cameraComponent;
            public string kspCameraName;
        }
    }
}
