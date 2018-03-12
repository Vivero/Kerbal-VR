using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace KerbalVR
{
    public class EventManager : MonoBehaviour
    {
        // store all the events available
        private Dictionary<string, UnityEvent> eventDict;

        // this is a singleton class, and there must be one EventManager in the scene
        private static EventManager _instance;
        public static EventManager Instance {
            get {
                if (_instance == null) {
                    _instance = FindObjectOfType<EventManager>();
                    if (_instance == null) {
                        Utils.LogError("The scene needs to have one active GameObject with an EventManager script attached!");
                    } else {
                        _instance.Initialize();
                    }
                }
                return _instance;
            }
        }

        // first-time initialization for this singleton class
        private void Initialize() {
            if (eventDict == null) {
                eventDict = new Dictionary<string, UnityEvent>();
            }
        }


        public static void StartListening(string eventName, UnityAction listener) {
            UnityEvent thisEvent = null;
            if (Instance.eventDict.TryGetValue(eventName, out thisEvent)) {
                // add listener to existing event
                thisEvent.AddListener(listener);
            } else {
                // first time this event is listened to
                thisEvent = new UnityEvent();
                thisEvent.AddListener(listener);
                Instance.eventDict.Add(eventName, thisEvent);
            }
        }

        public static void StopListening(string eventName, UnityAction listener) {
            if (_instance == null) return;
            UnityEvent thisEvent = null;
            if (Instance.eventDict.TryGetValue(eventName, out thisEvent)) {
                thisEvent.RemoveListener(listener);
            }
        }

        public static void TriggerEvent(string eventName) {
            UnityEvent thisEvent = null;
            if (Instance.eventDict.TryGetValue(eventName, out thisEvent)) {
                thisEvent.Invoke();
            }
        }
    }
}
