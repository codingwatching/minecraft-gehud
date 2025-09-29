using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Mathematics;

namespace Voxilarium
{
    public struct Blocks : IComponentData, IDisposable
    {
        public NativeArray<Block> Items;

        public Block this[ushort id] => Items[id];

        public void Dispose()
        {
            Items.Dispose();
        }
    }
}
