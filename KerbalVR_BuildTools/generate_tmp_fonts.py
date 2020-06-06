import base64, math, os, re, shutil

# define some regexes
regex_glyph_info_list = re.compile(r'\s*m_glyphInfoList:')
regex_font_asset = re.compile(r'\s*MonoBehaviour:')
regex_font_info = re.compile(r'\s*m_fontInfo:')
regex_texture2d = re.compile(r'\s*Texture2D:')

# enum conversion functions
def int_to_font_asset_type(value):
    if value == 0:
        return 'TMP_FontAsset.FontAssetTypes.None'
    elif value == 1:
        return 'TMP_FontAsset.FontAssetTypes.SDF'
    elif value == 2:
        return 'TMP_FontAsset.FontAssetTypes.Bitmap'
    raise Exception('Unknown TMP_FontAsset.FontAssetTypes value ({0})'.format(value))

def int_to_texture_format(value):
    if value == 1:
        return 'TextureFormat.Alpha8'
    raise Exception('Unknown TextureFormat value ({0})'.format(value))

# get paths to the skeleton pose asset files
fonts_dir = os.path.join('..', 'KerbalVR_Unity', 'Assets', 'Fonts')
font_asset_files = [f for f in os.listdir(fonts_dir) if os.path.isfile(os.path.join(fonts_dir, f)) and f.endswith('.asset')]

print('Fonts Dir: {0}'.format(fonts_dir))

# parsing state machine
PARSING_STATES = {
    'none': 1,
    'parsing_font_asset': 2,
    'parsing_texture2d': 3,
}
PARSING_SUB_STATES = {
    'none': 1,
    'parsing_glyph_info_list': 2,
    'parsing_font_info': 3,
}

# init state machine
parsing_state = PARSING_STATES['none']
parsing_sub_state = PARSING_SUB_STATES['none']

