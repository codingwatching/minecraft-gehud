using Unity.Entities;
using Unity.Mathematics;

namespace Minecraft
{
    public struct LastLoadingColumn : IComponentData
    {
        public int2 Value;
    }
}
