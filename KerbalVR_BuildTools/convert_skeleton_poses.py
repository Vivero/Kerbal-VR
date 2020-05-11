import os, re, shutil

# define some regexes
regex_left_hand = re.compile(r'\s*leftHand:')
regex_right_hand = re.compile(r'\s*rightHand:')
regex_apply_root = re.compile(r'\s*applyToSkeletonRoot:\s*(\d+)')
regex_vector3 = re.compile(r'{\s*x:\s*([e0-9.-]+),\s*y:\s*([e0-9.-]+),\s*z:\s*([e0-9.-]+)\s*}')
regex_quaternion = re.compile(r'{\s*x:\s*([e0-9.-]+),\s*y:\s*([e0-9.-]+),\s*z:\s*([e0-9.-]+),\s*w:\s*([e0-9.-]+)\s*}')

def int_to_steamvr_input_sources(value):
    if value == 1:
        return 'SteamVR_Input_Sources.LeftHand'
    elif value == 2:
        return 'SteamVR_Input_Sources.RightHand'
    raise Exception('Unknown SteamVR_Input_Sources value ({0})'.format(value))

def int_to_steamvr_skeleton_finger_extension_types(value):
    if value == 0:
        return 'SteamVR_Skeleton_FingerExtensionTypes.Static'
    elif value == 1:
        return 'SteamVR_Skeleton_FingerExtensionTypes.Free'
    elif value == 2:
        return 'SteamVR_Skeleton_FingerExtensionTypes.Extend'
    elif value == 3:
        return 'SteamVR_Skeleton_FingerExtensionTypes.Contract'
    raise Exception('Unknown SteamVR_Skeleton_FingerExtensionTypes value ({0})'.format(value))

def vector3_str_to_dict(value_str):
    result = regex_vector3.search(value_str)
    if result:
        x = float(result.group(1))
        y = float(result.group(2))
        z = float(result.group(3))
        return {'x': x, 'y': y, 'z': z}
    raise Exception('Could not parse Vector3 string into dict: {0}'.format(value_str))

def vector3_to_str(vector3_obj):
    return 'new Vector3({0}f, {1}f, {2}f)'.format(vector3_obj['x'], vector3_obj['y'], vector3_obj['z'])

def quaternion_str_to_dict(value_str):
    result = regex_quaternion.search(value_str)
    if result:
        x = float(result.group(1))
        y = float(result.group(2))
        z = float(result.group(3))
        w = float(result.group(4))
        return {'x': x, 'y': y, 'z': z, 'w': w}
    raise Exception('Could not parse Quaternion string into dict: {0}'.format(value_str))

def quaternion_to_str(quaternion_obj):
    return 'new Quaternion({0}f, {1}f, {2}f, {3}f)'.format(quaternion_obj['x'], quaternion_obj['y'], quaternion_obj['z'], quaternion_obj['w'])


# get paths to the skeleton pose asset files
skeleton_poses_dir = os.path.join('..', 'KerbalVR_UnitySteamVR', 'Assets', 'SkeletonPoses')
skeleton_poses_asset_files = [f for f in os.listdir(skeleton_poses_dir) if os.path.isfile(os.path.join(skeleton_poses_dir, f)) and f.endswith('.asset')]

print('Skeleton Poses Dir: {0}'.format(skeleton_poses_dir))

# parsing state machine
PARSING_STATES = {
    'none': 1,
    'parsing_left_hand': 2,
    'parsing_right_hand': 3,
}
PARSING_STATES_BONES = {
    'none': 1,
    'parsing_bone_positions': 2,
    'parsing_bone_rotations': 3,
}

# init state machine
parsing_state = PARSING_STATES['none']
parsing_state_bones = PARSING_STATES_BONES['none']

