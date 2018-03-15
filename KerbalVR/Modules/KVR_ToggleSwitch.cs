using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KerbalVR.Modules
{
    public class KVR_ToggleSwitch : InternalModule
    {
        void OnStart() {
            Utils.LogInfo("InternalModule Start " + gameObject.name);
        }
    }
}
