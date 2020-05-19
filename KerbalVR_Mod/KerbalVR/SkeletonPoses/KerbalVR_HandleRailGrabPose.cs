/* ===============================================
 *   This is an auto-generated file for KerbalVR. 
 *   Do not edit by hand.                         
 * ===============================================
 */

using UnityEngine;
using Valve.VR;

namespace KerbalVR {
    public class SkeletonPose_HandleRailGrabPose {
        public static SteamVR_Skeleton_Pose GetInstance() {
            SteamVR_Skeleton_Pose pose = ScriptableObject.CreateInstance<SteamVR_Skeleton_Pose>();
            pose.applyToSkeletonRoot = true;
            pose.leftHand.inputSource = SteamVR_Input_Sources.LeftHand;
            pose.leftHand.thumbFingerMovementType = SteamVR_Skeleton_FingerExtensionTypes.Static;
            pose.leftHand.indexFingerMovementType = SteamVR_Skeleton_FingerExtensionTypes.Static;
            pose.leftHand.middleFingerMovementType = SteamVR_Skeleton_FingerExtensionTypes.Static;
            pose.leftHand.ringFingerMovementType = SteamVR_Skeleton_FingerExtensionTypes.Static;
            pose.leftHand.pinkyFingerMovementType = SteamVR_Skeleton_FingerExtensionTypes.Static;
            pose.leftHand.ignoreRootPoseData = true;
            pose.leftHand.ignoreWristPoseData = true;
            pose.leftHand.position = new Vector3(0.005119898f, -0.04334565f, -0.05492314f);
            pose.leftHand.rotation = new Quaternion(-0.38216498f, -0.048071682f, 0.019911945f, -0.9226281f);
            pose.leftHand.bonePositions = new Vector3[31] {
                new Vector3(-0.0f, 0.0f, 0.0f),
                new Vector3(-0.0374f, 0.0301f, 0.1573f),
                new Vector3(-0.012083273f, 0.028070245f, 0.025049686f),
                new Vector3(0.040406488f, 7.8231096e-08f, 6.6682696e-07f),
                new Vector3(0.0325168f, -5.4875272e-08f, -1.2340024e-08f),
                new Vector3(0.030463902f, 1.6269207e-07f, 7.92839e-08f),
                new Vector3(0.0006324522f, 0.026866155f, 0.015001948f),
                new Vector3(0.074204385f, 0.005002201f, -0.00023377323f),
                new Vector3(0.043930072f, 0.0f, 0.0f),
                new Vector3(0.02869547f, -9.398158e-08f, -1.2649753e-07f),
                new Vector3(0.022821384f, -1.4365155e-07f, 7.651614e-08f),
                new Vector3(0.0021773134f, 0.007119544f, 0.016318738f),
                new Vector3(0.07095288f, -0.00077883265f, -0.000997186f),
                new Vector3(0.043108486f, -9.950596e-08f, -6.7041825e-09f),
                new Vector3(0.033266045f, -1.320567e-08f, -2.1670374e-08f),
                new Vector3(0.025892371f, 9.984198e-08f, -2.0352908e-09f),
                new Vector3(0.0005134356f, -0.0065451227f, 0.016347693f),
                new Vector3(0.06587581f, -0.0017857892f, -0.00069344096f),
                new Vector3(0.04069671f, -9.5347104e-08f, -2.2934731e-08f),
                new Vector3(0.028746964f, 1.0089892e-07f, 4.5306827e-08f),
                new Vector3(0.022430236f, 1.0846127e-07f, -1.7428562e-08f),
                new Vector3(-0.002478151f, -0.01898137f, 0.015213584f),
                new Vector3(0.0628784f, -0.0028440943f, -0.00033152848f),
                new Vector3(0.03021974f, -8.8708475e-08f, -5.098991e-08f),
                new Vector3(0.018186722f, -8.242205e-08f, -2.0070001e-07f),
                new Vector3(0.01801794f, -2.00012e-08f, 6.59746e-08f),
                new Vector3(-0.0060591106f, 0.05628522f, 0.060063843f),
                new Vector3(-0.04041555f, -0.043017667f, 0.019344581f),
                new Vector3(-0.03935372f, -0.07567404f, 0.047048334f),
                new Vector3(-0.038340144f, -0.09098663f, 0.08257892f),
                new Vector3(-0.031805996f, -0.08721431f, 0.12101539f),
            };
            pose.leftHand.boneRotations = new Quaternion[31] {
                new Quaternion(-6.123234e-17f, 1.0f, 6.123234e-17f, -4.371139e-08f),
                new Quaternion(-0.078608155f, -0.92027926f, 0.3792963f, -0.055146642f),
                new Quaternion(-0.26145115f, -0.7213955f, 0.41618466f, 0.48787525f),
                new Quaternion(0.11937635f, -0.056662083f, -0.26811036f, 0.9542828f),
                new Quaternion(-0.10534973f, 0.09216707f, -0.24559677f, 0.95921266f),
                new Quaternion(-1.3877788e-17f, -1.3877788e-17f, -5.551115e-17f, 1.0f),
                new Quaternion(-0.6442515f, -0.42213318f, -0.4782025f, 0.42197865f),
                new Quaternion(0.019549787f, 0.0116787795f, -0.596928f, 0.8019716f),
                new Quaternion(0.034517456f, 0.004169758f, -0.42240983f, 0.9057379f),
                new Quaternion(-0.025964372f, 0.002702249f, -0.6257829f, 0.7795604f),
                new Quaternion(6.938894e-18f, 1.9428903e-16f, -1.348151e-33f, 1.0f),
                new Quaternion(-0.5564024f, -0.47263962f, -0.43028674f, 0.5309252f),
                new Quaternion(-0.18400313f, -0.018806672f, -0.56584114f, 0.8035005f),
                new Quaternion(0.04917431f, 0.04468566f, -0.4810155f, 0.87419057f),
                new Quaternion(-0.02132691f, 0.0187856f, -0.6167448f, 0.78665f),
                new Quaternion(1.1639192e-17f, -5.602331e-17f, -0.040125635f, 0.9991947f),
                new Quaternion(-0.53544044f, -0.45080867f, -0.47522852f, 0.5331351f),
                new Quaternion(-0.12557463f, -0.01384405f, -0.50291634f, 0.8550524f),
                new Quaternion(-0.001956232f, -0.0010904209f, -0.5114048f, 0.8593371f),
                new Quaternion(-0.0077420403f, 0.010083841f, -0.5958945f, 0.80296206f),
                new Quaternion(6.938894e-18f, -9.62965e-35f, -1.3877788e-17f, 1.0f),
                new Quaternion(-0.58007795f, -0.37509266f, -0.5312601f, 0.49048728f),
                new Quaternion(0.09443956f, -0.026952697f, -0.41413692f, 0.9049008f),
                new Quaternion(0.03410991f, -0.06371035f, -0.37493703f, 0.92422926f),
                new Quaternion(-0.024831f, -0.10681728f, -0.37987846f, 0.9185129f),
                new Quaternion(0.0f, 0.0f, 1.9081958e-17f, 1.0f),
                new Quaternion(0.20274544f, 0.59426665f, 0.2494411f, 0.73723847f),
                new Quaternion(0.6235274f, -0.66380864f, -0.29373443f, -0.29033053f),
                new Quaternion(0.6780625f, -0.6592852f, -0.26568344f, -0.18704711f),
                new Quaternion(0.7367927f, -0.6347571f, -0.14393571f, -0.18303718f),
                new Quaternion(0.7584072f, -0.6393418f, -0.12667806f, -0.0036594148f),
            };
            pose.rightHand.inputSource = SteamVR_Input_Sources.RightHand;
            pose.rightHand.thumbFingerMovementType = SteamVR_Skeleton_FingerExtensionTypes.Static;
            pose.rightHand.indexFingerMovementType = SteamVR_Skeleton_FingerExtensionTypes.Static;
            pose.rightHand.middleFingerMovementType = SteamVR_Skeleton_FingerExtensionTypes.Static;
            pose.rightHand.ringFingerMovementType = SteamVR_Skeleton_FingerExtensionTypes.Static;
            pose.rightHand.pinkyFingerMovementType = SteamVR_Skeleton_FingerExtensionTypes.Static;
            pose.rightHand.ignoreRootPoseData = true;
            pose.rightHand.ignoreWristPoseData = true;
            pose.rightHand.position = new Vector3(-0.005119898f, -0.04334565f, -0.05492314f);
            pose.rightHand.rotation = new Quaternion(0.38216498f, -0.048071682f, 0.019911945f, 0.9226281f);
            pose.rightHand.bonePositions = new Vector3[31] {
                new Vector3(-0.0f, 0.0f, 0.0f),
                new Vector3(-0.0374f, 0.0301f, 0.1573f),
                new Vector3(-0.012083273f, 0.028070245f, 0.025049686f),
                new Vector3(0.040406488f, 7.8231096e-08f, 6.6682696e-07f),
                new Vector3(0.0325168f, -5.4875272e-08f, -1.2340024e-08f),
                new Vector3(0.030463902f, 1.6269207e-07f, 7.92839e-08f),
                new Vector3(0.0006324522f, 0.026866155f, 0.015001948f),
                new Vector3(0.074204385f, 0.005002201f, -0.00023377323f),
                new Vector3(0.043930072f, 0.0f, 0.0f),
                new Vector3(0.02869547f, -9.398158e-08f, -1.2649753e-07f),
                new Vector3(0.022821384f, -1.4365155e-07f, 7.651614e-08f),
                new Vector3(0.0021773134f, 0.007119544f, 0.016318738f),
                new Vector3(0.07095288f, -0.00077883265f, -0.000997186f),
                new Vector3(0.043108486f, -9.950596e-08f, -6.7041825e-09f),
                new Vector3(0.033266045f, -1.320567e-08f, -2.1670374e-08f),
                new Vector3(0.025892371f, 9.984198e-08f, -2.0352908e-09f),
                new Vector3(0.0005134356f, -0.0065451227f, 0.016347693f),
                new Vector3(0.06587581f, -0.0017857892f, -0.00069344096f),
                new Vector3(0.04069671f, -9.5347104e-08f, -2.2934731e-08f),
                new Vector3(0.028746964f, 1.0089892e-07f, 4.5306827e-08f),
                new Vector3(0.022430236f, 1.0846127e-07f, -1.7428562e-08f),
                new Vector3(-0.002478151f, -0.01898137f, 0.015213584f),
                new Vector3(0.0628784f, -0.0028440943f, -0.00033152848f),
                new Vector3(0.03021974f, -8.8708475e-08f, -5.098991e-08f),
                new Vector3(0.018186722f, -8.242205e-08f, -2.0070001e-07f),
                new Vector3(0.01801794f, -2.00012e-08f, 6.59746e-08f),
                new Vector3(-0.0060591106f, 0.05628522f, 0.060063843f),
                new Vector3(-0.04041555f, -0.043017667f, 0.019344581f),
                new Vector3(-0.03935372f, -0.07567404f, 0.047048334f),
                new Vector3(-0.038340144f, -0.09098663f, 0.08257892f),
                new Vector3(-0.031805996f, -0.08721431f, 0.12101539f),
            };
            pose.rightHand.boneRotations = new Quaternion[31] {
                new Quaternion(-6.123234e-17f, 1.0f, 6.123234e-17f, -4.371139e-08f),
                new Quaternion(-0.078608155f, -0.92027926f, 0.3792963f, -0.055146642f),
                new Quaternion(-0.26145115f, -0.7213955f, 0.41618466f, 0.48787525f),
                new Quaternion(0.11937635f, -0.056662083f, -0.26811036f, 0.9542828f),
                new Quaternion(-0.10534973f, 0.09216707f, -0.24559677f, 0.95921266f),
                new Quaternion(-1.3877788e-17f, -1.3877788e-17f, -5.551115e-17f, 1.0f),
                new Quaternion(-0.6442515f, -0.42213318f, -0.4782025f, 0.42197865f),
                new Quaternion(0.019549787f, 0.0116787795f, -0.596928f, 0.8019716f),
                new Quaternion(0.034517456f, 0.004169758f, -0.42240983f, 0.9057379f),
                new Quaternion(-0.025964372f, 0.002702249f, -0.6257829f, 0.7795604f),
                new Quaternion(6.938894e-18f, 1.9428903e-16f, -1.348151e-33f, 1.0f),
                new Quaternion(-0.5564024f, -0.47263962f, -0.43028674f, 0.5309252f),
                new Quaternion(-0.18400313f, -0.018806672f, -0.56584114f, 0.8035005f),
                new Quaternion(0.04917431f, 0.04468566f, -0.4810155f, 0.87419057f),
                new Quaternion(-0.02132691f, 0.0187856f, -0.6167448f, 0.78665f),
                new Quaternion(1.1639192e-17f, -5.602331e-17f, -0.040125635f, 0.9991947f),
                new Quaternion(-0.53544044f, -0.45080867f, -0.47522852f, 0.5331351f),
                new Quaternion(-0.12557462f, -0.013844049f, -0.5029163f, 0.85505235f),
                new Quaternion(-0.0019562317f, -0.0010904208f, -0.51140475f, 0.85933703f),
                new Quaternion(-0.0077420403f, 0.010083841f, -0.5958945f, 0.80296206f),
                new Quaternion(6.938894e-18f, -9.62965e-35f, -1.3877788e-17f, 1.0f),
                new Quaternion(-0.58007795f, -0.37509266f, -0.5312601f, 0.49048728f),
                new Quaternion(0.09443955f, -0.026952695f, -0.4141369f, 0.9049007f),
                new Quaternion(0.03410991f, -0.06371035f, -0.37493703f, 0.92422926f),
                new Quaternion(-0.024830999f, -0.106817275f, -0.37987843f, 0.9185128f),
                new Quaternion(0.0f, 0.0f, 1.9081958e-17f, 1.0f),
                new Quaternion(0.20274544f, 0.59426665f, 0.2494411f, 0.73723847f),
                new Quaternion(0.6235274f, -0.66380864f, -0.29373443f, -0.29033053f),
                new Quaternion(0.6780625f, -0.6592852f, -0.26568344f, -0.18704711f),
                new Quaternion(0.7367927f, -0.6347571f, -0.14393571f, -0.18303718f),
                new Quaternion(0.7584072f, -0.6393418f, -0.12667806f, -0.0036594148f),
            };
            return pose;
        }
    }
}
