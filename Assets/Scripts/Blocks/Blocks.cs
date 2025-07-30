using System;
using Unity.Collections;
using Unity.Entities;

namespace Voxilarium
{
    public struct Blocks : IComponentData, IDisposable
    {
        public NativeArray<Block> Items;

        public void Dispose()
        {
            Items.Dispose();
        }
    }
}
