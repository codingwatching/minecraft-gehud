using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Voxilarium.Utilities;
using static Voxilarium.Utilities.IndexUtility;

namespace Voxilarium
{
    public struct ChunkBuffer : IComponentData, IDisposable
    {
        public const int BufferDistance = 1;

        public int2 Center;
        public int Height;
        public int Distance;
        public int Size;

        public NativeArray<Entity> Chunks;
        public NativeArray<Entity> Buffer;

        public int ToIndex(in int3 coordinate)
        {
            var arrayCoordinate = new int3
            {
                x = coordinate.x - Center.x + Distance + BufferDistance,
                y = coordinate.y,
                z = coordinate.z - Center.y + Distance + BufferDistance
            };

            return CoordinateToIndex(arrayCoordinate, Size, Height);
        }

        public bool IsOutOfBuffer(in int3 arrayCoordinate)
        {
            return arrayCoordinate.x < 0
                || arrayCoordinate.y < 0
                || arrayCoordinate.z < 0
                || arrayCoordinate.x >= Size
                || arrayCoordinate.y >= Height
                || arrayCoordinate.z >= Size;
        }

        public Entity GetEntity(in int3 coordinate)
        {
            var arrayCoordinate = new int3
            {
                x = coordinate.x - Center.x + Distance + BufferDistance,
                y = coordinate.y,
                z = coordinate.z - Center.y + Distance + BufferDistance
            };

            if (IsOutOfBuffer(arrayCoordinate))
            {
                return Entity.Null;
            }

            var index = CoordinateToIndex(arrayCoordinate, Size, Height);
            return Chunks[index];
        }

        public bool TryGetEntity(in int3 coordinate, out Entity entity)
        {
            entity = GetEntity(coordinate);
            return entity != Entity.Null;
        }

        public Entity SpawnChunk(EntityManager entityManager, in int3 coordinate, bool isVisible = true)
        {
            if (GetEntity(coordinate) != Entity.Null)
            {
                throw new Exception("Chunk allready exists.");
            }

            var entity = ChunkUtility.SpawnChunk(entityManager, coordinate, isVisible);
            var index = ToIndex(coordinate);
            Chunks[index] = entity;

            return entity;
        }

        public void UpdateDistance(int newDistance)
        {
            var oldSize = Size;
            var oldChunks = Chunks;
            Distance = newDistance;
            Size = Distance * 2 + 1 + BufferDistance * 2;
            var chunksVolume = Size * Size * Height;

            Chunks = new NativeArray<Entity>(chunksVolume, Allocator.Persistent);

            if (Buffer.IsCreated)
            {
                Buffer.Dispose();
            }

            Buffer = new NativeArray<Entity>(chunksVolume, Allocator.Persistent);

            var sideDelta = oldSize - Size;
            for (var x = 0; x < oldSize; x++)
            {
                for (var z = 0; z < oldSize; z++)
                {
                    for (var y = 0; y < Height; y++)
                    {
                        var index = CoordinateToIndex(x, y, z, oldSize, Height);

                        var chunk = oldChunks[index];
                        if (chunk == Entity.Null)
                        {
                            continue;
                        }

                        var newX = x - sideDelta / 2;
                        var newZ = z - sideDelta / 2;
                        if (newX < 0 || newZ < 0 || newX >= Size || newZ >= Size)
                        {
                            continue;
                        }

                        Chunks[CoordinateToIndex(newX, y, newZ, Size, Height)] = chunk;
                    }
                }
            }

            if (oldChunks.IsCreated)
            {
                oldChunks.Dispose();
            }
        }

        public void UpdateCenter(in int2 newCenter, in EntityCommandBuffer commandBuffer)
        {
            for (var i = 0; i < Buffer.Length; i++)
            {
                Buffer[i] = Entity.Null;
            }

            var centerDelta = newCenter - Center;
            for (var x = 0; x < Size; x++)
            {
                for (var z = 0; z < Size; z++)
                {
                    for (var y = 0; y < Height; y++)
                    {
                        var index = CoordinateToIndex(x, y, z, Size, Height);

                        var chunk = Chunks[index];
                        if (chunk == Entity.Null)
                        {
                            continue;
                        }

                        var newX = x - centerDelta.x;
                        var newZ = z - centerDelta.y;
                        if (newX < 0 || newZ < 0 || newX >= Size || newZ >= Size)
                        {
                            commandBuffer.DestroyEntity(chunk);
                            continue;
                        }

                        var newIndex = CoordinateToIndex(newX, y, newZ, Size, Height);
                        Buffer[newIndex] = chunk;
                    }
                }
            }

            (Buffer, Chunks) = (Chunks, Buffer);

            Center = newCenter;
        }

