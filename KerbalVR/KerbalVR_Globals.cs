extern alias TMPVendor;

using System.IO;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;

namespace KerbalVR
{
    /// <summary>
    /// A class to contain globally accessible constants.
    /// </summary>
    public class Globals : MonoBehaviour
    {

        #region Singleton
        // this is a singleton class, and there must be one EventManager in the scene
        private static Globals _instance;
        public static Globals Instance {
            get {
                if (_instance == null) {
                    _instance = FindObjectOfType<Globals>();
                    if (_instance == null) {
                        Utils.LogError("The scene needs to have one active GameObject with a DeviceManager script attached!");
                    } else {
                        _instance.Initialize();
                    }
                }
                return _instance;
            }
        }

        // first-time initialization for this singleton class
        private void Initialize() {
            fonts = new Dictionary<string, TMPVendor::TMPro.TMP_FontAsset>();
        }
        #endregion

        // plugin name
        public static readonly string KERBALVR_NAME = "KerbalVR";

        // path to the KerbalVR Assets directory
        public static readonly string KERBALVR_ASSETS_DIR = "KerbalVR/Assets/";

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

        // store the fonts loaded from asset bundles
        private Dictionary<string, TMPVendor::TMPro.TMP_FontAsset> fonts;

        public void AddFont(string fontName, TMPVendor::TMPro.TMP_FontAsset font) {
            fonts[fontName] = font;
        }

        public TMPro.TMP_FontAsset GetFont(string fontName) {
            if (fonts == null) {
                Utils.LogWarning("The fucking fonts dict is null!");
                return null;
            }

            Utils.Log("WTF IS GOING ON 1");

            TMPVendor::TMPro.TMP_FontAsset vendorFont = null;
            if (!fonts.TryGetValue(fontName, out vendorFont)) {
                Utils.LogWarning("Font \"" + fontName + "\" is not loaded!");
                return null;
            }
            
            Utils.Log("WTF IS GOING ON 2 " + vendorFont.ToString());

            TMPro.TMP_Glyph[] kspGlyphInfo = GetGlyphs(vendorFont);

            TMPVendor::TMPro.FaceInfo vendorFaceInfo = vendorFont.fontInfo;
            TMPro.FaceInfo kspFaceInfo = new TMPro.FaceInfo();

            if (vendorFaceInfo != null) {
                kspFaceInfo.Name = vendorFaceInfo.Name;
                kspFaceInfo.PointSize = vendorFaceInfo.PointSize;
                kspFaceInfo.LineHeight = vendorFaceInfo.LineHeight;
                kspFaceInfo.Baseline = vendorFaceInfo.Baseline;
                kspFaceInfo.Ascender = vendorFaceInfo.Ascender;
                kspFaceInfo.Descender = vendorFaceInfo.Descender;
                kspFaceInfo.CenterLine = vendorFaceInfo.CenterLine;
                kspFaceInfo.Underline = vendorFaceInfo.Underline;
                kspFaceInfo.UnderlineThickness = vendorFaceInfo.UnderlineThickness;
                kspFaceInfo.SuperscriptOffset = vendorFaceInfo.SuperscriptOffset;
                kspFaceInfo.SubscriptOffset = vendorFaceInfo.SubscriptOffset;
                kspFaceInfo.SubSize = vendorFaceInfo.SubSize;
                kspFaceInfo.AtlasWidth = vendorFaceInfo.AtlasWidth;
                kspFaceInfo.AtlasHeight = vendorFaceInfo.AtlasHeight;
                kspFaceInfo.strikethrough = vendorFaceInfo.strikethrough;

                /*kspFaceInfo.strikethroughThickness = vendorFaceInfo.strikethroughThickness;
                kspFaceInfo.CapHeight = vendorFaceInfo.CapHeight;
                kspFaceInfo.CharacterCount = vendorFaceInfo.CharacterCount;
                kspFaceInfo.Padding = vendorFaceInfo.Padding;
                kspFaceInfo.Scale = vendorFaceInfo.Scale;
                kspFaceInfo.TabWidth = vendorFaceInfo.TabWidth;*/
            } else {
                Utils.LogWarning("vendorFont.fontInfo is null!");
            }
            
            Utils.Log("WTF IS GOING ON 3");

            TMPro.TMP_FontAsset kspFont = ScriptableObject.CreateInstance<TMPro.TMP_FontAsset>();
            kspFont.fontAssetType = TMPro.TMP_FontAsset.FontAssetTypes.SDF;
            kspFont.AddFaceInfo(kspFaceInfo);

            if (kspGlyphInfo != null) kspFont.AddGlyphInfo(kspGlyphInfo);
            else Utils.LogWarning("kspGlyphInfo is null!");

            kspFont.atlas = vendorFont.atlas;
            kspFont.material = vendorFont.material;

            Utils.Log("WTF IS GOING ON 4");

            return kspFont;
        }

        /*private static TMPro.KerningPair ConvertKerningPair(TMPVendor::TMPro.KerningPair pair) {
            return new TMPro.KerningPair((int)pair.firstGlyph, (int)pair.secondGlyph, pair.xOffset);
        }*/

        private static TMPro.KerningTable ConvertKerningTable(TMPVendor::TMPro.KerningTable table) {
            TMPro.KerningTable newTable = new TMPro.KerningTable();
            int count = table.kerningPairs.Count;
            for (int i = 0; i < count; i++) {
                newTable.AddKerningPair((int)table.kerningPairs[i].firstGlyph, (int)table.kerningPairs[i].secondGlyph, table.kerningPairs[i].xOffset);
            }
            return newTable;
        }

        private static TMPro.TMP_Glyph[] GetGlyphs(TMPVendor::TMPro.TMP_FontAsset font) {
            TMPro.TMP_Glyph[] glyphs = null;

            if (font.characterDictionary != null) {
                int numGlyphs = font.characterDictionary.Count;
                glyphs = new TMPro.TMP_Glyph[numGlyphs];

                int glyphIdx = 0;
                foreach (KeyValuePair<int, TMPVendor::TMPro.TMP_Glyph> entry in font.characterDictionary) {
                    TMPVendor::TMPro.TMP_Glyph glyph = entry.Value; // vendor's glyph
                    TMPro.TMP_Glyph kspGlyph = new TMPro.TMP_Glyph(); // glyph for KSP font

                    // populate KSP glyph
                    kspGlyph.id = glyph.id;
                    kspGlyph.x = glyph.x;
                    kspGlyph.y = glyph.y;
                    kspGlyph.width = glyph.width;
                    kspGlyph.height = glyph.height;
                    kspGlyph.xOffset = glyph.xOffset;
                    kspGlyph.yOffset = glyph.yOffset;
                    kspGlyph.xAdvance = glyph.xAdvance;
                    kspGlyph.scale = glyph.scale;

                    glyphs[glyphIdx++] = kspGlyph;
                }
            } else {
                Utils.LogWarning("font.characterDictionary is null!");
            }
            
            return glyphs;
        }
    }
}
