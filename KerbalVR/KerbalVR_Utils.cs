using System;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

namespace KerbalVR
{
    /// <summary>
    /// A class to contain general utility functions.
    /// </summary>
    public class Utils
    {
        public static Component GetOrAddComponent<T>(GameObject obj) where T : Component {
            Component c = obj.GetComponent<T>();
            if (c == null) {
                c = obj.AddComponent<T>();
            }
            return c;
        }

        public static void Log(object obj) {
            Debug.Log(Globals.LOG_PREFIX + obj);
        }

        public static void LogWarning(object obj) {
            Debug.LogWarning(Globals.LOG_PREFIX + obj);
        }

        public static void LogError(object obj) {
            Debug.LogError(Globals.LOG_PREFIX + obj);
        }

        public static bool Is64BitProcess {
            get { return (IntPtr.Size == 8); }
        }

        public static float CalculatePredictedSecondsToPhotons() {
            float secondsSinceLastVsync = 0f;
            ulong frameCounter = 0;
            OpenVR.System.GetTimeSinceLastVsync(ref secondsSinceLastVsync, ref frameCounter);

            float displayFrequency = GetFloatTrackedDeviceProperty(ETrackedDeviceProperty.Prop_DisplayFrequency_Float);
            float vsyncToPhotons = GetFloatTrackedDeviceProperty(ETrackedDeviceProperty.Prop_SecondsFromVsyncToPhotons_Float);
            float frameDuration = 1f / displayFrequency;

            return frameDuration - secondsSinceLastVsync + vsyncToPhotons;
        }

        public static float GetFloatTrackedDeviceProperty(ETrackedDeviceProperty property, uint device = OpenVR.k_unTrackedDeviceIndex_Hmd) {
            ETrackedPropertyError propertyError = ETrackedPropertyError.TrackedProp_Success;
            float value = OpenVR.System.GetFloatTrackedDeviceProperty(device, property, ref propertyError);
            if (propertyError != ETrackedPropertyError.TrackedProp_Success) {
                throw new Exception("Failed to obtain tracked device property \"" +
                    property + "\", error: (" + (int)propertyError + ") " + propertyError.ToString());
            }
            return value;
        }

        public static int[] Int32MaskToArray(int mask) {
            List<int> maskBits = new List<int>(32);
            for (int i = 0; i < 32; i++) {
                int checkMask = 1 << i;
                if ((mask & checkMask) > 0) {
                    maskBits.Add(i);
                }
            }
            return maskBits.ToArray();
        }

        public static void SetLayer(GameObject obj, int layer) {
            if (obj != null) {
                obj.layer = layer;
                int numChildren = obj.transform.childCount;
                for (int i = 0; i < numChildren; i++) {
                    SetLayer(obj.transform.GetChild(i).gameObject, layer);
                }
            }
        }

        public static void SetGameObjectTreeActive(GameObject obj, bool active) {
            if (obj != null) {
                obj.SetActive(active);
                int numChildren = obj.transform.childCount;
                for (int i = 0; i < numChildren; i++) {
                    SetGameObjectTreeActive(obj.transform.GetChild(i).gameObject, active);
                }
            }
        }

