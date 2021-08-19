using System;

namespace ImmersalRESTLocalizer.Types
{
    [Serializable]
    public struct ImmersalRequestParams
    {
        public MapId[] mapIds;
        public string b64;
        public float oy;
        public float ox;
        public string token;
        public float fx;
        public float fy;
    }
}