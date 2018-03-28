using UnityEngine.Events;

namespace KerbalVR
{
    /// <summary>
    /// An event system for KerbalVR interactive components.
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
    }
}
