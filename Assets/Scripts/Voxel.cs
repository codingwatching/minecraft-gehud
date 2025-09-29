using Unity.Mathematics;

namespace Voxilarium
{
    public struct Voxel
    {
        public const ushort Air = 0;
        public const ushort Stone = 1;

        public ushort Block;
        public Light Light;

        public Voxel(ushort block)
        {
            Block = block;
            Light = default;
        }

        public static readonly int3[] Sides = {
            new( 0,  0,  1),
            new( 0,  0, -1),
            new( 0,  1,  0),
            new( 0, -1,  0),
            new( 1,  0,  0),
            new(-1,  0,  0),
        };
    }
}
