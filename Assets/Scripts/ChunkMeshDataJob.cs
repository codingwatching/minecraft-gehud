using Minecraft.Utilities;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Minecraft
{
    public struct ChunkMeshDataJob : IJob
    {
        [ReadOnly]
        public Chunk Chunk;
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
            var voxel = Chunk[localVoxelCoordinate];
            var x = localVoxelCoordinate.x;
            var y = localVoxelCoordinate.y;
            var z = localVoxelCoordinate.z;

            if (voxel.Block == Voxel.Air.Block)
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
                Data.Vertices.Add(new Vertex(x + 1, y + 0, z + 0, u, v, r, g, b, s));
                Data.Vertices.Add(new Vertex(x + 1, y + 1, z + 0, u, v, r, g, b, s));
                Data.Vertices.Add(new Vertex(x + 1, y + 1, z + 1, u, v, r, g, b, s));
                Data.Vertices.Add(new Vertex(x + 1, y + 0, z + 1, u, v, r, g, b, s));
            }

            // Left face.
            if (HasFace(localVoxelCoordinate + new int3(-1, 0, 0)))
            {
                AddFaceIndices();
                Data.Vertices.Add(new Vertex(x + 0, y + 0, z + 1, u, v, r, g, b, s));
                Data.Vertices.Add(new Vertex(x + 0, y + 1, z + 1, u, v, r, g, b, s));
                Data.Vertices.Add(new Vertex(x + 0, y + 1, z + 0, u, v, r, g, b, s));
                Data.Vertices.Add(new Vertex(x + 0, y + 0, z + 0, u, v, r, g, b, s));
            }

            // Top face.
            if (HasFace(localVoxelCoordinate + new int3(0, 1, 0)))
            {
                AddFaceIndices();
                Data.Vertices.Add(new Vertex(x + 0, y + 1, z + 0, u, v, r, g, b, s));
                Data.Vertices.Add(new Vertex(x + 0, y + 1, z + 1, u, v, r, g, b, s));
                Data.Vertices.Add(new Vertex(x + 1, y + 1, z + 1, u, v, r, g, b, s));
                Data.Vertices.Add(new Vertex(x + 1, y + 1, z + 0, u, v, r, g, b, s));
            }

            // Bottom face.
            if (HasFace(localVoxelCoordinate + new int3(0, -1, 0)))
            {
                AddFaceIndices();
                Data.Vertices.Add(new Vertex(x + 1, y + 0, z + 0, u, v, r, g, b, s));
                Data.Vertices.Add(new Vertex(x + 1, y + 0, z + 1, u, v, r, g, b, s));
                Data.Vertices.Add(new Vertex(x + 0, y + 0, z + 1, u, v, r, g, b, s));
                Data.Vertices.Add(new Vertex(x + 0, y + 0, z + 0, u, v, r, g, b, s));
            }

            // Front face.
            if (HasFace(localVoxelCoordinate + new int3(0, 0, 1)))
            {
                AddFaceIndices();
                Data.Vertices.Add(new Vertex(x + 1, y + 0, z + 1, u, v, r, g, b, s));
                Data.Vertices.Add(new Vertex(x + 1, y + 1, z + 1, u, v, r, g, b, s));
                Data.Vertices.Add(new Vertex(x + 0, y + 1, z + 1, u, v, r, g, b, s));
                Data.Vertices.Add(new Vertex(x + 0, y + 0, z + 1, u, v, r, g, b, s));
            }

            // Back face.
            if (HasFace(localVoxelCoordinate + new int3(0, 0, -1)))
            {
                AddFaceIndices();
                Data.Vertices.Add(new Vertex(x + 0, y + 0, z + 0, u, v, r, g, b, s));
                Data.Vertices.Add(new Vertex(x + 0, y + 1, z + 0, u, v, r, g, b, s));
                Data.Vertices.Add(new Vertex(x + 1, y + 1, z + 0, u, v, r, g, b, s));
                Data.Vertices.Add(new Vertex(x + 1, y + 0, z + 0, u, v, r, g, b, s));
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

        private void GetVoxel(in int3 localVoxelCoordinate, out Voxel voxel)
        {
            var voxelCoordinate = Chunk.Coordinate * Chunk.Size + localVoxelCoordinate;
            var sideChunkCoordinate = CoordinateUtility.ToChunk(voxelCoordinate);

            if (sideChunkCoordinate.x != Chunk.Coordinate.x || sideChunkCoordinate.y != Chunk.Coordinate.y || sideChunkCoordinate.z != Chunk.Coordinate.z)
            {
                voxel = Voxel.Air;
                return;
            }

            voxel = Chunk[localVoxelCoordinate];

            //var sideLocalVoxelCoordinate = voxelCoordinate - sideChunkCoordinate * Chunk.Size;

            //sideChunkCoordinate -= Chunk.Coordinate;
            //sideChunkCoordinate += new int3(1, 1, 1);
            //var clasterIndex = IndexUtility.CoordinateToIndex(sideChunkCoordinate, 3, 3);
            //var voxels = Claster[clasterIndex];
            //if (!voxels.IsCreated)
            //{
            //    voxel = default;
            //    return;
            //}

            //var sideLocalVoxelIndex = IndexUtility.CoordinateToIndex(sideLocalVoxelCoordinate, Chunk.Size, Chunk.Size, Chunk.Area);
            //voxel = voxels[sideLocalVoxelIndex];
        }

        private bool HasFace(in int3 localVoxelCoordinate)
        {
            GetVoxel(localVoxelCoordinate, out var voxel);
            return voxel.Block == Voxel.Air.Block;
        }
    }
}
