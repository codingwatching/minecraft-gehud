using Unity.Entities;
using Unity.Mathematics;

namespace Voxilarium
{
    public struct LastLoadingColumn : IComponentData
    {
        public int2 Value;
    }
}
