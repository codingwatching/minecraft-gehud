using Voxilarium.Utilities;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Voxilarium
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
        [ReadOnly]
        public Blocks Blocks;

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
            var x = (uint)localVoxelCoordinate.x;
            var y = (uint)localVoxelCoordinate.y;
            var z = (uint)localVoxelCoordinate.z;

            if (voxel.Block == Voxel.Air)
            {
                return;
            }

            var block = Blocks.Value[voxel.Block];

            var r = 0u;
            var g = 0u;
            var b = 0u;
            var s = 0u;

            // Right face.
            if (HasFace(localVoxelCoordinate + new int3(1, 0, 0)))
            {
                var u = block.Textures.Right.x;
                var v = block.Textures.Right.y;

                AddFaceIndices();
                Data.Vertices.Add(new Vertex(x + 1, y + 0, z + 0, u + 0, v + 0, Vertex.Normal.Right, r, g, b, s));
                Data.Vertices.Add(new Vertex(x + 1, y + 1, z + 0, u + 0, v + 1, Vertex.Normal.Right, r, g, b, s));
                Data.Vertices.Add(new Vertex(x + 1, y + 1, z + 1, u + 1, v + 1, Vertex.Normal.Right, r, g, b, s));
                Data.Vertices.Add(new Vertex(x + 1, y + 0, z + 1, u + 1, v + 0, Vertex.Normal.Right, r, g, b, s));
            }

            // Left face.
            if (HasFace(localVoxelCoordinate + new int3(-1, 0, 0)))
            {
                var u = block.Textures.Left.x;
                var v = block.Textures.Left.y;

                AddFaceIndices();
                Data.Vertices.Add(new Vertex(x + 0, y + 0, z + 1, u + 0, v + 0, Vertex.Normal.Left, r, g, b, s));
                Data.Vertices.Add(new Vertex(x + 0, y + 1, z + 1, u + 0, v + 1, Vertex.Normal.Left, r, g, b, s));
                Data.Vertices.Add(new Vertex(x + 0, y + 1, z + 0, u + 1, v + 1, Vertex.Normal.Left, r, g, b, s));
                Data.Vertices.Add(new Vertex(x + 0, y + 0, z + 0, u + 1, v + 0, Vertex.Normal.Left, r, g, b, s));
            }

            // Top face.
            if (HasFace(localVoxelCoordinate + new int3(0, 1, 0)))
            {
                var u = block.Textures.Top.x;
                var v = block.Textures.Top.y;

                AddFaceIndices();
                Data.Vertices.Add(new Vertex(x + 0, y + 1, z + 0, u + 0, v + 0, Vertex.Normal.Top, r, g, b, s));
                Data.Vertices.Add(new Vertex(x + 0, y + 1, z + 1, u + 0, v + 1, Vertex.Normal.Top, r, g, b, s));
                Data.Vertices.Add(new Vertex(x + 1, y + 1, z + 1, u + 1, v + 1, Vertex.Normal.Top, r, g, b, s));
                Data.Vertices.Add(new Vertex(x + 1, y + 1, z + 0, u + 1, v + 0, Vertex.Normal.Top, r, g, b, s));
            }

            // Bottom face.
            if (HasFace(localVoxelCoordinate + new int3(0, -1, 0)))
            {
                var u = block.Textures.Bottom.x;
                var v = block.Textures.Bottom.y;

                AddFaceIndices();
                Data.Vertices.Add(new Vertex(x + 1, y + 0, z + 0, u + 0, v + 0, Vertex.Normal.Bottom, r, g, b, s));
                Data.Vertices.Add(new Vertex(x + 1, y + 0, z + 1, u + 0, v + 1, Vertex.Normal.Bottom, r, g, b, s));
                Data.Vertices.Add(new Vertex(x + 0, y + 0, z + 1, u + 1, v + 1, Vertex.Normal.Bottom, r, g, b, s));
                Data.Vertices.Add(new Vertex(x + 0, y + 0, z + 0, u + 1, v + 0, Vertex.Normal.Bottom, r, g, b, s));
            }

            // Front face.
            if (HasFace(localVoxelCoordinate + new int3(0, 0, 1)))
            {
                var u = block.Textures.Front.x;
                var v = block.Textures.Front.y;

                AddFaceIndices();
                Data.Vertices.Add(new Vertex(x + 1, y + 0, z + 1, u + 0, v + 0, Vertex.Normal.Front, r, g, b, s));
                Data.Vertices.Add(new Vertex(x + 1, y + 1, z + 1, u + 0, v + 1, Vertex.Normal.Front, r, g, b, s));
                Data.Vertices.Add(new Vertex(x + 0, y + 1, z + 1, u + 1, v + 1, Vertex.Normal.Front, r, g, b, s));
                Data.Vertices.Add(new Vertex(x + 0, y + 0, z + 1, u + 1, v + 0, Vertex.Normal.Front, r, g, b, s));
            }

            // Back face.
            if (HasFace(localVoxelCoordinate + new int3(0, 0, -1)))
            {
                var u = block.Textures.Back.x;
                var v = block.Textures.Back.y;

                AddFaceIndices();
                Data.Vertices.Add(new Vertex(x + 0, y + 0, z + 0, u + 0, v + 0, Vertex.Normal.Back, r, g, b, s));
                Data.Vertices.Add(new Vertex(x + 0, y + 1, z + 0, u + 0, v + 1, Vertex.Normal.Back, r, g, b, s));
                Data.Vertices.Add(new Vertex(x + 1, y + 1, z + 0, u + 1, v + 1, Vertex.Normal.Back, r, g, b, s));
                Data.Vertices.Add(new Vertex(x + 1, y + 0, z + 0, u + 1, v + 0, Vertex.Normal.Back, r, g, b, s));
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
