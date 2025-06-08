using Unity.Collections;
using Unity.Entities;

namespace Minecraft
{
    public struct Chunk : IComponentData
    {
        public NativeArray<Voxel> Voxels;
    }
}
