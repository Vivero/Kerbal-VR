using UnityEngine;
using Valve.VR;

namespace KerbalVR
{
    public class MathUtils
    {
        /// <summary>
        /// Converts a pose matrix provided by OpenVR to a set of Euler angles, in radians.
        /// </summary>
        /// <param name="poseMatrix">OpenVR pose matrix</param>
        /// <returns>Vector3 containing [pitch, yaw, roll] (in radians).</returns>
        public static Vector3 PoseMatrix2RotationEuler(ref HmdMatrix34_t poseMatrix) {
            Vector2 vector_r32_r33 = new Vector2(poseMatrix.m9, poseMatrix.m10);
            float eulerPitch = Mathf.Atan2(poseMatrix.m9, poseMatrix.m10);
            float eulerYaw = Mathf.Atan2(-poseMatrix.m8, vector_r32_r33.magnitude);
            float eulerRoll = Mathf.Atan2(poseMatrix.m4, poseMatrix.m0);
            return new Vector3(eulerPitch, eulerYaw, eulerRoll);
        }

        /// <summary>
        /// Converts a pose matrix provided by OpenVR to a rotation quaternion.
        /// </summary>
        /// <param name="poseMatrix">OpenVR pose matrix</param>
        /// <returns>Quaternion representing the pose matrix rotation.</returns>
        public static Quaternion PoseMatrix2RotationQuaternion(ref HmdMatrix34_t poseMatrix) {
            // Adapted from: http://www.euclideanspace.com/maths/geometry/rotations/conversions/matrixToQuaternion/index.htm
            Quaternion q = new Quaternion();
            q.w = Mathf.Sqrt(Mathf.Max(0, 1 + poseMatrix.m0 + poseMatrix.m5 + poseMatrix.m10)) / 2;
            q.x = Mathf.Sqrt(Mathf.Max(0, 1 + poseMatrix.m0 - poseMatrix.m5 - poseMatrix.m10)) / 2;
            q.y = Mathf.Sqrt(Mathf.Max(0, 1 - poseMatrix.m0 + poseMatrix.m5 - poseMatrix.m10)) / 2;
            q.z = Mathf.Sqrt(Mathf.Max(0, 1 - poseMatrix.m0 - poseMatrix.m5 + poseMatrix.m10)) / 2;
            q.x *= Mathf.Sign(q.x * (poseMatrix.m9 - poseMatrix.m6));
            q.y *= Mathf.Sign(q.y * (poseMatrix.m2 - poseMatrix.m8));
            q.z *= Mathf.Sign(q.z * (poseMatrix.m4 - poseMatrix.m1));
            return q;
        }

        /// <summary>
        /// Converts a pose matrix provided by OpenVR to a position vector.
        /// </summary>
        /// <param name="poseMatrix">OpenVR pose matrix</param>
        /// <returns>Vector3 with [x,y,z] position of pose matrix.</returns>
        public static Vector3 PoseMatrix2Position(ref HmdMatrix34_t poseMatrix) {
            return new Vector3(poseMatrix.m3, poseMatrix.m7, poseMatrix.m11);
        }

        /// <summary>
        /// Converts a pose matrix provided by OpenVR to a position vector and rotation vector ([pitch, yaw, roll] in radians)
        /// </summary>
        /// <param name="poseMatrix">OpenVR pose matrix</param>
        /// <param name="position">Output position vector</param>
        /// <param name="rotation">Output rotation vector [pitch, yaw, roll] in radians</param>
        public static void PoseMatrix2PositionAndRotation(ref HmdMatrix34_t poseMatrix, ref Vector3 position, ref Vector3 rotation) {
            // return position
            position = PoseMatrix2Position(ref poseMatrix);

            // return rotation
            rotation = PoseMatrix2RotationEuler(ref poseMatrix);
        }

        /// <summary>
        /// Converts a pose matrix provided by OpenVR to a position vector and rotation quaternion
        /// </summary>
        /// <param name="poseMatrix">OpenVR pose matrix</param>
        /// <param name="position">Output position vector</param>
        /// <param name="rotation">Output rotation quaternion</param>
        public static void PoseMatrix2PositionAndRotation(ref HmdMatrix34_t poseMatrix, ref Vector3 position, ref Quaternion rotation) {
            // return position
            position = PoseMatrix2Position(ref poseMatrix);

            // return rotation
            rotation = PoseMatrix2RotationQuaternion(ref poseMatrix);
        }

        public static Matrix4x4 Matrix4x4_OpenVr2UnityFormat(ref HmdMatrix44_t mat44_openvr) {
            Matrix4x4 mat44_unity = Matrix4x4.identity;
            mat44_unity.m00 = mat44_openvr.m0;
            mat44_unity.m01 = mat44_openvr.m1;
            mat44_unity.m02 = mat44_openvr.m2;
            mat44_unity.m03 = mat44_openvr.m3;
            mat44_unity.m10 = mat44_openvr.m4;
            mat44_unity.m11 = mat44_openvr.m5;
            mat44_unity.m12 = mat44_openvr.m6;
            mat44_unity.m13 = mat44_openvr.m7;
            mat44_unity.m20 = mat44_openvr.m8;
            mat44_unity.m21 = mat44_openvr.m9;
            mat44_unity.m22 = mat44_openvr.m10;
            mat44_unity.m23 = mat44_openvr.m11;
            mat44_unity.m30 = mat44_openvr.m12;
            mat44_unity.m31 = mat44_openvr.m13;
            mat44_unity.m32 = mat44_openvr.m14;
            mat44_unity.m33 = mat44_openvr.m15;
            return mat44_unity;
        }
    }
}
