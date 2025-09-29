using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Voxilarium
{
    public struct Chunks : IComponentData, IDisposable
    {
        public const int BufferDistance = 1;

        public int2 Center;
        public int Height;
        public int Distance;
        public int Size;

        public NativeArray<Entity> Entities;
        public NativeArray<Entity> Buffer;

        public int ToIndex(in int3 coordinate)
        {
            var arrayCoordinate = new int3
            {
                x = coordinate.x - Center.x + Distance + BufferDistance,
                y = coordinate.y,
                z = coordinate.z - Center.y + Distance + BufferDistance
            };

            return IndexUtility.CoordinateToIndex(arrayCoordinate, Size, Height);
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

            var index = IndexUtility.CoordinateToIndex(arrayCoordinate, Size, Height);
            return Entities[index];
        }

        public bool TryGetEntity(in int3 coordinate, out Entity entity)
        {
            entity = GetEntity(coordinate);
            return entity != Entity.Null;
        }

        public void SpawnChunk(EntityCommandBuffer commandBuffer, in int3 coordinate, bool isVisible = true)
        {
            if (GetEntity(coordinate) != Entity.Null)
            {
                throw new Exception("Chunk allready exists.");
            }

            ChunkUtility.SpawnChunk(commandBuffer, coordinate, isVisible);
        }

        public void UpdateDistance(int newDistance)
        {
            var oldSize = Size;
            var oldChunks = Entities;
            Distance = newDistance;
            Size = Distance * 2 + 1 + BufferDistance * 2;
            var chunksVolume = Size * Size * Height;

            Entities = new NativeArray<Entity>(chunksVolume, Allocator.Persistent);

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
                        var index = IndexUtility.CoordinateToIndex(x, y, z, oldSize, Height);

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

                        Entities[IndexUtility.CoordinateToIndex(newX, y, newZ, Size, Height)] = chunk;
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
                        var index = IndexUtility.CoordinateToIndex(x, y, z, Size, Height);

                        var chunk = Entities[index];
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

                        var newIndex = IndexUtility.CoordinateToIndex(newX, y, newZ, Size, Height);
                        Buffer[newIndex] = chunk;
                    }
                }
            }

            (Buffer, Entities) = (Entities, Buffer);

            Center = newCenter;
        }

        public Voxel GetVoxel(EntityManager entityManager, in int3 coordinate)
        {
            var chunkCoordinate = CoordinateUtility.ToChunk(coordinate);

            var entity = GetEntity(chunkCoordinate);

            if (entity == Entity.Null
                || entityManager.IsComponentEnabled<ThreadedChunk>(entity)
                || !entityManager.HasComponent<Chunk>(entity)
                || entityManager.HasComponent<NotGeneratedChunk>(entity))
            {
                return default;
            }

            var localCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, coordinate);
            var index = IndexUtility.CoordinateToIndex(localCoordinate, Chunk.Size, Chunk.Size);
            return entityManager.GetComponentData<Chunk>(entity).Voxels[index];
        }

        public void DestroyVoxel(EntityManager entityManager, in Blocks blocks, in Lighting lighting, in int3 coordinate)
        {
            var chunkCoordinate = CoordinateUtility.ToChunk(coordinate);

            var chunkEntity = GetEntity(chunkCoordinate);

            if (chunkEntity == Entity.Null
                || entityManager.IsComponentEnabled<ThreadedChunk>(chunkEntity)
                || !entityManager.HasComponent<Chunk>(chunkEntity)
                || entityManager.HasComponent<NotGeneratedChunk>(chunkEntity))
            {
                return;
            }

            var localVoxelCoordinate = coordinate - chunkCoordinate * Chunk.Size;

            var chunk = entityManager.GetComponentData<Chunk>(chunkEntity);
            var index = IndexUtility.CoordinateToIndex(localVoxelCoordinate, Chunk.Size, Chunk.Size);
            var voxel = chunk.Voxels[index];
            voxel.Block = Voxel.Air;
            chunk.Voxels[index] = voxel;
            entityManager.SetComponentEnabled<DirtyChunk>(chunkEntity, true);
            entityManager.SetComponentEnabled<ImmediateChunk>(chunkEntity, true);
            MarkDirtyIfNeededImmediate(entityManager, chunkCoordinate, localVoxelCoordinate);

            lighting.RemoveLight(entityManager, this, coordinate, LightChanel.Red);
            lighting.RemoveLight(entityManager, this, coordinate, LightChanel.Green);
            lighting.RemoveLight(entityManager, this, coordinate, LightChanel.Blue);
            lighting.Calculate(entityManager, this, blocks, LightChanel.Red);
            lighting.Calculate(entityManager, this, blocks, LightChanel.Green);
            lighting.Calculate(entityManager, this, blocks, LightChanel.Blue);

            var topVoxel = GetVoxel(entityManager, coordinate + new int3(0, 1, 0));

            if (topVoxel.Block == Voxel.Air && topVoxel.Light.Sun == Light.Max)
            {
                for (var y = coordinate.y; y >= 0; y--)
                {
                    var bottomVoxelCoordinate = new int3(coordinate.x, y, coordinate.z);
                    var bottomVoxel = GetVoxel(entityManager, bottomVoxelCoordinate);
                    if (bottomVoxel.Block != Voxel.Air)
                    {
                        break;
                    }

                    lighting.AddLight(entityManager, this, bottomVoxelCoordinate, LightChanel.Sun, Light.Max);
                }
            }

            lighting.AddLight(entityManager, this, coordinate + new int3(1, 0, 0), LightChanel.Red);
            lighting.AddLight(entityManager, this, coordinate + new int3(-1, 0, 0), LightChanel.Red);
            lighting.AddLight(entityManager, this, coordinate + new int3(0, 1, 0), LightChanel.Red);
            lighting.AddLight(entityManager, this, coordinate + new int3(0, -1, 0), LightChanel.Red);
            lighting.AddLight(entityManager, this, coordinate + new int3(0, 0, 1), LightChanel.Red);
            lighting.AddLight(entityManager, this, coordinate + new int3(0, 0, -1), LightChanel.Red);
            lighting.Calculate(entityManager, this, blocks, LightChanel.Red);

            lighting.AddLight(entityManager, this, coordinate + new int3(1, 0, 0), LightChanel.Green);
            lighting.AddLight(entityManager, this, coordinate + new int3(-1, 0, 0), LightChanel.Green);
            lighting.AddLight(entityManager, this, coordinate + new int3(0, 1, 0), LightChanel.Green);
            lighting.AddLight(entityManager, this, coordinate + new int3(0, -1, 0), LightChanel.Green);
            lighting.AddLight(entityManager, this, coordinate + new int3(0, 0, 1), LightChanel.Green);
            lighting.AddLight(entityManager, this, coordinate + new int3(0, 0, -1), LightChanel.Green);
            lighting.Calculate(entityManager, this, blocks, LightChanel.Green);

            lighting.AddLight(entityManager, this, coordinate + new int3(1, 0, 0), LightChanel.Blue);
            lighting.AddLight(entityManager, this, coordinate + new int3(-1, 0, 0), LightChanel.Blue);
            lighting.AddLight(entityManager, this, coordinate + new int3(0, 1, 0), LightChanel.Blue);
            lighting.AddLight(entityManager, this, coordinate + new int3(0, -1, 0), LightChanel.Blue);
            lighting.AddLight(entityManager, this, coordinate + new int3(0, 0, 1), LightChanel.Blue);
            lighting.AddLight(entityManager, this, coordinate + new int3(0, 0, -1), LightChanel.Blue);
            lighting.Calculate(entityManager, this, blocks, LightChanel.Blue);

            lighting.AddLight(entityManager, this, coordinate + new int3(1, 0, 0), LightChanel.Sun);
            lighting.AddLight(entityManager, this, coordinate + new int3(-1, 0, 0), LightChanel.Sun);
            lighting.AddLight(entityManager, this, coordinate + new int3(0, 1, 0), LightChanel.Sun);
            lighting.AddLight(entityManager, this, coordinate + new int3(0, -1, 0), LightChanel.Sun);
            lighting.AddLight(entityManager, this, coordinate + new int3(0, 0, 1), LightChanel.Sun);
            lighting.AddLight(entityManager, this, coordinate + new int3(0, 0, -1), LightChanel.Sun);
            lighting.Calculate(entityManager, this, blocks, LightChanel.Sun);
        }

        public void PlaceVoxel(EntityManager entityManager, in Blocks blocks, in Lighting lighting, in int3 coordinate, ushort blockId)
        {
            var chunkCoordinate = CoordinateUtility.ToChunk(coordinate);

            var entity = GetEntity(chunkCoordinate);

            if (entity == Entity.Null
                || entityManager.IsComponentEnabled<ThreadedChunk>(entity)
                || !entityManager.HasComponent<Chunk>(entity)
                || entityManager.HasComponent<NotGeneratedChunk>(entity))
            {
                return;
            }

            var localVoxelCoordinate = coordinate - chunkCoordinate * Chunk.Size;

            var chunk = entityManager.GetComponentData<Chunk>(entity);
            var index = IndexUtility.CoordinateToIndex(localVoxelCoordinate, Chunk.Size, Chunk.Size);
            var voxel = chunk.Voxels[index];
            voxel.Block = blockId;
            chunk.Voxels[index] = voxel;
            entityManager.SetComponentEnabled<DirtyChunk>(entity, true);
            entityManager.SetComponentEnabled<ImmediateChunk>(entity, true);
            MarkDirtyIfNeededImmediate(entityManager, chunkCoordinate, localVoxelCoordinate);

            lighting.RemoveLight(entityManager, this, coordinate, LightChanel.Red);
            lighting.RemoveLight(entityManager, this, coordinate, LightChanel.Green);
            lighting.RemoveLight(entityManager, this, coordinate, LightChanel.Blue);
            lighting.RemoveLight(entityManager, this, coordinate, LightChanel.Sun);

            for (int y = coordinate.y - 1; y >= 0; y--)
            {
                var bottomVoxelCoordinate = new int3(coordinate.x, y, coordinate.z);
                var bottomVoxel = GetVoxel(entityManager, bottomVoxelCoordinate);
                if (!blocks.Items[(int)bottomVoxel.Block].IsTransparent)
                {
                    break;
                }

                lighting.RemoveLight(entityManager, this, bottomVoxelCoordinate, LightChanel.Sun);
            }

            lighting.Calculate(entityManager, this, blocks, LightChanel.Red);
            lighting.Calculate(entityManager, this, blocks, LightChanel.Green);
            lighting.Calculate(entityManager, this, blocks, LightChanel.Blue);
            lighting.Calculate(entityManager, this, blocks, LightChanel.Sun);

            var emission = blocks.Items[blockId].Emission;

            if (emission.Red != 0)
            {
                lighting.AddLight(entityManager, this, coordinate, LightChanel.Red, emission.Red);
                lighting.Calculate(entityManager, this, blocks, LightChanel.Red);
            }

            if (emission.Green != 0)
            {
                lighting.AddLight(entityManager, this, coordinate, LightChanel.Green, emission.Green);
                lighting.Calculate(entityManager, this, blocks, LightChanel.Green);
            }

            if (emission.Blue != 0)
            {
                lighting.AddLight(entityManager, this, coordinate, LightChanel.Blue, emission.Blue);
                lighting.Calculate(entityManager, this, blocks, LightChanel.Blue);
            }
        }

        public void MarkDirtyIfExistsImmediate(EntityManager entityManager, in int3 chunkCoordinate)
        {
            var entity = GetEntity(chunkCoordinate);
            if (entity == Entity.Null || !entityManager.HasComponent<Chunk>(entity))
            {
                return;
            }

            entityManager.SetComponentEnabled<DirtyChunk>(entity, true);
            entityManager.SetComponentEnabled<ImmediateChunk>(entity, true);
        }

        public void MarkDirtyIfExists(EntityManager entityManager, in int3 chunkCoordinate)
        {
            var entity = GetEntity(chunkCoordinate);
            if (entity == Entity.Null || !entityManager.HasComponent<Chunk>(entity))
            {
                return;
            }

            entityManager.SetComponentEnabled<DirtyChunk>(entity, true);
        }

        public void MarkDirtyIfExists(EntityManager entityManager, in EntityCommandBuffer commandBuffer, in int3 chunkCoordinate)
        {
            var entity = GetEntity(chunkCoordinate);
            if (entity == Entity.Null || !entityManager.HasComponent<Chunk>(entity) || entityManager.IsComponentEnabled<DirtyChunk>(entity))
            {
                return;
            }

            commandBuffer.SetComponentEnabled<DirtyChunk>(entity, true);
        }

        public void MarkDirtyIfNeededImmediate(EntityManager entityManager, in int3 chunkCoordinate, in int3 localVoxelCoordinate)
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

        public void MarkDirtyIfNeeded(EntityManager entityManager, in int3 chunkCoordinate, in int3 localVoxelCoordinate)
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

        public void MarkDirtyIfNeeded(EntityManager entityManager, in EntityCommandBuffer commandBuffer, in int3 chunkCoordinate, in int3 localVoxelCoordinate)
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
            Entities.Dispose();
            Buffer.Dispose();
        }
    }
}