        public static void SetGameObjectChildrenActive(GameObject obj, bool active) {
            if (obj != null) {
                int numChildren = obj.transform.childCount;
                for (int i = 0; i < numChildren; i++) {
                    obj.transform.GetChild(i).gameObject.SetActive(active);
                }
            }
        }

#if DEBUG
        public static GameObject CreateGizmo() {
            GameObject gizmo = new GameObject("gizmo");
            gizmo.transform.localScale = Vector3.one;

            GameObject gizmoX = GameObject.CreatePrimitive(PrimitiveType.Cube);
            UnityEngine.Object.Destroy(gizmoX.GetComponent<BoxCollider>());
            gizmoX.transform.SetParent(gizmo.transform);
            gizmoX.transform.localScale = new Vector3(.1f, .01f, .01f);
            gizmoX.transform.localPosition = new Vector3(.05f, 0f, 0f);
            gizmoX.GetComponent<MeshRenderer>().material.color = Color.red;
            gizmoX.layer = 20;

            GameObject gizmoY = GameObject.CreatePrimitive(PrimitiveType.Cube);
            UnityEngine.Object.Destroy(gizmoY.GetComponent<BoxCollider>());
            gizmoY.transform.SetParent(gizmo.transform);
            gizmoY.transform.localScale = new Vector3(.01f, .1f, .01f);
            gizmoY.transform.localPosition = new Vector3(.0f, .05f, 0f);
            gizmoY.GetComponent<MeshRenderer>().material.color = Color.green;
            gizmoY.layer = 20;

            GameObject gizmoZ = GameObject.CreatePrimitive(PrimitiveType.Cube);
            UnityEngine.Object.Destroy(gizmoZ.GetComponent<BoxCollider>());
            gizmoZ.transform.SetParent(gizmo.transform);
            gizmoZ.transform.localScale = new Vector3(.01f, .01f, .1f);
            gizmoZ.transform.localPosition = new Vector3(.0f, 0f, .05f);
            gizmoZ.GetComponent<MeshRenderer>().material.color = Color.blue;
            gizmoZ.layer = 20;

            GameObject gizmoPivot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            UnityEngine.Object.Destroy(gizmoPivot.GetComponent<SphereCollider>());
            gizmoPivot.transform.SetParent(gizmo.transform);
            gizmoPivot.transform.localScale = new Vector3(.02f, .02f, .02f);
            gizmoPivot.transform.localPosition = Vector3.zero;
            gizmoPivot.GetComponent<MeshRenderer>().material.color = Color.gray;
            gizmoPivot.layer = 20;

            return gizmo;
        }

        public static GameObject CreateGizmoAtPosition(Transform location) {
            return CreateGizmoAtPosition(location.position, location.rotation);
        }

        public static GameObject CreateGizmoAtPosition(Vector3 position) {
            return CreateGizmoAtPosition(position, Quaternion.identity);
        }

        public static GameObject CreateGizmoAtPosition(Vector3 position, Quaternion rotation) {
            GameObject gizmo = CreateGizmo();
            gizmo.transform.position = position;
            gizmo.transform.rotation = rotation;
            return gizmo;
        }

        public static GameObject CreateGizmoBox(Vector3 position, Vector3 size) {
            GameObject gizmo = new GameObject("gizmo");

            GameObject gizmoPivot0 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            UnityEngine.Object.Destroy(gizmoPivot0.GetComponent<SphereCollider>());
            gizmoPivot0.transform.SetParent(gizmo.transform);
            gizmoPivot0.transform.localScale = Vector3.one * .05f;
            gizmoPivot0.transform.localPosition = position + size;
            gizmoPivot0.GetComponent<MeshRenderer>().material.color = Color.gray;
            gizmoPivot0.layer = 20;

            GameObject gizmoPivot1 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            UnityEngine.Object.Destroy(gizmoPivot0.GetComponent<SphereCollider>());
            gizmoPivot0.transform.SetParent(gizmo.transform);
            gizmoPivot0.transform.localScale = Vector3.one * .005f;
            gizmoPivot0.transform.localPosition = position - size;
            gizmoPivot0.GetComponent<MeshRenderer>().material.color = Color.gray;
            gizmoPivot0.layer = 20;

            return gizmo;
        }

