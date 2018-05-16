using System.Collections.Generic;
using UnityEngine.Events;

namespace KerbalVR
{
    /// <summary>
    /// An event system for KerbalVR interactive components. Note this class is largely copied
	/// from SteamVR_Events class of the SteamVR Unity plugin.
    /// 
    /// Example usage:
    ///     void ManipulatorLeftUpdated(Device state) { ... }
    ///     KerbalVR.Events.ManipulatorLeftUpdated.Listen(OnManipulatorLeftUpdated); // Usually in OnEnable
    ///     KerbalVR.Events.ManipulatorLeftUpdated.Remove(OnManipulatorLeftUpdated); // Usually in OnDisable
    ///     
    /// Alternatively, if Listening/Removing often these can be cached as follows:
    ///     KerbalVR.Events.Action manipulatorUpdatedAction;
    ///     void OnAwake() { manipulatorUpdatedAction = KerbalVR.Events.ManipulatorLeftUpdatedAction(OnManipulatorLeftUpdated); }
    ///     void OnEnable() { manipulatorUpdatedAction.enabled = true; }
    ///     void OnDisable() { manipulatorUpdatedAction.enabled = false; }
    ///     
    /// TODO: investigate if we should just merge this code with the SteamVR_Events.
    /// </summary>
    public static class Events
    {
        public abstract class Action
        {
            public abstract void Enable(bool enabled);
            public bool enabled { set { Enable(value); } }
        }

        [System.Serializable]
        public class ActionNoArgs : Action
        {
            public ActionNoArgs(Event _event, UnityAction action) {
                this._event = _event;
                this.action = action;

            }

            public override void Enable(bool enabled) {
                if (enabled)
                    _event.Listen(action);
                else
                    _event.Remove(action);
            }

            Event _event;
            UnityAction action;
        }

        [System.Serializable]
        public class Action<T> : Action
        {
            public Action(Event<T> _event, UnityAction<T> action) {
                this._event = _event;
                this.action = action;
            }

            public override void Enable(bool enabled) {
                if (enabled)
                    _event.Listen(action);
                else
                    _event.Remove(action);
            }

            Event<T> _event;
            UnityAction<T> action;
        }

        [System.Serializable]
        public class Action<T0, T1> : Action
        {
            public Action(Event<T0, T1> _event, UnityAction<T0, T1> action) {
                this._event = _event;
                this.action = action;
            }

            public override void Enable(bool enabled) {
                if (enabled)
                    _event.Listen(action);
                else
                    _event.Remove(action);
            }

            Event<T0, T1> _event;
            UnityAction<T0, T1> action;
        }

        [System.Serializable]
        public class Action<T0, T1, T2> : Action
        {
            public Action(Event<T0, T1, T2> _event, UnityAction<T0, T1, T2> action) {
                this._event = _event;
                this.action = action;
            }

            public override void Enable(bool enabled) {
                if (enabled)
                    _event.Listen(action);
                else
                    _event.Remove(action);
            }

            Event<T0, T1, T2> _event;
            UnityAction<T0, T1, T2> action;
        }

        public class Event : UnityEvent
        {
            public void Listen(UnityAction action) { this.AddListener(action); }
            public void Remove(UnityAction action) { this.RemoveListener(action); }
            public void Send() { this.Invoke(); }
        }

        public class Event<T> : UnityEvent<T>
        {
            public void Listen(UnityAction<T> action) { this.AddListener(action); }
            public void Remove(UnityAction<T> action) { this.RemoveListener(action); }
            public void Send(T arg0) { this.Invoke(arg0); }
        }

        public class Event<T0, T1> : UnityEvent<T0, T1>
        {
            public void Listen(UnityAction<T0, T1> action) { this.AddListener(action); }
            public void Remove(UnityAction<T0, T1> action) { this.RemoveListener(action); }
            public void Send(T0 arg0, T1 arg1) { this.Invoke(arg0, arg1); }
        }

        public class Event<T0, T1, T2> : UnityEvent<T0, T1, T2>
        {
            public void Listen(UnityAction<T0, T1, T2> action) { this.AddListener(action); }
            public void Remove(UnityAction<T0, T1, T2> action) { this.RemoveListener(action); }
            public void Send(T0 arg0, T1 arg1, T2 arg2) { this.Invoke(arg0, arg1, arg2); }
        }

        public static Event<SteamVR_Controller.Device> ManipulatorLeftUpdated = new Event<SteamVR_Controller.Device>();
        public static Action ManipulatorLeftUpdatedAction(UnityAction<SteamVR_Controller.Device> action) { return new Action<SteamVR_Controller.Device>(ManipulatorLeftUpdated, action); }

        public static Event<SteamVR_Controller.Device> ManipulatorRightUpdated = new Event<SteamVR_Controller.Device>();
        public static Action ManipulatorRightUpdatedAction(UnityAction<SteamVR_Controller.Device> action) { return new Action<SteamVR_Controller.Device>(ManipulatorRightUpdated, action); }
        
        static Dictionary<string, Event> avionicsSignals = new Dictionary<string, Event>();
        public static Event Avionics(string signalName) {
            Event e;
            if (!avionicsSignals.TryGetValue(signalName, out e)) {
                e = new Event();
                avionicsSignals.Add(signalName, e);
            }
            return e;
        }

        public static ActionNoArgs AvionicsAction(string signalName, UnityAction action) {
            return new ActionNoArgs(Avionics(signalName), action);
        }

        static Dictionary<string, Event<float>> avionicsSignalsFloat = new Dictionary<string, Event<float>>();
        public static Event<float> AvionicsFloat(string signalName) {
            Event<float> e;
            if (!avionicsSignalsFloat.TryGetValue(signalName, out e)) {
                e = new Event<float>();
                avionicsSignalsFloat.Add(signalName, e);
            }
            return e;
        }

        public static Action AvionicsFloatAction(string signalName, UnityAction<float> action) {
            return new Action<float>(AvionicsFloat(signalName), action);
        }

        static Dictionary<string, Event<int>> avionicsSignalsInt = new Dictionary<string, Event<int>>();
        public static Event<int> AvionicsInt(string signalName) {
            Event<int> e;
            if (!avionicsSignalsInt.TryGetValue(signalName, out e)) {
                e = new Event<int>();
                avionicsSignalsInt.Add(signalName, e);
            }
            return e;
        }

        public static Action AvionicsIntAction(string signalName, UnityAction<int> action) {
            return new Action<int>(AvionicsInt(signalName), action);
        }

    } // class Events
} // namespace KerbalVR
