using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Valve.VR;

namespace KerbalVR
{
    class Utils
    {
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