        public static void PrintAllCameras() {
            Utils.Log("Scene: " + HighLogic.LoadedScene);
            Utils.Log("CameraMode: " + CameraManager.Instance.currentCameraMode);
            for (int i = 0; i < Camera.allCamerasCount; i++) {
                Camera cam = Camera.allCameras[i];
                Utils.Log("Camera: " + cam.name);
                Utils.Log("* clearFlags: " + cam.clearFlags);
                Utils.Log("* backgroundColor: " + cam.backgroundColor);

                string maskString = "[";
                int[] cullingMaskLayers = Int32MaskToArray(Camera.allCameras[i].cullingMask);
                string[] cullingMaskLayersStr = new string[cullingMaskLayers.Length];
                for (int j = 0; j < cullingMaskLayers.Length; j++) {
                    cullingMaskLayersStr[j] = cullingMaskLayers[j].ToString();
                }
                maskString += String.Join(",", cullingMaskLayersStr);
                maskString += "]";

                Utils.Log("* cullingMask: " + maskString);
                Utils.Log("* orthographic? " + cam.orthographic);
                Utils.Log("* fieldOfView: " + cam.fieldOfView.ToString("F3"));
                Utils.Log("* nearClipPlane: " + cam.nearClipPlane.ToString("F3"));
                Utils.Log("* farClipPlane: " + cam.farClipPlane.ToString("F3"));
                Utils.Log("* rect: " + cam.rect.ToString("F3"));
                Utils.Log("* depth: " + cam.depth.ToString("F1"));
                Utils.Log("* renderingPath: " + cam.renderingPath);
                Utils.Log("* useOcclusionCulling? " + cam.useOcclusionCulling);
                Utils.Log("* allowHDR? " + cam.allowHDR);
                Utils.Log("* allowMSAA? " + cam.allowMSAA);
                Utils.Log("* depthTextureMode: " + cam.depthTextureMode);
            }
        }

        public static void PrintGameObject(GameObject go) {
            Log("GameObject (" + (go.activeInHierarchy ? "on" : "off") + "): " +
                go.name + " (layer: " + go.layer + ")");
            Component[] components = go.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++) {
                Log("Component: " + components[i].ToString());

                if (components[i] is MeshFilter) {
                    MeshFilter meshFilter = components[i] as MeshFilter;
                    Log("MeshFilter: " + meshFilter.sharedMesh.name +
                        " (" + meshFilter.sharedMesh.vertexCount + " vertices)");
                }

                if (components[i] is MeshRenderer) {
                    MeshRenderer meshRenderer = components[i] as MeshRenderer;
                    Log("MeshRenderer (" + (meshRenderer.enabled ? "on" : "off") + "): " +
                        meshRenderer.sharedMaterial.name +
                        ", shader \"" + meshRenderer.sharedMaterial.shader.name + "\", " +
                        "color " + meshRenderer.sharedMaterial.color);
                }
            }
        }

        public static void PrintGameObjectTree(GameObject go) {
            PrintGameObject(go);
            for (int i = 0; i < go.transform.childCount; i++) {
                Log(go.name + " child " + i);
                PrintGameObjectTree(go.transform.GetChild(i).gameObject);
            }
        }

        public static void PrintDebug() {
            GameObject[] allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();

            foreach (GameObject GO in allObjects) {
                if (GO.activeInHierarchy) {
                    Collider GO_coll = GO.GetComponent<Collider>();

                    if (GO_coll != null) {
                        Utils.Log("GO.name = " + GO.name);
                        Utils.Log("GO.layer = " + GO.layer);
                        Utils.Log("GO.collider = " + GO_coll);
                    }
                }
                
            }
        }

        public static void PrintAllLayers() {
            for (int i = 0; i < 32; i++) {
                Utils.Log("Layer " + i + ": " + LayerMask.LayerToName(i));
            }
        }

        public static void PrintFonts() {
            TMPro.TMP_FontAsset[] fonts = Resources.FindObjectsOfTypeAll(typeof(TMPro.TMP_FontAsset)) as TMPro.TMP_FontAsset[];
            Utils.Log("num fonts: " + fonts.Length);
            for (int i = 0; i < fonts.Length; i++) {
                TMPro.TMP_FontAsset font = fonts[i];
                Utils.Log("font name: " + font.name);
            }
        }

        public static void PrintCollisionMatrix() {
            string header = string.Format("{0,22} {1,3}", "", "");
            for (int i = 0; i < 32; i++) {
                header += string.Format("{0,3}", i);
            }
            Utils.Log(header);

            for (int y = 0; y < 32; y++) {
                string line = string.Format("{0,22} {1,3}", LayerMask.LayerToName(y), y);
                for (int x = 0; x < 32; x++) {
                    line += Physics.GetIgnoreLayerCollision(x, y) ? "   " : "  X";
                }
                Utils.Log(line);
            }
        }
#endif

    } // class Utils
} // namespace KerbalVR
