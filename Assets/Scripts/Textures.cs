using System;
using Unity.Mathematics;

namespace Voxilarium
{
    [Serializable]
    public struct Textures
    {
        public uint2 Right;
        public uint2 Left;
        public uint2 Top;
        public uint2 Bottom;
        public uint2 Front;
        public uint2 Back;
    }
}
