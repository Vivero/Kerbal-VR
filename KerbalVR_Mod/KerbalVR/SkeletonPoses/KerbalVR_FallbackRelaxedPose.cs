/* ===============================================
 *   This is an auto-generated file for KerbalVR. 
 *   Do not edit by hand.                         
 * ===============================================
 */

using UnityEngine;
using Valve.VR;

namespace KerbalVR {
    public class SkeletonPose_FallbackRelaxedPose {
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
            pose.leftHand.position = new Vector3(0.0f, 0.0f, 0.0f);
            pose.leftHand.rotation = new Quaternion(0.0f, -0.0f, -0.0f, -1.0f);
            pose.leftHand.bonePositions = new Vector3[31] {
                new Vector3(-0.0f, 0.0f, 0.0f),
                new Vector3(-0.034037687f, 0.03650266f, 0.16472164f),
                new Vector3(-0.012083233f, 0.028070247f, 0.025049694f),
                new Vector3(0.040405963f, -5.1561553e-08f, 4.5447194e-08f),
                new Vector3(0.032516792f, -5.1137583e-08f, -1.2933195e-08f),
                new Vector3(0.030463902f, 1.6269207e-07f, 7.92839e-08f),
                new Vector3(0.0006324522f, 0.026866155f, 0.015001948f),
                new Vector3(0.074204385f, 0.005002201f, -0.00023377323f),
                new Vector3(0.043930072f, 5.9567498e-08f, 1.8367103e-07f),
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
                new Vector3(0.0628784f, -0.0028440945f, -0.0003315112f),
                new Vector3(0.030219711f, -3.418319e-08f, -9.332872e-08f),
                new Vector3(0.018186597f, -5.0220166e-09f, -2.0934549e-07f),
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
                new Quaternion(-0.24104308f, -0.76422274f, 0.45859465f, 0.38412613f),
                new Quaternion(0.085189685f, 5.13494e-05f, -0.28143752f, 0.95579064f),
                new Quaternion(0.0052029183f, -0.021480577f, -0.15888694f, 0.9870494f),
                new Quaternion(-1.3877788e-17f, -1.3877788e-17f, -5.551115e-17f, 1.0f),
                new Quaternion(-0.6442515f, -0.42213318f, -0.4782025f, 0.42197865f),
                new Quaternion(0.08568421f, 0.023565516f, -0.19161178f, 0.9774394f),
                new Quaternion(0.045650285f, 0.0043684426f, -0.095879465f, 0.99433607f),
                new Quaternion(-0.0020507684f, 0.022764975f, -0.15681197f, 0.987364f),
                new Quaternion(6.938894e-18f, 1.9428903e-16f, -1.348151e-33f, 1.0f),
                new Quaternion(-0.546723f, -0.46074906f, -0.44252017f, 0.54127645f),
                new Quaternion(-0.17867392f, 0.047816366f, -0.24333772f, 0.9521429f),
                new Quaternion(0.020366715f, -0.010060345f, -0.21893612f, 0.9754748f),
                new Quaternion(-0.010457605f, 0.026426358f, -0.19179714f, 0.981023f),
                new Quaternion(1.1639192e-17f, -5.602331e-17f, -0.040125635f, 0.9991947f),
                new Quaternion(-0.5166922f, -0.4298879f, -0.49554786f, 0.5501435f),
                new Quaternion(-0.17289871f, 0.114340894f, -0.29726714f, 0.93202174f),
                new Quaternion(-0.0021954547f, -0.000443071f, -0.22544385f, 0.9742536f),
                new Quaternion(-0.00472193f, 0.011803731f, -0.35618067f, 0.93433064f),
                new Quaternion(6.938894e-18f, -9.62965e-35f, -1.3877788e-17f, 1.0f),
                new Quaternion(-0.5269183f, -0.32674035f, -0.5840246f, 0.52394f),
                new Quaternion(-0.2006022f, 0.15258452f, -0.36497858f, 0.8962519f),
                new Quaternion(0.0018557907f, 0.0004098564f, -0.25201905f, 0.96772045f),
                new Quaternion(-0.019474672f, 0.048342716f, -0.26703015f, 0.9622778f),
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
            pose.rightHand.position = new Vector3(0.0f, 0.0f, 0.0f);
            pose.rightHand.rotation = new Quaternion(-0.0f, -0.0f, -0.0f, 1.0f);
            pose.rightHand.bonePositions = new Vector3[31] {
                new Vector3(-0.0f, 0.0f, 0.0f),
                new Vector3(-0.034037687f, 0.03650266f, 0.16472164f),
                new Vector3(-0.012083233f, 0.028070247f, 0.025049694f),
                new Vector3(0.040405963f, -5.1561553e-08f, 4.5447194e-08f),
                new Vector3(0.032516792f, -5.1137583e-08f, -1.2933195e-08f),
                new Vector3(0.030463902f, 1.6269207e-07f, 7.92839e-08f),
                new Vector3(0.0006324522f, 0.026866155f, 0.015001948f),
                new Vector3(0.074204385f, 0.005002201f, -0.00023377323f),
                new Vector3(0.043930072f, 5.9567498e-08f, 1.8367103e-07f),
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
                new Vector3(0.0628784f, -0.0028440945f, -0.0003315112f),
                new Vector3(0.030219711f, -3.418319e-08f, -9.332872e-08f),
                new Vector3(0.018186597f, -5.0220166e-09f, -2.0934549e-07f),
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
                new Quaternion(-0.24104308f, -0.76422274f, 0.45859465f, 0.38412613f),
                new Quaternion(0.085189685f, 5.13494e-05f, -0.28143752f, 0.95579064f),
                new Quaternion(0.005202918f, -0.021480575f, -0.15888692f, 0.98704934f),
                new Quaternion(-1.3877788e-17f, -1.3877788e-17f, -5.551115e-17f, 1.0f),
                new Quaternion(-0.6442515f, -0.42213318f, -0.4782025f, 0.42197865f),
                new Quaternion(0.08568421f, 0.023565516f, -0.19161178f, 0.9774394f),
                new Quaternion(0.045650285f, 0.0043684426f, -0.095879465f, 0.99433607f),
                new Quaternion(-0.0020507684f, 0.022764975f, -0.15681197f, 0.987364f),
                new Quaternion(6.938894e-18f, 1.9428903e-16f, -1.348151e-33f, 1.0f),
                new Quaternion(-0.546723f, -0.46074906f, -0.44252017f, 0.54127645f),
                new Quaternion(-0.17867392f, 0.047816366f, -0.24333772f, 0.9521429f),
                new Quaternion(0.020366715f, -0.010060345f, -0.21893612f, 0.9754748f),
                new Quaternion(-0.010457605f, 0.026426358f, -0.19179714f, 0.981023f),
                new Quaternion(1.1639192e-17f, -5.602331e-17f, -0.040125635f, 0.9991947f),
                new Quaternion(-0.5166922f, -0.4298879f, -0.49554786f, 0.5501435f),
                new Quaternion(-0.1728987f, 0.11434089f, -0.2972671f, 0.9320217f),
                new Quaternion(-0.0021954547f, -0.000443071f, -0.22544385f, 0.9742536f),
                new Quaternion(-0.00472193f, 0.011803731f, -0.35618067f, 0.93433064f),
                new Quaternion(6.938894e-18f, -9.62965e-35f, -1.3877788e-17f, 1.0f),
                new Quaternion(-0.5269183f, -0.32674035f, -0.5840246f, 0.52394f),
                new Quaternion(-0.2006022f, 0.15258452f, -0.36497858f, 0.8962519f),
                new Quaternion(0.0018557906f, 0.00040985638f, -0.25201902f, 0.9677204f),
                new Quaternion(-0.019474672f, 0.048342716f, -0.26703015f, 0.9622778f),
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
