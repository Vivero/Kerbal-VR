using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnhideScript : MonoBehaviour
{
    public void UnhideObjects() {
        UnhideGameObject(gameObject);
    }

    public void UnhideGameObject(GameObject obj) {
        obj.hideFlags = HideFlags.None;
        int numChildren = obj.transform.childCount;
        if (numChildren > 0) {
            for (int i = 0; i < numChildren; i++) {
                UnhideGameObject(obj.transform.GetChild(i).gameObject);
            }
        }
        
    }
}
