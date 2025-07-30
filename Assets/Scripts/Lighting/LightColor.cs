using System;
using UnityEngine;

namespace Voxilarium
{
    [Serializable]
    public struct LightColor
    {
        [Range(Lighting.Min, Lighting.Max)]
        public byte Red;
        [Range(Lighting.Min, Lighting.Max)]
        public byte Green;
        [Range(Lighting.Min, Lighting.Max)]
        public byte Blue;
    }
}