        public void GetVoxel(in EntityManager entityManager, in int3 coordinate, out Voxel voxel)
        {
            var chunkCoordinate = new int3
            {
                x = (int)math.floor(coordinate.x / (float)Chunk.Size),
                y = (int)math.floor(coordinate.y / (float)Chunk.Size),
                z = (int)math.floor(coordinate.z / (float)Chunk.Size)
            };

            var entity = GetEntity(chunkCoordinate);
            if (entity == Entity.Null
                || !entityManager.HasComponent<Chunk>(entity)
                //|| entityManager.IsComponentEnabled<ThreadedChunk>(entity)
                || entityManager.HasComponent<RawChunk>(entity))
            {
                voxel = default;
                return;
            }

            var localCoordinate = coordinate - chunkCoordinate * Chunk.Size;
            var index = CoordinateToIndex(localCoordinate, Chunk.Size, Chunk.Size);
            voxel = entityManager.GetComponentData<Chunk>(entity).Voxels[index];
        }

        public void MarkDirtyIfExistsImmediate(in EntityManager entityManager, in int3 chunkCoordinate)
        {
            var entity = GetEntity(chunkCoordinate);
            if (entity == Entity.Null || !entityManager.HasComponent<Chunk>(entity))
            {
                return;
            }

            entityManager.SetComponentEnabled<DirtyChunk>(entity, true);
            entityManager.SetComponentEnabled<ImmediateChunk>(entity, true);
        }

        public void MarkDirtyIfExistsImmediate(in EntityManager entityManager, in EntityCommandBuffer commandBuffer, in int3 chunkCoordinate)
        {
            var entity = GetEntity(chunkCoordinate);
            if (entity == Entity.Null || !entityManager.HasComponent<Chunk>(entity))
            {
                return;
            }

            commandBuffer.SetComponentEnabled<DirtyChunk>(entity, true);
            commandBuffer.SetComponentEnabled<ImmediateChunk>(entity, true);
        }

        public void MarkDirtyIfExists(in EntityManager entityManager, in int3 chunkCoordinate)
        {
            var entity = GetEntity(chunkCoordinate);
            if (entity == Entity.Null || !entityManager.HasComponent<Chunk>(entity))
            {
                return;
            }

            entityManager.SetComponentEnabled<DirtyChunk>(entity, true);
        }

        public void MarkDirtyIfExists(in EntityManager entityManager, in EntityCommandBuffer commandBuffer, in int3 chunkCoordinate)
        {
            var entity = GetEntity(chunkCoordinate);
            if (entity == Entity.Null || !entityManager.HasComponent<Chunk>(entity) || entityManager.IsComponentEnabled<DirtyChunk>(entity))
            {
                return;
            }

            commandBuffer.SetComponentEnabled<DirtyChunk>(entity, true);
        }

        public void MarkDirtyIfNeededImmediate(in EntityManager entityManager, in int3 chunkCoordinate, in int3 localVoxelCoordinate)
        {
            if (localVoxelCoordinate.x == 0)
            {
                MarkDirtyIfExistsImmediate(entityManager, chunkCoordinate + new int3(-1, 0, 0));
            }

            if (localVoxelCoordinate.y == 0)
            {
                MarkDirtyIfExistsImmediate(entityManager, chunkCoordinate + new int3(0, -1, 0));
            }

            if (localVoxelCoordinate.z == 0)
            {
                MarkDirtyIfExistsImmediate(entityManager, chunkCoordinate + new int3(0, 0, -1));
            }

            if (localVoxelCoordinate.x == Chunk.Size - 1)
            {
                MarkDirtyIfExistsImmediate(entityManager, chunkCoordinate + new int3(1, 0, 0));
            }

            if (localVoxelCoordinate.y == Chunk.Size - 1)
            {
                MarkDirtyIfExistsImmediate(entityManager, chunkCoordinate + new int3(0, 1, 0));
            }

            if (localVoxelCoordinate.z == Chunk.Size - 1)
            {
                MarkDirtyIfExistsImmediate(entityManager, chunkCoordinate + new int3(0, 0, 1));
            }
        }

