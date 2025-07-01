using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;

namespace Minecraft
{
    [BurstCompile]
    public struct ChunkMeshJob : IJob
    {
        [ReadOnly]
        public ChunkMeshData Data;
        [ReadOnly]
        public NativeArray<VertexAttributeDescriptor> Descriptors;

        public Mesh.MeshDataArray MeshDataArray;

        void IJob.Execute()
        {
            var mesh = MeshDataArray[0];

            mesh.SetVertexBufferParams(Data.Vertices.Length, Descriptors);

            var vertices = mesh.GetVertexData<Vertex>();
            for (int i = 0; i < Data.Vertices.Length; i++)
            {
                vertices[i] = Data.Vertices[i];
            }

            var opaqueIndicesCount = Data.OpaqueIndices.Length;
            var transparentIndicesCount = Data.TransparentIndices.Length;
            var indicesCount = opaqueIndicesCount + transparentIndicesCount;
            mesh.SetIndexBufferParams(indicesCount, IndexFormat.UInt16);
            var indices = mesh.GetIndexData<ushort>();

            for (int i = 0; i < opaqueIndicesCount; i++)
            {
                indices[i] = Data.OpaqueIndices[i];
            }

            for (int i = 0; i < transparentIndicesCount; i++)
            {
                indices[i + opaqueIndicesCount] = Data.TransparentIndices[i];
            }

            mesh.subMeshCount = 2;
            mesh.SetSubMesh(0, new SubMeshDescriptor(0, opaqueIndicesCount), ChunkMeshSystem.UpdateFlags);
            mesh.SetSubMesh(1, new SubMeshDescriptor(opaqueIndicesCount, transparentIndicesCount), ChunkMeshSystem.UpdateFlags);
        }
    }
}
