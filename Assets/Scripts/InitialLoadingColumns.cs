using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Voxilarium
{
    public struct InitialLoadingColumns : IComponentData
    {
        public NativeList<int2> Columns;
    }
}
