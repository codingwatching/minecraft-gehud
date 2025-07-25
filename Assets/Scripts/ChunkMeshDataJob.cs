using Minecraft.Utilities;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Minecraft
{
    [BurstCompile]
    public struct ChunkMeshDataJob : IJob, IDisposable
    {
        [ReadOnly]
        public int3 Coordinate;
        [ReadOnly]
        public NativeArray<Voxel> Claster;
        [ReadOnly]
        public NativeArray<Entity> ClasterEntities;
        [WriteOnly]
        public ChunkMeshData Data;

        private ushort vertexCount;

        public void Execute()
        {
            for (var x = 0; x < Chunk.Size; x++)
            {
                for (var y = 0; y < Chunk.Size; y++)
                {
                    for (var z = 0; z < Chunk.Size; z++)
                    {
                        ProcessVoxel(new int3(x, y, z));
                    }
                }
            }
        }

        private void ProcessVoxel(int3 localVoxelCoordinate)
        {
            var voxel = GetVoxel(localVoxelCoordinate);
            var x = localVoxelCoordinate.x;
            var y = localVoxelCoordinate.y;
            var z = localVoxelCoordinate.z;

            if (voxel.Block == Voxel.Air)
            {
                return;
            }

            var u = 0;
            var v = 0;
            var r = 0;
            var g = 0;
            var b = 0;
            var s = 0;

            // Right face.
            if (HasFace(localVoxelCoordinate + new int3(1, 0, 0)))
            {
                AddFaceIndices();
                Data.Vertices.Add(new Vertex(x + 1, y + 0, z + 0, u, v, Vertex.Normal.Right, r, g, b, s));
                Data.Vertices.Add(new Vertex(x + 1, y + 1, z + 0, u, v, Vertex.Normal.Right, r, g, b, s));
                Data.Vertices.Add(new Vertex(x + 1, y + 1, z + 1, u, v, Vertex.Normal.Right, r, g, b, s));
                Data.Vertices.Add(new Vertex(x + 1, y + 0, z + 1, u, v, Vertex.Normal.Right, r, g, b, s));
            }

            // Left face.
            if (HasFace(localVoxelCoordinate + new int3(-1, 0, 0)))
            {
                AddFaceIndices();
                Data.Vertices.Add(new Vertex(x + 0, y + 0, z + 1, u, v, Vertex.Normal.Left, r, g, b, s));
                Data.Vertices.Add(new Vertex(x + 0, y + 1, z + 1, u, v, Vertex.Normal.Left, r, g, b, s));
                Data.Vertices.Add(new Vertex(x + 0, y + 1, z + 0, u, v, Vertex.Normal.Left, r, g, b, s));
                Data.Vertices.Add(new Vertex(x + 0, y + 0, z + 0, u, v, Vertex.Normal.Left, r, g, b, s));
            }

            // Top face.
            if (HasFace(localVoxelCoordinate + new int3(0, 1, 0)))
            {
                AddFaceIndices();
                Data.Vertices.Add(new Vertex(x + 0, y + 1, z + 0, u, v, Vertex.Normal.Top, r, g, b, s));
                Data.Vertices.Add(new Vertex(x + 0, y + 1, z + 1, u, v, Vertex.Normal.Top, r, g, b, s));
                Data.Vertices.Add(new Vertex(x + 1, y + 1, z + 1, u, v, Vertex.Normal.Top, r, g, b, s));
                Data.Vertices.Add(new Vertex(x + 1, y + 1, z + 0, u, v, Vertex.Normal.Top, r, g, b, s));
            }

            // Bottom face.
            if (HasFace(localVoxelCoordinate + new int3(0, -1, 0)))
            {
                AddFaceIndices();
                Data.Vertices.Add(new Vertex(x + 1, y + 0, z + 0, u, v, Vertex.Normal.Bottom, r, g, b, s));
                Data.Vertices.Add(new Vertex(x + 1, y + 0, z + 1, u, v, Vertex.Normal.Bottom, r, g, b, s));
                Data.Vertices.Add(new Vertex(x + 0, y + 0, z + 1, u, v, Vertex.Normal.Bottom, r, g, b, s));
                Data.Vertices.Add(new Vertex(x + 0, y + 0, z + 0, u, v, Vertex.Normal.Bottom, r, g, b, s));
            }

            // Front face.
            if (HasFace(localVoxelCoordinate + new int3(0, 0, 1)))
            {
                AddFaceIndices();
                Data.Vertices.Add(new Vertex(x + 1, y + 0, z + 1, u, v, Vertex.Normal.Front, r, g, b, s));
                Data.Vertices.Add(new Vertex(x + 1, y + 1, z + 1, u, v, Vertex.Normal.Front, r, g, b, s));
                Data.Vertices.Add(new Vertex(x + 0, y + 1, z + 1, u, v, Vertex.Normal.Front, r, g, b, s));
                Data.Vertices.Add(new Vertex(x + 0, y + 0, z + 1, u, v, Vertex.Normal.Front, r, g, b, s));
            }

            // Back face.
            if (HasFace(localVoxelCoordinate + new int3(0, 0, -1)))
            {
                AddFaceIndices();
                Data.Vertices.Add(new Vertex(x + 0, y + 0, z + 0, u, v, Vertex.Normal.Back, r, g, b, s));
                Data.Vertices.Add(new Vertex(x + 0, y + 1, z + 0, u, v, Vertex.Normal.Back, r, g, b, s));
                Data.Vertices.Add(new Vertex(x + 1, y + 1, z + 0, u, v, Vertex.Normal.Back, r, g, b, s));
                Data.Vertices.Add(new Vertex(x + 1, y + 0, z + 0, u, v, Vertex.Normal.Back, r, g, b, s));
            }
        }

        private void AddFaceIndices()
        {
            Data.OpaqueIndices.Add((ushort)(vertexCount + 0));
            Data.OpaqueIndices.Add((ushort)(vertexCount + 1));
            Data.OpaqueIndices.Add((ushort)(vertexCount + 2));
            Data.OpaqueIndices.Add((ushort)(vertexCount + 0));
            Data.OpaqueIndices.Add((ushort)(vertexCount + 2));
            Data.OpaqueIndices.Add((ushort)(vertexCount + 3));
            vertexCount += 4;
        }

        private Voxel GetVoxel(in int3 localVoxelCoordinate)
        {
            var voxelCoordinate = Coordinate * Chunk.Size + localVoxelCoordinate;
            var sideChunkCoordinate = CoordinateUtility.ToChunk(voxelCoordinate);

            var sideLocalVoxelCoordinate = voxelCoordinate - sideChunkCoordinate * Chunk.Size;

            var clasterChunkCoordinate = sideChunkCoordinate - Coordinate + new int3(1, 1, 1);
            var clasterIndex = IndexUtility.CoordinateToIndex(clasterChunkCoordinate, 3, 3);

            var sideLocalVoxelIndex = IndexUtility.CoordinateToIndex(sideLocalVoxelCoordinate, Chunk.Size, Chunk.Size);
            return Claster[clasterIndex * Chunk.Volume + sideLocalVoxelIndex];
        }

        private bool HasFace(in int3 localVoxelCoordinate)
        {
            return GetVoxel(localVoxelCoordinate).Block == Voxel.Air;
        }

        public void Dispose()
        {
            Claster.Dispose();
            ClasterEntities.Dispose();
        }
    }
}
