using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace KerbalVR
{
    class Utils
    {
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
            Debug.Log("[KerbalVR] L:: " + obj);
        }

        public static void LogWarning(object obj) {
            Debug.LogWarning("[KerbalVR] W@@ " + obj);
        }

        public static void LogError(object obj) {
            Debug.LogError("[KerbalVR] E!! " + obj);
        }

        public static bool Is64BitProcess {
            get { return (IntPtr.Size == 8); }
        }
    }
}
