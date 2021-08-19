using System;

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
    }

    [Serializable]
    public struct ImmersalResponseError
    {
        public string error;
    }
}