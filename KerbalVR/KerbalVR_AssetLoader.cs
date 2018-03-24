extern alias TMPVendor;

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KerbalVR
{
    /// <summary>
    /// The AssetLoader plugin should load at the Main Menu,
    /// when all the asset bundles have been loaded.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class AssetLoader : MonoBehaviour
    {
        void Awake() {
            // keep this object around forever
            DontDestroyOnLoad(this);

            // load KerbalVR asset bundles
            LoadAssets();
        }

        private void LoadAssets() {
            
            /*Shader shader = Shader.Find("TextMeshPro/Distance Field");
            if (shader == null) {
                Utils.LogWarning("shader is null!");
            } else {
                Utils.Log("shader: " + shader.name);
            }*/
            
            // get path to asset bundle
            string assetBundlePath = KSPUtil.ApplicationRootPath +
                "GameData/" + Globals.KERBALVR_ASSETS_DIR +
                "kerbalvr-fonts.ksp";
            Utils.Log("path = " + assetBundlePath);

            // load asset bundle
            AssetBundle bundle = AssetBundle.LoadFromFile(assetBundlePath);
            if (bundle == null) {
                Utils.LogError("Error loading asset bundle from: " + assetBundlePath);
                return;
            }

            string[] assetNames = bundle.GetAllAssetNames();
            for (int i = 0; i < assetNames.Length; i++) {
                Utils.Log("Asset: " + assetNames[i]);
                /*if (assetNames[i].EndsWith("sdf.asset")) {
                    TMPVendor::TMPro.TMP_FontAsset font = bundle.LoadAsset<TMPVendor::TMPro.TMP_FontAsset>(assetNames[i]);
                    if (font != null) {
                        Globals.Instance.AddFont(font.name, font);
                        Utils.Log("Loaded font: " + font.name);

                        PrintFont(font);
                        font.ReadFontDefinition();
                        font.SortGlyphs();
                        Utils.Log("");
                        Utils.Log("re-do font");
                        PrintFont(font);

                    } else {
                        Utils.LogWarning("something went wrong");
                    }
                }*/

                /*if (assetNames[i].EndsWith(".ttf")) {
                    Font font = bundle.LoadAsset<Font>(assetNames[i]);
                    if (font != null) {
                        Utils.Log("loaded font: " + font.name);

                        try {
                            TMPVendor::TMPro.TMP_FontAsset tmpFont = BakeTMPFont(font);
                            Globals.Instance.AddFont(font.name, tmpFont);
                            PrintFont(tmpFont);
                        } catch (Exception e) {
                            Utils.LogError("PrintFont error! " + e.ToString());
                        }

                    } else {
                        Utils.LogWarning("font is null!");
                    }
                }*/
            }
        }

        private static void PrintFont(TMPVendor::TMPro.TMP_FontAsset font) {
            Utils.Log("font " + font);
            Utils.Log("fontAssetType " + font.fontAssetType);
            Utils.Log("italicStyle " + font.italicStyle);
            Utils.Log("boldSpacing " + font.boldSpacing);
            Utils.Log("boldStyle " + font.boldStyle);
            Utils.Log("normalSpacingOffset " + font.normalSpacingOffset);
            Utils.Log("tabSize " + font.tabSize);

            Utils.Log("fontWeights " + font.fontWeights + " count: " +
                font.fontWeights.Length);
            Utils.Log("fontCreationSettings " + font.fontCreationSettings);
            Utils.Log("fallbackFontAssets " + font.fallbackFontAssets);
            Utils.Log("atlas " + font.atlas);
            Utils.Log("normalStyle " + font.normalStyle);
            
            Utils.Log("kerningInfo " + font.kerningInfo);
            Utils.Log("kerningDictionary " + font.kerningDictionary);
            Utils.Log("characterDictionary " + font.characterDictionary);
            Utils.Log("fontInfo " + font.fontInfo);

            // Utils.Log("GetCharacters " + TMPVendor::TMPro.TMP_FontAsset.GetCharacters(font));
        }

        private static TMPVendor::TMPro.TMP_FontAsset BakeTMPFont(Font font) {
            TMPVendor::TMPro.TMP_FontAsset fontAsset =
                ScriptableObject.CreateInstance<TMPVendor::TMPro.TMP_FontAsset>();

            int errorCode = TMPVendor::TMPro.EditorUtilities.TMPro_FontPlugin.Initialize_FontEngine();
            if (errorCode != 0 && errorCode != 99) { // 99 means that engine was already initialized
                Debug.LogWarning("Error Code: " + errorCode + "  occurred while initializing TMPro_FontPlugin.");
                return fontAsset;
            }

            string fontPath = KSPUtil.ApplicationRootPath +
                "GameData/" + Globals.KERBALVR_ASSETS_DIR +
                "Product_Sans_Regular.ttf";
            errorCode = TMPVendor::TMPro.EditorUtilities.TMPro_FontPlugin.Load_TrueType_Font(fontPath);
            if (errorCode != 0 && errorCode != 99) { // 99 means that font was already loaded
                Debug.LogWarning("Error Code: " + errorCode + "  occurred while loading font: " + font + ".");
                return fontAsset;
            }

            bool useAutoSizing = true;
            int fontSize = 72;
            if (useAutoSizing) {
                fontSize = 72;
            }
            errorCode = TMPVendor::TMPro.EditorUtilities.TMPro_FontPlugin.FT_Size_Font(fontSize);
            if (errorCode != 0) {
                Debug.LogWarning("Error Code: " + errorCode + "  occurred while sizing font: " + font + " to size: " + fontSize + ".");
                return fontAsset;
            }

            int atlasWidth = 1024, atlasHeight = 1024;
            byte[] textureBuffer = new byte[atlasWidth * atlasHeight];

            string charactersToBake = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
            int[] characterArray = charactersToBake.Select(c => (int)c).ToArray();
            int characterCount = charactersToBake.Length;

            var fontFaceInfo = new TMPVendor::TMPro.EditorUtilities.FT_FaceInfo();
            var fontGlyphInfo = new TMPVendor::TMPro.EditorUtilities.FT_GlyphInfo[characterCount];

            float strokeSize = 2f;
            TMPVendor::TMPro.EditorUtilities.RenderModes fontRenderMode = TMPVendor::TMPro.EditorUtilities.RenderModes.DistanceField16;
            if (fontRenderMode == TMPVendor::TMPro.EditorUtilities.RenderModes.DistanceField16) {
                strokeSize *= 16;
            } else if (fontRenderMode == TMPVendor::TMPro.EditorUtilities.RenderModes.DistanceField32) {
                strokeSize *= 32;
            }

            int characterPadding = 6;
            TMPVendor::TMPro.EditorUtilities.FaceStyles fontStyle = TMPVendor::TMPro.EditorUtilities.FaceStyles.Normal;
            int fontPackingMode = 0;
            errorCode = TMPVendor::TMPro.EditorUtilities.TMPro_FontPlugin.Render_Characters(
                textureBuffer, atlasWidth, atlasHeight, characterPadding, characterArray, 
                characterCount, fontStyle, strokeSize, useAutoSizing, fontRenderMode, 
                fontPackingMode, ref fontFaceInfo, fontGlyphInfo);
            if (errorCode != 0) {
                Debug.LogWarning("Error Code: " + errorCode + "  occurred while rendering font characters!");
                return fontAsset;
            }

            fontAsset.fontAssetType =
                (fontRenderMode >= TMPVendor::TMPro.EditorUtilities.RenderModes.DistanceField16) ?
                TMPVendor::TMPro.TMP_FontAsset.FontAssetTypes.SDF :
                TMPVendor::TMPro.TMP_FontAsset.FontAssetTypes.Bitmap;

            fontAsset.AddFaceInfo(ConvertToFaceInfo(fontFaceInfo));
            fontAsset.AddGlyphInfo(ConvertToGlyphs(fontGlyphInfo));

            var fontTexture = CreateFontTexture(atlasWidth, atlasHeight, textureBuffer, fontRenderMode);
            fontTexture.name = font.name + " Atlas";
            fontTexture.hideFlags = HideFlags.HideInHierarchy;

            fontAsset.atlas = fontTexture;
            
            // Create new Material and add it as Sub-Asset
            Shader shader = Shader.Find("TextMeshPro/Distance Field");
            if (shader == null) {
                Utils.LogWarning("shader is null!");
            }
            Material fontMaterial = new Material(shader);
            fontMaterial.name = font.name + " Material";
            
            fontMaterial.SetTexture(TMPVendor::TMPro.ShaderUtilities.ID_MainTex, fontTexture);
            fontMaterial.SetFloat(TMPVendor::TMPro.ShaderUtilities.ID_TextureWidth, fontTexture.width);
            fontMaterial.SetFloat(TMPVendor::TMPro.ShaderUtilities.ID_TextureHeight, fontTexture.height);
            fontMaterial.SetFloat(TMPVendor::TMPro.ShaderUtilities.ID_WeightNormal, fontAsset.normalStyle);
            fontMaterial.SetFloat(TMPVendor::TMPro.ShaderUtilities.ID_WeightBold, fontAsset.boldStyle);
            fontMaterial.SetFloat(TMPVendor::TMPro.ShaderUtilities.ID_GradientScale, characterPadding + 1);

            fontAsset.material = fontMaterial;

            fontAsset.ReadFontDefinition();

            return fontAsset;
        }

        private static Texture2D CreateFontTexture(
            int atlasWidth, int altasHeight, byte[] textureBuffer,
            TMPVendor::TMPro.EditorUtilities.RenderModes fontRenderMode) {
            var fontTexture = new Texture2D(atlasWidth, altasHeight, TextureFormat.Alpha8, mipmap: false, linear: true);

            Color32[] colors = new Color32[atlasWidth * altasHeight];
            for (int i = 0; i < (atlasWidth * altasHeight); i++) {
                byte c = textureBuffer[i];
                colors[i] = new Color32(c, c, c, c);
            }

            if (fontRenderMode == TMPVendor::TMPro.EditorUtilities.RenderModes.RasterHinted) {
                fontTexture.filterMode = FilterMode.Point;
            }

            fontTexture.SetPixels32(colors, miplevel: 0);
            fontTexture.Apply(updateMipmaps: false, makeNoLongerReadable: false);
            return fontTexture;
        }

        // Convert from FT_FaceInfo to FaceInfo
        private static TMPVendor::TMPro.FaceInfo ConvertToFaceInfo(TMPVendor::TMPro.EditorUtilities.FT_FaceInfo ftFace) {
            TMPVendor::TMPro.FaceInfo face = new TMPVendor::TMPro.FaceInfo();

            face.Name = ftFace.name;
            face.PointSize = (float)ftFace.pointSize;
            face.Padding = ftFace.padding;
            face.LineHeight = ftFace.lineHeight;
            face.Baseline = 0;
            face.Ascender = ftFace.ascender;
            face.Descender = ftFace.descender;
            face.CenterLine = ftFace.centerLine;
            face.Underline = ftFace.underline;
            face.UnderlineThickness = ftFace.underlineThickness == 0 ? 5 : ftFace.underlineThickness; // Set Thickness to 5 if TTF value is Zero.
            face.SuperscriptOffset = face.Ascender;
            face.SubscriptOffset = face.Underline;
            face.SubSize = 0.5f;
            //face.CharacterCount = ftFace.characterCount;
            face.AtlasWidth = ftFace.atlasWidth;
            face.AtlasHeight = ftFace.atlasHeight;

            if (face.strikethrough == 0) {
                face.strikethrough = face.CapHeight / 2.5f;
            }

            return face;
        }


        // Convert from FT_GlyphInfo[] to GlyphInfo[]
        private static TMPVendor::TMPro.TMP_Glyph[] ConvertToGlyphs(
            TMPVendor::TMPro.EditorUtilities.FT_GlyphInfo[] ftGlyphs) {
            List<TMPVendor::TMPro.TMP_Glyph> glyphs = new List<TMPVendor::TMPro.TMP_Glyph>();

            for (int i = 0; i < ftGlyphs.Length; i++) {
                TMPVendor::TMPro.TMP_Glyph g = new TMPVendor::TMPro.TMP_Glyph();

                g.id = ftGlyphs[i].id;
                g.x = ftGlyphs[i].x;
                g.y = ftGlyphs[i].y;
                g.width = ftGlyphs[i].width;
                g.height = ftGlyphs[i].height;
                g.xOffset = ftGlyphs[i].xOffset;
                g.yOffset = ftGlyphs[i].yOffset;
                g.xAdvance = ftGlyphs[i].xAdvance;

                // Filter out characters with missing glyphs.
                if (g.x == -1) {
                    continue;
                }

                glyphs.Add(g);
            }

            return glyphs.ToArray();
        }
    }
}
