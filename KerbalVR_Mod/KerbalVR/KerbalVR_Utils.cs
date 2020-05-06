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
        private static AppDebugGUI debugUi = null;

        public static T GetOrAddComponent<T>(GameObject obj) where T : Component {
            T c = obj.GetComponent<T>();
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

        public static void PostScreenMessage(object obj) {
            ScreenMessages.PostScreenMessage(Globals.LOG_PREFIX + obj);
        }

        public static void SetDebugText(object obj) {
            if (debugUi == null) {
                debugUi = GameObject.FindObjectOfType<AppDebugGUI>();
            }
            if (debugUi != null) {
                debugUi.SetText(obj);
            }
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

        public static GameObject CreateGizmo() {
            GameObject gizmo = new GameObject("gizmo");
            gizmo.transform.localScale = Vector3.one;

            GameObject gizmoX = GameObject.CreatePrimitive(PrimitiveType.Cube);
            UnityEngine.Object.Destroy(gizmoX.GetComponent<BoxCollider>());
            gizmoX.transform.SetParent(gizmo.transform);
            gizmoX.transform.localScale = new Vector3(.1f, .01f, .01f);
            gizmoX.transform.localPosition = new Vector3(.05f, 0f, 0f);
            gizmoX.GetComponent<MeshRenderer>().material.color = Color.red;
            gizmoX.layer = 0;

            GameObject gizmoY = GameObject.CreatePrimitive(PrimitiveType.Cube);
            UnityEngine.Object.Destroy(gizmoY.GetComponent<BoxCollider>());
            gizmoY.transform.SetParent(gizmo.transform);
            gizmoY.transform.localScale = new Vector3(.01f, .1f, .01f);
            gizmoY.transform.localPosition = new Vector3(0f, .05f, 0f);
            gizmoY.GetComponent<MeshRenderer>().material.color = Color.green;
            gizmoY.layer = 0;

            GameObject gizmoZ = GameObject.CreatePrimitive(PrimitiveType.Cube);
            UnityEngine.Object.Destroy(gizmoZ.GetComponent<BoxCollider>());
            gizmoZ.transform.SetParent(gizmo.transform);
            gizmoZ.transform.localScale = new Vector3(.01f, .01f, .1f);
            gizmoZ.transform.localPosition = new Vector3(0f, 0f, .05f);
            gizmoZ.GetComponent<MeshRenderer>().material.color = Color.blue;
            gizmoZ.layer = 0;

            GameObject gizmoPivot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            UnityEngine.Object.Destroy(gizmoPivot.GetComponent<SphereCollider>());
            gizmoPivot.transform.SetParent(gizmo.transform);
            gizmoPivot.transform.localScale = new Vector3(.02f, .02f, .02f);
            gizmoPivot.transform.localPosition = Vector3.zero;
            gizmoPivot.GetComponent<MeshRenderer>().material.color = Color.gray;
            gizmoPivot.layer = 0;

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

        public static GameObject CreateArrow() {
            GameObject gizmo = new GameObject("gizmo_arrow");
            gizmo.transform.localScale = Vector3.one;

            GameObject gizmoZ = GameObject.CreatePrimitive(PrimitiveType.Cube);
            UnityEngine.Object.Destroy(gizmoZ.GetComponent<BoxCollider>());
            gizmoZ.transform.SetParent(gizmo.transform);
            gizmoZ.transform.localScale = new Vector3(.02f, .02f, .1f);
            gizmoZ.transform.localPosition = new Vector3(0f, 0f, .05f);
            gizmoZ.GetComponent<MeshRenderer>().material.color = Color.blue;
            gizmoZ.layer = 0;

            GameObject gizmoPivot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            UnityEngine.Object.Destroy(gizmoPivot.GetComponent<SphereCollider>());
            gizmoPivot.transform.SetParent(gizmo.transform);
            gizmoPivot.transform.localScale = new Vector3(.03f, .03f, .03f);
            gizmoPivot.transform.localPosition = Vector3.zero;
            gizmoPivot.GetComponent<MeshRenderer>().material.color = Color.gray;
            gizmoPivot.layer = 0;

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

        public static string GetGameObjectInfo(GameObject go, int level = 0) {
            string indentation = "";
            for (int i = 0; i < level; ++i) {
                indentation += "    ";
            }

            string msg = indentation + "GameObject (" + (go.activeInHierarchy ? "on" : "off") + "): " + go.name + " (layer: " + go.layer + ")";
            if (go.transform.parent != null) {
                msg += " (Parent: " + go.transform.parent.name + ")";
            }
            msg += "\n";

            msg += indentation + "localPosition : " + go.transform.localPosition.ToString() + "\n";
            msg += indentation + "localRotation : " + go.transform.localRotation.ToString() + "\n";
            msg += indentation + "localScale    : " + go.transform.localScale.ToString() + "\n";

            Component[] components = go.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++) {
                msg += indentation + "Component: " + components[i].GetType().ToString();

                if (components[i] is MeshFilter) {
                    MeshFilter meshFilter = components[i] as MeshFilter;
                    if (meshFilter.sharedMesh != null) {
                        msg += "\n" + indentation + " '" + meshFilter.sharedMesh.name + "' (" + meshFilter.sharedMesh.vertexCount + " vertices)";
                    }
                }

                if (components[i] is MeshRenderer) {
                    MeshRenderer meshRenderer = components[i] as MeshRenderer;
                    msg += " (" + (meshRenderer.enabled ? "on" : "off") + ")";
                    if (meshRenderer.sharedMaterial != null) {
                        msg += "\n" + indentation + " sharedMaterial.name: " + meshRenderer.sharedMaterial.name;
                        // msg += " (color=" + meshRenderer.sharedMaterial.color + ")";
                    }
                }
                msg += "\n";
            }
            return msg;
        }

        public static string GetCameraInfo(Camera cam) {
            string msg = "'" + cam.name + "', tag: '" + cam.gameObject.tag + "'\n";
            msg += "* position: " + cam.transform.position + "\n";
            msg += "* rotation: " + cam.transform.rotation + "\n";
            msg += "* clearFlags: " + cam.clearFlags + "\n";
            msg += "* backgroundColor: " + cam.backgroundColor + "\n";
            msg += "* cullingMask: [" + String.Join(",", Int32MaskToArray(cam.cullingMask)) + "]\n";
            msg += "* orthographic? " + cam.orthographic + "\n";
            msg += "* fieldOfView: " + cam.fieldOfView.ToString("F3") + "\n";
            msg += "* nearClipPlane: " + cam.nearClipPlane.ToString("F3") + "\n";
            msg += "* farClipPlane: " + cam.farClipPlane.ToString("F3") + "\n";
            msg += "* rect: " + cam.rect.ToString("F3") + "\n";
            msg += "* depth: " + cam.depth.ToString("F1") + "\n";
            msg += "* renderingPath: " + cam.renderingPath + "\n";
            msg += "* useOcclusionCulling? " + cam.useOcclusionCulling + "\n";
            msg += "* allowHDR? " + cam.allowHDR + "\n";
            msg += "* allowMSAA? " + cam.allowMSAA + "\n";
            msg += "* depthTextureMode: " + cam.depthTextureMode + "\n";
            return msg;
        }

        public static string GetGameObjectTree(GameObject go, int level = 0) {
            string logMsg = GetGameObjectInfo(go, level);
            for (int i = 0; i < go.transform.childCount; i++) {
                // logMsg += "+-- (" + (i + 1) + "/" + go.transform.childCount + ") Parent: " + go.name + "\n";
                logMsg += GetGameObjectTree(go.transform.GetChild(i).gameObject, level + 1);
            }
            return logMsg;
        }

        public static void PrintAllCameras() {
            string logMsg = "\nScene: " + HighLogic.LoadedScene;
            logMsg += ", CameraMode: " + (CameraManager.Instance != null ? CameraManager.Instance.currentCameraMode.ToString() : "null") + "\n\n";
            for (int i = 0; i < Camera.allCamerasCount; i++) {
                logMsg += "Camera (" + (i + 1) + "/" + Camera.allCamerasCount + ") ";
                logMsg += GetCameraInfo(Camera.allCameras[i]);
                logMsg += "-- Object hierarchy --\n";
                logMsg += GetGameObjectTree(Camera.allCameras[i].gameObject) + "\n\n";
            }
            Log(logMsg);
        }

        public static void PrintGameObject(GameObject go) {
            Log(GetGameObjectInfo(go));
        }

        public static void PrintGameObjectTree(GameObject go) {
            Log(GetGameObjectTree(go));
        }

        public static void PrintAllGameObjects() {
            GameObject[] allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
            foreach (GameObject go in allObjects) {
                if (go.activeInHierarchy) {
                    // string logMsg = GetGameObjectInfo(go) + "\n";
                    string logMsg = GetGameObjectTree(go);
                    Log(logMsg);
                }
            }
        }

        public static void PrintAllLayers() {
            for (int i = 0; i < 32; i++) {
                Log("Layer " + i + ": " + LayerMask.LayerToName(i));
            }
        }

        public static void PrintFonts() {
            TMPro.TMP_FontAsset[] fonts = Resources.FindObjectsOfTypeAll(typeof(TMPro.TMP_FontAsset)) as TMPro.TMP_FontAsset[];
            Log("num fonts: " + fonts.Length);
            for (int i = 0; i < fonts.Length; i++) {
                TMPro.TMP_FontAsset font = fonts[i];
                Log("font name: " + font.name);
            }
        }

        public static void PrintCollisionMatrix() {
            string header = string.Format("{0,22} {1,3}", "", "");
            for (int i = 0; i < 32; i++) {
                header += string.Format("{0,3}", i);
            }
            Log(header);

            for (int y = 0; y < 32; y++) {
                string line = string.Format("{0,22} {1,3}", LayerMask.LayerToName(y), y);
                for (int x = 0; x < 32; x++) {
                    line += Physics.GetIgnoreLayerCollision(x, y) ? "   " : "  X";
                }
                Log(line);
            }
        }

        public static void PrintMainMenuInfo() {
            Log("=== MainMenuEnvLogic ===");

            MainMenuEnvLogic mainMenuLogic = GameObject.FindObjectOfType<MainMenuEnvLogic>();
            if (mainMenuLogic != null) {
                Log("GameObject position: " + mainMenuLogic.gameObject.transform.position);
                Log("landscapeCamera position: " + mainMenuLogic.landscapeCamera.gameObject.transform.position);
                Log("currentStage: " + mainMenuLogic.currentStage);

                Log("areas (" + mainMenuLogic.areas.Length + "):");
                foreach (GameObject area in mainMenuLogic.areas) {
                    PrintGameObject(area);
                }
                Log("startingArea:");
                PrintGameObject(mainMenuLogic.startingArea);

                Log("camPivots (" + mainMenuLogic.camPivots.Length + "):");
                foreach (MainMenuEnvLogic.MenuStage stage in mainMenuLogic.camPivots) {
                    Log("MenuStage initial = " + stage.initialPoint.position + ", target = " + stage.targetPoint.position);
                }
            } else {
                Log("No MainMenuEnvLogic component found.");
            }
        }

        public class DataCollection {
            public double[] DataSamples { get; private set; }
            public int NumSamples { get; private set; }
            public bool IsFull {
                get {
                    return index >= (NumSamples - 1);
                }
            }
            public bool AllowCollection = false;

            private int index = 0;

            public DataCollection(int numSamples) {
                NumSamples = numSamples;
                DataSamples = new double[NumSamples];
            }

            public void AddSample(double sample) {
                if (AllowCollection && index >= 0 && index < NumSamples) {
                    DataSamples[index] = sample;
                    index++;
                }
            }
        }
    } // class Utils
} // namespace KerbalVR
