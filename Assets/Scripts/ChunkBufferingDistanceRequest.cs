using Unity.Entities;

namespace Minecraft
{
    public struct ChunkBufferingDistanceRequest : IComponentData
    {
        public int NewDistance;
    }
}
