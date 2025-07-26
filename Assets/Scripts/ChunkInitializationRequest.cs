using Unity.Entities;
using Unity.Mathematics;

namespace Voxilarium
{
    public struct ChunkInitializationRequest : IComponentData
    {
        public bool IsVisible;
        public int3 Coordinate;
    }
}
