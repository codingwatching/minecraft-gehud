using Unity.Entities;
using Unity.Mathematics;

namespace Minecraft
{
    public struct ChunkSpawnRequest : IComponentData
    {
        public bool IsVisible;
        public int3 Coordinate;
    }
}
