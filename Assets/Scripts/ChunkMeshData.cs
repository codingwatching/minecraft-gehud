using System;
using Unity.Collections;
using Unity.Entities;

namespace Voxilarium
{
    public struct ChunkMeshData : IComponentData, IDisposable
    {
        public NativeList<Vertex> Vertices;
        public NativeList<ushort> OpaqueIndices;
        public NativeList<ushort> TransparentIndices;

        public void Dispose()
        {
            Vertices.Dispose();
            OpaqueIndices.Dispose();
            TransparentIndices.Dispose();
        }
    }
}
