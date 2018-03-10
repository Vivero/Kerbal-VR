using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;
using Valve.VR;

namespace KerbalVR
{
    class Utils
    {
        private static readonly string LOG_PREFIX = "[KerbalVR] ";

        // define location of OpenVR library
        public static string OpenVRDllPath {
            get {
                string currentPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string openVrPath = Path.Combine(currentPath, "openvr");
                return Path.Combine(openVrPath, Utils.Is64BitProcess ? "win64" : "win32");
            }
        }

        public static Component GetOrAddComponent<T>(GameObject obj) where T : Component {
            Component c = obj.GetComponent<T>();
            if (c == null) {
                c = obj.AddComponent<T>();
            }
            return c;
        }

        public static void LogInfo(object obj) {
            Debug.Log(LOG_PREFIX + obj);
        }

        public static void LogWarning(object obj) {
            Debug.LogWarning(LOG_PREFIX + obj);
        }

        public static void LogError(object obj) {
            Debug.LogError(LOG_PREFIX + obj);
        }

        public static bool Is64BitProcess {
            get { return (IntPtr.Size == 8); }
        }

        public static Mesh CreateHiddenAreaMesh(HiddenAreaMesh_t src, VRTextureBounds_t bounds) {
            if (src.unTriangleCount == 0)
                return null;

            var data = new float[src.unTriangleCount * 3 * 2]; //HmdVector2_t
            Marshal.Copy(src.pVertexData, data, 0, data.Length);

            var vertices = new Vector3[src.unTriangleCount * 3 + 12];
            var indices = new int[src.unTriangleCount * 3 + 24];

            var x0 = 2.0f * bounds.uMin - 1.0f;
            var x1 = 2.0f * bounds.uMax - 1.0f;
            var y0 = 2.0f * bounds.vMin - 1.0f;
            var y1 = 2.0f * bounds.vMax - 1.0f;

            for (int i = 0, j = 0; i < src.unTriangleCount * 3; i++) {
                var x = SteamVR_Utils.Lerp(x0, x1, data[j++]);
                var y = SteamVR_Utils.Lerp(y0, y1, data[j++]);
                vertices[i] = new Vector3(x, y, 0.0f);
                indices[i] = i;
            }

            // Add border
            var offset = (int)src.unTriangleCount * 3;
            var iVert = offset;
            vertices[iVert++] = new Vector3(-1, -1, 0);
            vertices[iVert++] = new Vector3(x0, -1, 0);
            vertices[iVert++] = new Vector3(-1, 1, 0);
            vertices[iVert++] = new Vector3(x0, 1, 0);
            vertices[iVert++] = new Vector3(x1, -1, 0);
            vertices[iVert++] = new Vector3(1, -1, 0);
            vertices[iVert++] = new Vector3(x1, 1, 0);
            vertices[iVert++] = new Vector3(1, 1, 0);
            vertices[iVert++] = new Vector3(x0, y0, 0);
            vertices[iVert++] = new Vector3(x1, y0, 0);
            vertices[iVert++] = new Vector3(x0, y1, 0);
            vertices[iVert++] = new Vector3(x1, y1, 0);

            var iTri = offset;
            indices[iTri++] = offset + 0;
            indices[iTri++] = offset + 1;
            indices[iTri++] = offset + 2;
            indices[iTri++] = offset + 2;
            indices[iTri++] = offset + 1;
            indices[iTri++] = offset + 3;
            indices[iTri++] = offset + 4;
            indices[iTri++] = offset + 5;
            indices[iTri++] = offset + 6;
            indices[iTri++] = offset + 6;
            indices[iTri++] = offset + 5;
            indices[iTri++] = offset + 7;
            indices[iTri++] = offset + 1;
            indices[iTri++] = offset + 4;
            indices[iTri++] = offset + 8;
            indices[iTri++] = offset + 8;
            indices[iTri++] = offset + 4;
            indices[iTri++] = offset + 9;
            indices[iTri++] = offset + 10;
            indices[iTri++] = offset + 11;
            indices[iTri++] = offset + 3;
            indices[iTri++] = offset + 3;
            indices[iTri++] = offset + 11;
            indices[iTri++] = offset + 6;

            var mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.triangles = indices;
            mesh.bounds = new Bounds(Vector3.zero, new Vector3(float.MaxValue, float.MaxValue, float.MaxValue)); // Prevent frustum culling from culling this mesh
            return mesh;
        }
    }
}
