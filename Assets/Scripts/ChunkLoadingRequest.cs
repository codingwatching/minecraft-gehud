using Unity.Entities;
using Unity.Mathematics;

namespace Voxilarium
{
    public struct ChunkLoadingRequest : IComponentData
    {
        public int2 NewCenter;
    }
}
