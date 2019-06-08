using System.IO;
using System.Reflection;

namespace KerbalVR
{
    /// <summary>
    /// A class to contain globally accessible constants.
    /// </summary>
    public class Globals
    {
        // plugin name
        public static readonly string KERBALVR_NAME = "KerbalVR";

        // path to the KerbalVR Assets directory
        public static readonly string KERBALVR_ASSETS_DIR = Path.Combine(KERBALVR_NAME, "Assets");

        // path to asset bundles
        public static readonly string KERBALVR_ASSETBUNDLES_DIR = Path.Combine(KERBALVR_ASSETS_DIR, "AssetBundles");

        // path to textures
        public static readonly string KERBALVR_TEXTURES_DIR = Path.Combine(KERBALVR_ASSETS_DIR, "Textures");

        // define location of OpenVR library
        public static string OpenVRDllPath {
            get {
                string currentPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string openVrPath = Path.Combine(currentPath, "openvr");
                return Path.Combine(openVrPath, Utils.Is64BitProcess ? "win64" : "win32");
            }
        }

        // a prefix to append to every KerbalVR debug log message
        public static readonly string LOG_PREFIX = "[" + KERBALVR_NAME + "] ";
    }
}
