using Unity.Entities;
using Unity.Mathematics;

namespace Voxilarium
{
    public struct IlluminationRequest : IComponentData
    {
        public int2 Column;
    }
}