for filename in font_asset_files:
    class_name = re.sub(r'\..*$', '', filename)
    class_filename = "KerbalVR_{0}.cs".format(class_name)
    texture_filename = "{0}_Texture.bin".format(class_name)

    asset_file_path = os.path.join(fonts_dir, filename) # original asset file
    class_file_path = os.path.join(fonts_dir, class_filename) # new C# file
    texture_file_path = os.path.join(fonts_dir, texture_filename) # texture file

    # object structure
    tmp_font_data = {
        'class_name': class_name,
        'name': '',
        'hashCode': 0,
        'materialHashCode': 0,
        'fontAssetType': 0,
        'normalStyle': 0.0,
        'normalSpacingOffset': 0.0,
        'boldStyle': 0.0,
        'boldSpacing': 0.0,
        'italicStyle': 0,
        'tabSize': 0,
        'fontInfo': {
            'Name': '',
            'PointSize': 0.0,
            'Scale': 0.0,
            'CharacterCount': 0,
            'LineHeight': 0.0,
            'Baseline': 0.0,
            'Ascender': 0.0,
            'CapHeight': 0.0,
            'Descender': 0.0,
            'CenterLine': 0.0,
            'SuperscriptOffset': 0.0,
            'SubscriptOffset': 0.0,
            'SubSize': 0.0,
            'Underline': 0.0,
            'UnderlineThickness': 0.0,
            'strikethrough': 0.0,
            'strikethroughThickness': 0.0,
            'TabWidth': 0.0,
            'Padding': 0.0,
            'AtlasWidth': 0,
            'AtlasHeight': 0,
        },
        'glyphInfoList': [],
        'Texture2D': {
            'name': '',
            'width': 0,
            'height': 0,
            'textureFormat': 0,
            'rawDataString': '',
        }
    }

    print('* Generating {0} from {1}'.format(class_filename, filename))

    with open(asset_file_path, 'r') as f_input:
        for line in f_input:
            line = line.rstrip()

            # parse the TMP_FontAsset data
            result = regex_font_asset.search(line)
            if result:
                parsing_state = PARSING_STATES['parsing_font_asset']
                continue

            if parsing_state == PARSING_STATES['parsing_font_asset']:
                result = re.search(r'\s*m_Name:\s*(.+)', line)
                if result:
                    tmp_font_data['name'] = result.group(1)
                    continue
                result = re.search(r'\s*hashCode:\s*(\d+)', line)
                if result:
                    tmp_font_data['hashCode'] = int(result.group(1))
                    continue
                result = re.search(r'\s*materialHashCode:\s*(\d+)', line)
                if result:
                    tmp_font_data['materialHashCode'] = int(result.group(1))
                    continue
                result = re.search(r'\s*fontAssetType:\s*(\d+)', line)
                if result:
                    tmp_font_data['fontAssetType'] = int(result.group(1))
                    continue
                result = re.search(r'\s*normalStyle:\s*([e0-9.-]+)', line)
                if result:
                    tmp_font_data['normalStyle'] = float(result.group(1))
                    continue
                result = re.search(r'\s*normalSpacingOffset:\s*([e0-9.-]+)', line)
                if result:
                    tmp_font_data['normalSpacingOffset'] = float(result.group(1))
                    continue
                result = re.search(r'\s*boldStyle:\s*([e0-9.-]+)', line)
                if result:
                    tmp_font_data['boldStyle'] = float(result.group(1))
                    continue
                result = re.search(r'\s*boldSpacing:\s*([e0-9.-]+)', line)
                if result:
                    tmp_font_data['boldSpacing'] = float(result.group(1))
                    continue
                result = re.search(r'\s*italicStyle:\s*(\d+)', line)
                if result:
                    tmp_font_data['italicStyle'] = int(result.group(1))
                    continue
                result = re.search(r'\s*tabSize:\s*(\d+)', line)
                if result:
                    tmp_font_data['tabSize'] = int(result.group(1))
                    continue

                # parse the FaceInfo data
                result = regex_font_info.search(line)
                if result:
                    parsing_sub_state = PARSING_SUB_STATES['parsing_font_info']
                    continue

                if parsing_sub_state == PARSING_SUB_STATES['parsing_font_info']:
                    result = re.search(r'\s*Name:\s*(.+)', line)
                    if result:
                        tmp_font_data['fontInfo']['Name'] = result.group(1)
                        continue
                    result = re.search(r'\s*PointSize:\s*(.+)', line)
                    if result:
                        tmp_font_data['fontInfo']['PointSize'] = float(result.group(1))
                        continue
                    result = re.search(r'\s*Scale:\s*([e0-9.-]+)', line)
                    if result:
                        tmp_font_data['fontInfo']['Scale'] = float(result.group(1))
                        continue
                    result = re.search(r'\s*CharacterCount:\s*([e0-9.-]+)', line)
                    if result:
                        tmp_font_data['fontInfo']['CharacterCount'] = result.group(1)
                        continue
                    result = re.search(r'\s*LineHeight:\s*(.+)', line)
                    if result:
                        tmp_font_data['fontInfo']['LineHeight'] = float(result.group(1))
                        continue
                    result = re.search(r'\s*Baseline:\s*([e0-9.-]+)', line)
                    if result:
                        tmp_font_data['fontInfo']['Baseline'] = float(result.group(1))
                        continue
                    result = re.search(r'\s*Ascender:\s*([e0-9.-]+)', line)
                    if result:
                        tmp_font_data['fontInfo']['Ascender'] = float(result.group(1))
                        continue
                    result = re.search(r'\s*CapHeight:\s*([e0-9.-]+)', line)
                    if result:
                        tmp_font_data['fontInfo']['CapHeight'] = float(result.group(1))
                        continue
                    result = re.search(r'\s*Descender:\s*([e0-9.-]+)', line)
                    if result:
                        tmp_font_data['fontInfo']['Descender'] = float(result.group(1))
                        continue
                    result = re.search(r'\s*CenterLine:\s*([e0-9.-]+)', line)
                    if result:
                        tmp_font_data['fontInfo']['CenterLine'] = float(result.group(1))
                        continue
                    result = re.search(r'\s*SuperscriptOffset:\s*([e0-9.-]+)', line)
                    if result:
                        tmp_font_data['fontInfo']['SuperscriptOffset'] = float(result.group(1))
                        continue
                    result = re.search(r'\s*SubscriptOffset:\s*([e0-9.-]+)', line)
                    if result:
                        tmp_font_data['fontInfo']['SubscriptOffset'] = float(result.group(1))
                        continue
                    result = re.search(r'\s*SubSize:\s*([e0-9.-]+)', line)
                    if result:
                        tmp_font_data['fontInfo']['SubSize'] = float(result.group(1))
                        continue
                    result = re.search(r'\s*Underline:\s*([e0-9.-]+)', line)
                    if result:
                        tmp_font_data['fontInfo']['Underline'] = float(result.group(1))
                        continue
                    result = re.search(r'\s*UnderlineThickness:\s*([e0-9.-]+)', line)
                    if result:
                        tmp_font_data['fontInfo']['UnderlineThickness'] = float(result.group(1))
                        continue
                    result = re.search(r'\s*strikethrough:\s*([e0-9.-]+)', line)
                    if result:
                        tmp_font_data['fontInfo']['strikethrough'] = float(result.group(1))
                        continue
                    result = re.search(r'\s*strikethroughThickness:\s*([e0-9.-]+)', line)
                    if result:
                        tmp_font_data['fontInfo']['strikethroughThickness'] = float(result.group(1))
                        continue
                    result = re.search(r'\s*TabWidth:\s*([e0-9.-]+)', line)
                    if result:
                        tmp_font_data['fontInfo']['TabWidth'] = float(result.group(1))
                        continue
                    result = re.search(r'\s*Padding:\s*([e0-9.-]+)', line)
                    if result:
                        tmp_font_data['fontInfo']['Padding'] = float(result.group(1))
                        continue
                    result = re.search(r'\s*AtlasWidth:\s*(.+)', line)
                    if result:
                        tmp_font_data['fontInfo']['AtlasWidth'] = result.group(1)
                        continue
                    result = re.search(r'\s*AtlasHeight:\s*(.+)', line)
                    if result:
                        tmp_font_data['fontInfo']['AtlasHeight'] = result.group(1)
                        continue
                    # done, no more font face info to process
                    parsing_sub_state = PARSING_SUB_STATES['none']

                # parse the list of glyphs
                result = regex_glyph_info_list.search(line)
                if result:
                    parsing_sub_state = PARSING_SUB_STATES['parsing_glyph_info_list']
                    continue

                if parsing_sub_state == PARSING_SUB_STATES['parsing_glyph_info_list']:
                    result = re.search(r'\s*- id:\s*(\d+)', line)
                    if result:
                        value = int(result.group(1))
                        tmp_font_data['glyphInfoList'].append({
                            'id': value,
                            'x': 0.0,
                            'y': 0.0,
                            'width': 0.0,
                            'height': 0.0,
                            'xOffset': 0.0,
                            'yOffset': 0.0,
                            'xAdvance': 0.0,
                            'scale': 0.0,
                        })
                        continue

                    result = re.search(r'\s*x:\s*([e0-9.-]+)', line)
                    if result:
                        tmp_font_data['glyphInfoList'][-1]['x'] = float(result.group(1))
                        continue
                    result = re.search(r'\s*y:\s*([e0-9.-]+)', line)
                    if result:
                        tmp_font_data['glyphInfoList'][-1]['y'] = float(result.group(1))
                        continue
                    result = re.search(r'\s*width:\s*([e0-9.-]+)', line)
                    if result:
                        tmp_font_data['glyphInfoList'][-1]['width'] = float(result.group(1))
                        continue
                    result = re.search(r'\s*height:\s*([e0-9.-]+)', line)
                    if result:
                        tmp_font_data['glyphInfoList'][-1]['height'] = float(result.group(1))
                        continue
                    result = re.search(r'\s*xOffset:\s*([e0-9.-]+)', line)
                    if result:
                        tmp_font_data['glyphInfoList'][-1]['xOffset'] = float(result.group(1))
                        continue
                    result = re.search(r'\s*yOffset:\s*([e0-9.-]+)', line)
                    if result:
                        tmp_font_data['glyphInfoList'][-1]['yOffset'] = float(result.group(1))
                        continue
                    result = re.search(r'\s*xAdvance:\s*([e0-9.-]+)', line)
                    if result:
                        tmp_font_data['glyphInfoList'][-1]['xAdvance'] = float(result.group(1))
                        continue
                    result = re.search(r'\s*scale:\s*([e0-9.-]+)', line)
                    if result:
                        tmp_font_data['glyphInfoList'][-1]['scale'] = float(result.group(1))
                        continue
                    # done, no more glyphs to process
                    parsing_sub_state = PARSING_SUB_STATES['none']

                if re.search(r'^---', line):
                    # done processing TMP_FontAsset (Monobehaviour) data
                    parsing_state = PARSING_STATES['none']
                    continue


            # parse the Texture2D data
            result = regex_texture2d.search(line)
            if result:
                parsing_state = PARSING_STATES['parsing_texture2d']
                continue

            if parsing_state == PARSING_STATES['parsing_texture2d']:
                result = re.search(r'\s*m_Name:\s*(.+)', line)
                if result:
                    tmp_font_data['Texture2D']['name'] = result.group(1)
                result = re.search(r'\s*m_Width:\s*(\d+)', line)
                if result:
                    tmp_font_data['Texture2D']['width'] = int(result.group(1))
                result = re.search(r'\s*m_Height:\s*(\d+)', line)
                if result:
                    tmp_font_data['Texture2D']['height'] = int(result.group(1))
                result = re.search(r'\s*m_TextureFormat:\s*(\d+)', line)
                if result:
                    tmp_font_data['Texture2D']['textureFormat'] = int(result.group(1))
                result = re.search(r'\s*_typelessdata:\s*([A-Fa-f0-9]+)', line)
                if result:
                    tmp_font_data['Texture2D']['rawDataString'] = result.group(1)

                if re.search(r'^---', line):
                    # done processing Texture2D data
                    parsing_state = PARSING_STATES['none']
                    continue


    # write the output C# file
    with open(class_file_path, 'w') as out:
        out.write('/* ===============================================\n')
        out.write(' *   This is an auto-generated file for KerbalVR. \n')
        out.write(' *   Do not edit by hand.                         \n')
        out.write(' * ===============================================\n')
        out.write(' */\n\n')
        out.write('using System.IO;\n')
        out.write('using TMPro;\n')
        out.write('using Unity.Collections;\n')
        out.write('using UnityEngine;\n\n')
        out.write('namespace KerbalVR.Fonts {\n')
        out.write('    public class TMPFont_{0} {{\n'.format(tmp_font_data['class_name']))
        out.write('        public static TMPro.TMP_FontAsset GetInstance() {\n')
        out.write('            TMPro.TMP_FontAsset tmpFont = ScriptableObject.CreateInstance<TMPro.TMP_FontAsset>();\n')
        out.write('            tmpFont.name = "{0}";\n'.format(tmp_font_data['name']))
        out.write('            tmpFont.hashCode = {0};\n'.format(tmp_font_data['hashCode']))
        out.write('            tmpFont.materialHashCode = {0};\n'.format(tmp_font_data['materialHashCode']))
        out.write('            tmpFont.fontAssetType = {0};\n'.format(int_to_font_asset_type(tmp_font_data['fontAssetType'])))
        out.write('            tmpFont.normalStyle = {0}f;\n'.format(tmp_font_data['normalStyle']))
        out.write('            tmpFont.normalSpacingOffset = {0}f;\n'.format(tmp_font_data['normalSpacingOffset']))
        out.write('            tmpFont.boldStyle = {0}f;\n'.format(tmp_font_data['boldStyle']))
        out.write('            tmpFont.boldSpacing = {0}f;\n'.format(tmp_font_data['boldSpacing']))
        out.write('            tmpFont.italicStyle = {0};\n'.format(tmp_font_data['italicStyle']))
        out.write('            tmpFont.tabSize = {0};\n'.format(tmp_font_data['tabSize']))

        out.write('            tmpFont.AddFaceInfo(new FaceInfo() {\n')
        out.write('                Name = "{0}",\n'.format(tmp_font_data['fontInfo']['Name']))
        out.write('                PointSize = {0}f,\n'.format(tmp_font_data['fontInfo']['PointSize']))
        out.write('                Scale = {0}f,\n'.format(tmp_font_data['fontInfo']['Scale']))
        out.write('                CharacterCount = {0},\n'.format(tmp_font_data['fontInfo']['CharacterCount']))
        out.write('                LineHeight = {0}f,\n'.format(tmp_font_data['fontInfo']['LineHeight']))
        out.write('                Baseline = {0}f,\n'.format(tmp_font_data['fontInfo']['Baseline']))
        out.write('                Ascender = {0}f,\n'.format(tmp_font_data['fontInfo']['Ascender']))
        out.write('                CapHeight = {0}f,\n'.format(tmp_font_data['fontInfo']['CapHeight']))
        out.write('                Descender = {0}f,\n'.format(tmp_font_data['fontInfo']['Descender']))
        out.write('                CenterLine = {0}f,\n'.format(tmp_font_data['fontInfo']['CenterLine']))
        out.write('                SuperscriptOffset = {0}f,\n'.format(tmp_font_data['fontInfo']['SuperscriptOffset']))
        out.write('                SubscriptOffset = {0}f,\n'.format(tmp_font_data['fontInfo']['SubscriptOffset']))
        out.write('                SubSize = {0}f,\n'.format(tmp_font_data['fontInfo']['SubSize']))
        out.write('                Underline = {0}f,\n'.format(tmp_font_data['fontInfo']['Underline']))
        out.write('                UnderlineThickness = {0}f,\n'.format(tmp_font_data['fontInfo']['UnderlineThickness']))
        out.write('                strikethrough = {0}f,\n'.format(tmp_font_data['fontInfo']['strikethrough']))
        out.write('                strikethroughThickness = {0}f,\n'.format(tmp_font_data['fontInfo']['strikethroughThickness']))
        out.write('                TabWidth = {0}f,\n'.format(tmp_font_data['fontInfo']['TabWidth']))
        out.write('                Padding = {0}f,\n'.format(tmp_font_data['fontInfo']['Padding']))
        out.write('                AtlasWidth = {0},\n'.format(tmp_font_data['fontInfo']['AtlasWidth']))
        out.write('                AtlasHeight = {0},\n'.format(tmp_font_data['fontInfo']['AtlasHeight']))
        out.write('            });\n')

        out.write('            TMP_Glyph[] glyphs = {\n')
        for glyph in tmp_font_data['glyphInfoList']:
            out.write('                new TMP_Glyph() {\n')
            out.write('                    id = {0},\n'.format(glyph['id']))
            out.write('                    x = {0}f,\n'.format(glyph['x']))
            out.write('                    y = {0}f,\n'.format(glyph['y']))
            out.write('                    width = {0}f,\n'.format(glyph['width']))
            out.write('                    height = {0}f,\n'.format(glyph['height']))
            out.write('                    xOffset = {0}f,\n'.format(glyph['xOffset']))
            out.write('                    yOffset = {0}f,\n'.format(glyph['yOffset']))
            out.write('                    xAdvance = {0}f,\n'.format(glyph['xAdvance']))
            out.write('                    scale = {0}f,\n'.format(glyph['scale']))
            out.write('                },\n')
        out.write('            };\n')
        out.write('            tmpFont.AddGlyphInfo(glyphs);\n')
        out.write('            tmpFont.SortGlyphs();\n')

        out.write('            Texture2D tmpTexture = new Texture2D({0}, {1}, {2}, false);\n'.format(tmp_font_data['Texture2D']['width'], tmp_font_data['Texture2D']['height'], int_to_texture_format(tmp_font_data['Texture2D']['textureFormat'])))
        out.write('            tmpTexture.name = "{0}";\n'.format(tmp_font_data['Texture2D']['name']))

        out.write('            string textureFilePath = Path.Combine(KSPUtil.ApplicationRootPath, "GameData", KerbalVR.Globals.KERBALVR_TEXTURES_DIR, "{0}_Texture.bin");\n'.format(tmp_font_data['class_name']))
        out.write('            byte[] textureBytes = File.ReadAllBytes(textureFilePath);\n')

        out.write('            NativeArray<byte> textureData = tmpTexture.GetRawTextureData<byte>();\n')
        out.write('            textureData.CopyFrom(textureBytes);\n')
        out.write('            tmpTexture.Apply();\n')
        out.write('            tmpFont.atlas = tmpTexture;\n')

        out.write('            Material tmpMaterial = new Material(Shader.Find("TextMeshPro/Distance Field"));\n')
        out.write('            tmpMaterial.name = "{0} Material";\n'.format(tmp_font_data['name']))
        out.write('            tmpMaterial.mainTexture = tmpTexture;\n')
        out.write('            tmpFont.material = tmpMaterial;\n')
        out.write('            tmpFont.AddKerningInfo(new KerningTable());\n')
        out.write('            tmpFont.ReadFontDefinition();\n')

        out.write('            return tmpFont;\n')
        out.write('        }\n')
        out.write('    }\n')
        out.write('}\n')

    # write the texture to a file
    rawDataString = tmp_font_data['Texture2D']['rawDataString']
    rawDataBytes = bytearray()
    for strIdx in range(0, len(rawDataString), 2):
        rawDataSubStr = rawDataString[strIdx:strIdx+2]
        rawDataByte = int(rawDataSubStr, 16)
        rawDataBytes.append(rawDataByte)

    with open(texture_file_path, 'wb') as tex_out:
        tex_out.write(rawDataBytes)



# move the output class files to the KerbalVR_Mod folder
input_files_path = fonts_dir
output_files = [f for f in os.listdir(input_files_path) if os.path.isfile(os.path.join(input_files_path, f)) and f.endswith('.cs')]
output_path = os.path.join('..', 'KerbalVR_Mod', 'KerbalVR', 'TMPFonts')

for filename in output_files:
    input_full_path = os.path.abspath(os.path.join(input_files_path, filename))
    output_full_path = os.path.abspath(os.path.join(output_path, filename))

    shutil.move(input_full_path, output_full_path)

# move the output texture files to the KerbalVR_Mod folder
output_files = [f for f in os.listdir(input_files_path) if os.path.isfile(os.path.join(input_files_path, f)) and f.endswith('.bin')]
output_path = os.path.join('..', 'KerbalVR_Mod', 'KerbalVR', 'Assets', 'Textures')

for filename in output_files:
    input_full_path = os.path.abspath(os.path.join(input_files_path, filename))
    output_full_path = os.path.abspath(os.path.join(output_path, filename))

    shutil.move(input_full_path, output_full_path)
