using Unity.Entities;
using Unity.Mathematics;

namespace Voxilarium
{
    public struct LightCalculationRequest : IComponentData
    {
        public int2 Column;
    }
}
