using Unity.Entities;
using Unity.Mathematics;

namespace Minecraft
{
    public struct ChunkBufferingCenterRequest : IComponentData
    {
        public int2 NewCenter;
    }
}
