using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    }
}
