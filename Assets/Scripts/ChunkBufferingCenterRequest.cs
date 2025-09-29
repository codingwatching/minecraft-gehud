using Unity.Entities;
using Unity.Mathematics;

namespace Voxilarium
{
    public struct ChunkBufferingCenterRequest : IComponentData
    {
        public int2 NewCenter;
    }
}
