using System;
using Unity.Collections;
using Unity.Entities;

namespace Voxilarium
{
    public struct Blocks : IComponentData, IDisposable
    {
        public NativeArray<Block> Value;

        public void Dispose()
        {
            Value.Dispose();
        }
    }
}
