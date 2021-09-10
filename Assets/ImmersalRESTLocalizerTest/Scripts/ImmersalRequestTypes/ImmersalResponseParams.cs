using System;
using UnityEngine;

namespace ImmersalRESTLocalizer.Types
{
    [Serializable]
    public struct ImmersalResponseParams
    {
        public string error;
        public bool success;
        public int map;
        public float px;
        public float py;
        public float pz;
        public float r00;
        public float r01;
        public float r02;
        public float r10;
        public float r11;
        public float r12;
        public float r20;
        public float r21;
        public float r22;

        public Matrix4x4 ToMatrix4()
        {
            var mat = new Matrix4x4
            {
                m00 = r00,
                m01 = r01,
                m02 = r02,
                m10 = r10,
                m11 = r11,
                m12 = r12,
                m20 = r20,
                m21 = r21,
                m22 = r22,

                m03 = px,
                m13 = py,
                m23 = pz,

                m33 = 1
            };

            return mat;
        }
    }

    [Serializable]
    public struct ImmersalResponseError
    {
        public string error;
    }
}