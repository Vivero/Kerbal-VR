using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(UnhideScript))]
public class Unhide : Editor {

    public override void OnInspectorGUI() {
        base.OnInspectorGUI();

        UnhideScript obj = (UnhideScript)target;

        if (GUILayout.Button("Unhide")) {
            obj.UnhideObjects();
        }
    }
}