        public void MarkDirtyIfNeededImmediate(in EntityManager entityManager, in EntityCommandBuffer commandBuffer, in int3 chunkCoordinate, in int3 localVoxelCoordinate)
        {
            if (localVoxelCoordinate.x == 0)
            {
                MarkDirtyIfExistsImmediate(entityManager, commandBuffer, chunkCoordinate + new int3(-1, 0, 0));
            }

            if (localVoxelCoordinate.y == 0)
            {
                MarkDirtyIfExistsImmediate(entityManager, commandBuffer, chunkCoordinate + new int3(0, -1, 0));
            }

            if (localVoxelCoordinate.z == 0)
            {
                MarkDirtyIfExistsImmediate(entityManager, commandBuffer, chunkCoordinate + new int3(0, 0, -1));
            }

            if (localVoxelCoordinate.x == Chunk.Size - 1)
            {
                MarkDirtyIfExistsImmediate(entityManager, commandBuffer, chunkCoordinate + new int3(1, 0, 0));
            }

            if (localVoxelCoordinate.y == Chunk.Size - 1)
            {
                MarkDirtyIfExistsImmediate(entityManager, commandBuffer, chunkCoordinate + new int3(0, 1, 0));
            }

            if (localVoxelCoordinate.z == Chunk.Size - 1)
            {
                MarkDirtyIfExistsImmediate(entityManager, commandBuffer, chunkCoordinate + new int3(0, 0, 1));
            }
        }

        public void MarkDirtyIfNeeded(in EntityManager entityManager, in int3 chunkCoordinate, in int3 localVoxelCoordinate)
        {
            if (localVoxelCoordinate.x == 0)
            {
                MarkDirtyIfExists(entityManager, chunkCoordinate + new int3(-1, 0, 0));
            }

            if (localVoxelCoordinate.y == 0)
            {
                MarkDirtyIfExists(entityManager, chunkCoordinate + new int3(0, -1, 0));
            }

            if (localVoxelCoordinate.z == 0)
            {
                MarkDirtyIfExists(entityManager, chunkCoordinate + new int3(0, 0, -1));
            }

            if (localVoxelCoordinate.x == Chunk.Size - 1)
            {
                MarkDirtyIfExists(entityManager, chunkCoordinate + new int3(1, 0, 0));
            }

            if (localVoxelCoordinate.y == Chunk.Size - 1)
            {
                MarkDirtyIfExists(entityManager, chunkCoordinate + new int3(0, 1, 0));
            }

            if (localVoxelCoordinate.z == Chunk.Size - 1)
            {
                MarkDirtyIfExists(entityManager, chunkCoordinate + new int3(0, 0, 1));
            }
        }

        public void MarkDirtyIfNeeded(in EntityManager entityManager, in EntityCommandBuffer commandBuffer, in int3 chunkCoordinate, in int3 localVoxelCoordinate)
        {
            if (localVoxelCoordinate.x == 0)
            {
                MarkDirtyIfExists(entityManager, commandBuffer, chunkCoordinate + new int3(-1, 0, 0));
            }

            if (localVoxelCoordinate.y == 0)
            {
                MarkDirtyIfExists(entityManager, commandBuffer, chunkCoordinate + new int3(0, -1, 0));
            }

            if (localVoxelCoordinate.z == 0)
            {
                MarkDirtyIfExists(entityManager, commandBuffer, chunkCoordinate + new int3(0, 0, -1));
            }

            if (localVoxelCoordinate.x == Chunk.Size - 1)
            {
                MarkDirtyIfExists(entityManager, commandBuffer, chunkCoordinate + new int3(1, 0, 0));
            }

            if (localVoxelCoordinate.y == Chunk.Size - 1)
            {
                MarkDirtyIfExists(entityManager, commandBuffer, chunkCoordinate + new int3(0, 1, 0));
            }

            if (localVoxelCoordinate.z == Chunk.Size - 1)
            {
                MarkDirtyIfExists(entityManager, commandBuffer, chunkCoordinate + new int3(0, 0, 1));
            }
        }

        public void Dispose()
        {
            Chunks.Dispose();
            Buffer.Dispose();
        }
    }
}
