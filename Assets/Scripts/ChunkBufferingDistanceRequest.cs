using Unity.Entities;

namespace Voxilarium
{
    public struct ChunkBufferingDistanceRequest : IComponentData
    {
        public int NewDistance;
    }
}
