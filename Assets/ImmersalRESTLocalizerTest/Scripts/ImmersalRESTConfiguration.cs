﻿using UnityEngine;

namespace ImmersalRESTLocalizerTest
{
    [CreateAssetMenu(fileName = "ImmersalConfig", menuName = "Immersal REST Localizer/ConfigurationScriptableObject", order = 0)]
    public class ImmersalRESTConfiguration : ScriptableObject
    {
        [Header("Immersal Developer Token")]
        public string token;
    }
}