for filename in skeleton_poses_asset_files:
    class_name = re.sub(r'\..*$', '', filename)
    class_filename = re.sub(r'\.asset$', '.cs', filename)
    asset_file_path = os.path.join(skeleton_poses_dir, filename) # original asset file
    class_file_path = os.path.join(skeleton_poses_dir, class_filename) # new C# file

    # object structure
    skeleton_pose = {
        'class_name': class_name,
        'leftHand': {
            'inputSource': 0,
            'thumbFingerMovementType': 0,
            'indexFingerMovementType': 0,
            'middleFingerMovementType': 0,
            'ringFingerMovementType': 0,
            'pinkyFingerMovementType': 0,
            'ignoreRootPoseData': True,
            'ignoreWristPoseData': True,
            'position': { 'x': 0, 'y': 0, 'z': 0},
            'rotation': { 'x': 0, 'y': 0, 'z': 0, 'w': 0},
            'bonePositions': [],
            'boneRotations': [],
        },
        'rightHand': {
            'inputSource': 0,
            'thumbFingerMovementType': 0,
            'indexFingerMovementType': 0,
            'middleFingerMovementType': 0,
            'ringFingerMovementType': 0,
            'pinkyFingerMovementType': 0,
            'ignoreRootPoseData': True,
            'ignoreWristPoseData': True,
            'position': { 'x': 0, 'y': 0, 'z': 0},
            'rotation': { 'x': 0, 'y': 0, 'z': 0, 'w': 0},
            'bonePositions': [],
            'boneRotations': [],
        },
        'applyToSkeletonRoot': True,
    }

    print('* Generating {0} from {1}'.format(class_filename, filename))

    with open(asset_file_path, 'r') as f_input:
        for line in f_input:
            line = line.rstrip()

            result = regex_apply_root.search(line)
            if result:
                apply_to_skeleton_root = int(result.group(1))
                skeleton_pose['applyToSkeletonRoot'] = apply_to_skeleton_root != 0

            result = regex_left_hand.search(line)
            if result:
                parsing_state = PARSING_STATES['parsing_left_hand']
            result = regex_right_hand.search(line)
            if result:
                parsing_state = PARSING_STATES['parsing_right_hand']

            # select a key for parsing the left or right Hand objects
            if parsing_state == PARSING_STATES['parsing_left_hand']:
                hand_key = 'leftHand'
            elif parsing_state == PARSING_STATES['parsing_right_hand']:
                hand_key = 'rightHand'

            # parse the Hand object keys
            if parsing_state == PARSING_STATES['parsing_left_hand'] or parsing_state == PARSING_STATES['parsing_right_hand']:
                # input source
                result = re.search(r'\s*inputSource:\s*(\d+)', line)
                if result:
                    value_int = int(result.group(1))
                    skeleton_pose[hand_key]['inputSource'] = int_to_steamvr_input_sources(value_int)

                # finger movement types
                result = re.search(r'\s*(\w+)FingerMovementType:\s*(\d+)', line)
                if result:
                    finger = result.group(1)
                    value_int = int(result.group(2))
                    finger_key = '{0}FingerMovementType'.format(finger)
                    skeleton_pose[hand_key][finger_key] = int_to_steamvr_skeleton_finger_extension_types(value_int)

                # ignore pose data
                result = re.search(r'\s*ignoreRootPoseData:\s*(\d+)', line)
                if result:
                    value_int = int(result.group(1))
                    skeleton_pose[hand_key]['ignoreRootPoseData'] = value_int != 0
                result = re.search(r'\s*ignoreWristPoseData:\s*(\d+)', line)
                if result:
                    value_int = int(result.group(1))
                    skeleton_pose[hand_key]['ignoreWristPoseData'] = value_int != 0

                # hand pose
                result = re.search(r'\s*position:\s*(.+)', line)
                if result:
                    vector3_dict = vector3_str_to_dict(result.group(1))
                    skeleton_pose[hand_key]['position'] = vector3_to_str(vector3_dict)

                result = re.search(r'\s*rotation:\s*(.+)', line)
                if result:
                    quaternion_dict = quaternion_str_to_dict(result.group(1))
                    skeleton_pose[hand_key]['rotation'] = quaternion_to_str(quaternion_dict)

                # parse the bone positions and rotations
                result = re.search(r'\s*bonePositions:', line)
                if result:
                    parsing_state_bones = PARSING_STATES_BONES['parsing_bone_positions']
                result = re.search(r'\s*boneRotations:', line)
                if result:
                    parsing_state_bones = PARSING_STATES_BONES['parsing_bone_rotations']

                if parsing_state_bones == PARSING_STATES_BONES['parsing_bone_positions']:
                    pose_key = 'bonePositions'
                elif parsing_state_bones == PARSING_STATES_BONES['parsing_bone_rotations']:
                    pose_key = 'boneRotations'

                if parsing_state_bones == PARSING_STATES_BONES['parsing_bone_positions'] or \
                    parsing_state_bones == PARSING_STATES_BONES['parsing_bone_rotations']:

                    result = re.search(r'\s*-\s*({[xyzwe0-9.,:\s-]+})', line)
                    if result:
                        vector = result.group(1)

                        if parsing_state_bones == PARSING_STATES_BONES['parsing_bone_positions']:
                            skeleton_pose[hand_key][pose_key].append(vector3_str_to_dict(vector))
                        elif parsing_state_bones == PARSING_STATES_BONES['parsing_bone_rotations']:
                            skeleton_pose[hand_key][pose_key].append(quaternion_str_to_dict(vector))



    # write the output C# file
    with open(class_file_path, 'w') as out:
        out.write('/* ===============================================\n')
        out.write(' *   This is an auto-generated file for KerbalVR. \n')
        out.write(' *   Do not edit by hand.                         \n')
        out.write(' * ===============================================\n')
        out.write(' */\n\n')
        out.write('using UnityEngine;\n')
        out.write('using Valve.VR;\n\n')
        out.write('namespace KerbalVR {\n')
        out.write('    public class SkeletonPose_{0} {{\n'.format(skeleton_pose['class_name']))
        out.write('        public SteamVR_Skeleton_Pose GetInstance() {\n')
        out.write('            SteamVR_Skeleton_Pose pose = ScriptableObject.CreateInstance<SteamVR_Skeleton_Pose>();\n')
        out.write('            pose.applyToSkeletonRoot = {0};\n'.format('true' if skeleton_pose['applyToSkeletonRoot'] else 'false'))

        for hand_key in ['leftHand', 'rightHand']:
            out.write('            pose.{0}.inputSource = {1};\n'.format(hand_key, skeleton_pose[hand_key]['inputSource']))
            out.write('            pose.{0}.thumbFingerMovementType = {1};\n'.format(hand_key, skeleton_pose[hand_key]['thumbFingerMovementType']))
            out.write('            pose.{0}.indexFingerMovementType = {1};\n'.format(hand_key, skeleton_pose[hand_key]['indexFingerMovementType']))
            out.write('            pose.{0}.middleFingerMovementType = {1};\n'.format(hand_key, skeleton_pose[hand_key]['middleFingerMovementType']))
            out.write('            pose.{0}.ringFingerMovementType = {1};\n'.format(hand_key, skeleton_pose[hand_key]['ringFingerMovementType']))
            out.write('            pose.{0}.pinkyFingerMovementType = {1};\n'.format(hand_key, skeleton_pose[hand_key]['pinkyFingerMovementType']))
            out.write('            pose.{0}.ignoreRootPoseData = {1};\n'.format(hand_key, 'true' if skeleton_pose[hand_key]['ignoreRootPoseData'] else 'false'))
            out.write('            pose.{0}.ignoreWristPoseData = {1};\n'.format(hand_key, 'true' if skeleton_pose[hand_key]['ignoreWristPoseData'] else 'false'))
            out.write('            pose.{0}.position = {1};\n'.format(hand_key, skeleton_pose[hand_key]['position']))
            out.write('            pose.{0}.rotation = {1};\n'.format(hand_key, skeleton_pose[hand_key]['rotation']))

            bone_positions_count = len(skeleton_pose[hand_key]['bonePositions'])
            out.write('            pose.{0}.bonePositions = new Vector3[{1}] {{'.format(hand_key, bone_positions_count))
            if bone_positions_count == 0:
                out.write(' };\n')
            else:
                for vector3 in skeleton_pose[hand_key]['bonePositions']:
                    out.write('\n                {1},'.format(hand_key, vector3_to_str(vector3)))
                out.write('\n            };\n')

            bone_rotations_count = len(skeleton_pose[hand_key]['boneRotations'])
            out.write('            pose.{0}.boneRotations = new Quaternion[{1}] {{'.format(hand_key, bone_rotations_count))
            if bone_rotations_count == 0:
                out.write(' };\n')
            else:
                for quaternion in skeleton_pose[hand_key]['boneRotations']:
                    out.write('\n                {1},'.format(hand_key, quaternion_to_str(quaternion)))
                out.write('\n            };\n')

        out.write('            return pose;\n')
        out.write('        }\n')
        out.write('    }\n')
        out.write('}\n')


# move the output files to the KerbalVR_Mod folder
skeleton_poses_cs_files = [f for f in os.listdir(skeleton_poses_dir) if os.path.isfile(os.path.join(skeleton_poses_dir, f)) and f.endswith('.cs')]
output_path = os.path.join('..', 'KerbalVR_Mod', 'KerbalVR', 'SkeletonPoses')

for filename in skeleton_poses_cs_files:
    cs_file_path = os.path.abspath(os.path.join(skeleton_poses_dir, filename))
    move_path = os.path.abspath(os.path.join(output_path, filename))

    shutil.move(cs_file_path, move_path)
