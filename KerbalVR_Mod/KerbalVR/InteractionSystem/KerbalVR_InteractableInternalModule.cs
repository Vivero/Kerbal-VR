using UnityEngine;
using Valve.VR;

namespace KerbalVR {
    public class InteractableInternalModule : InternalModule {
        /// <summary>
        /// The skeleton poses that will be applied when interacting with this object.
        /// </summary>
        public SteamVR_Skeleton_Poser SkeletonPoser { get; protected set; }

        /// <summary>
        /// If true, this object is currently being grabbed by a hand.
        /// False, otherwise.
        /// </summary>
        public bool IsGrabbed { get; set; } = false;

        /// <summary>
        /// The Hand object that is grabbing this object. Null, if no
        /// hand is grabbing this object.
        /// </summary>
        public Hand GrabbedHand { get; set; }
    }
}
