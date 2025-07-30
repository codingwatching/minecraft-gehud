using System;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Voxilarium
{
    public struct LightingQueues : IComponentData, IDisposable
    {
        public NativeArray<NativeQueue<LightingEntry>> Add;
        public NativeArray<NativeQueue<LightingEntry>> Remove;

        public void AddLight(EntityManager entityManager, in ChunkBuffer buffer, in int3 coordinate, LightChanel chanel, byte level)
        {
            if (level <= 1)
            {
                return;
            }

            var chunkCoordinate = CoordinateUtility.ToChunk(coordinate);

            var chunkEntity = buffer.GetEntity(chunkCoordinate);

            if (chunkEntity == Entity.Null)
            {
                return;
            }

            var localCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, coordinate);
            var index = IndexUtility.CoordinateToIndex(localCoordinate, Chunk.Size, Chunk.Size);
            var voxels = entityManager.GetComponentData<Chunk>(chunkEntity).Voxels;
            var voxel = voxels[index];
            voxel.Lighting.Set(chanel, level);
            voxels[index] = voxel;

            entityManager.SetComponentEnabled<DirtyChunk>(chunkEntity, true);
            entityManager.SetComponentEnabled<ImmediateChunk>(chunkEntity, true);

            buffer.MarkDirtyIfNeededImmediate(entityManager, chunkCoordinate, localCoordinate);

            var entry = new LightingEntry(coordinate, level);
            Add[(int)chanel].Enqueue(entry);
        }

        public void AddLight(EntityManager entityManager, in ChunkBuffer buffer, in int3 coordinate, LightChanel chanel)
        {
            var chunkCoordinate = CoordinateUtility.ToChunk(coordinate);
            var chunkEntity = buffer.GetEntity(chunkCoordinate);

            if (chunkEntity == Entity.Null)
            {
                return;
            }

            var localCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, coordinate);
            var index = IndexUtility.CoordinateToIndex(localCoordinate, Chunk.Size, Chunk.Size);
            var voxels = entityManager.GetComponentData<Chunk>(chunkEntity).Voxels;
            var level = voxels[index].Lighting.Get(chanel);

            if (level <= 1)
            {
                return;
            }

            var entry = new LightingEntry(coordinate, level);
            Add[(int)chanel].Enqueue(entry);
        }

        public void RemoveLight(EntityManager entityManager, in ChunkBuffer buffer, in int3 coordinate, LightChanel chanel)
        {
            var chunkCoordinate = CoordinateUtility.ToChunk(coordinate);
            var entity = buffer.GetEntity(chunkCoordinate);

            if (entity == Entity.Null)
            {
                return;
            }

            var localCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, coordinate);
            var index = IndexUtility.CoordinateToIndex(localCoordinate, Chunk.Size, Chunk.Size);
            var voxels = entityManager.GetComponentData<Chunk>(entity).Voxels;
            var voxel = voxels[index];
            var level = voxel.Lighting.Get(chanel);

            if (level <= 1)
            {
                return;
            }

            voxel.Lighting.Set(chanel, Lighting.Min);
            voxels[index] = voxel;

            entityManager.SetComponentEnabled<DirtyChunk>(entity, true);
            entityManager.SetComponentEnabled<ImmediateChunk>(entity, true);

            buffer.MarkDirtyIfNeededImmediate(entityManager, chunkCoordinate, localCoordinate);

            var entry = new LightingEntry(coordinate, level);
            Remove[(int)chanel].Enqueue(entry);
        }

        private static readonly int3[] blockSides =
        {
            new(0, 0, 1),
            new(0, 0, -1),
            new(0, 1, 0),
            new(0, -1, 0),
            new(1, 0, 0),
            new(-1, 0, 0),
        };

        public void Calculate(EntityManager entityManager, in Blocks blocks, in ChunkBuffer buffer, LightChanel chanel)
        {
            while (Remove[(int)chanel].TryDequeue(out var entry))
            {
                for (var i = 0; i < blockSides.Length; i++)
                {
                    var voxelCoordinate = entry.Coordinate + blockSides[i];
                    var chunkCoordinate = CoordinateUtility.ToChunk(voxelCoordinate);
                    var chunkEntity = buffer.GetEntity(chunkCoordinate);

                    if (chunkEntity != Entity.Null)
                    {
                        var localVoxelCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, voxelCoordinate);
                        var index = IndexUtility.CoordinateToIndex(localVoxelCoordinate, Chunk.Size, Chunk.Size);
                        var voxels = entityManager.GetComponentData<Chunk>(chunkEntity).Voxels;
                        var voxel = voxels[index];
                        var level = voxel.Lighting.Get(chanel);
                        var block = voxels[index].Block;
                        var absorption = blocks.Items[(int)block].Absorption;

                        if (level != 0 && level == entry.Level - absorption - 1)
                        {
                            var removeEntry = new LightingEntry(voxelCoordinate, level);
                            Remove[(int)chanel].Enqueue(removeEntry);
                            voxel.Lighting.Set(chanel, Lighting.Min);
                            voxels[index] = voxel;
                            entityManager.SetComponentEnabled<DirtyChunk>(chunkEntity, true);
                            entityManager.SetComponentEnabled<ImmediateChunk>(chunkEntity, true);
                            buffer.MarkDirtyIfNeededImmediate(entityManager, chunkCoordinate, localVoxelCoordinate);
                        }
                        else if (level >= entry.Level)
                        {
                            var addEntry = new LightingEntry(voxelCoordinate, level);
                            Add[(int)chanel].Enqueue(addEntry);
                        }
                    }
                }
            }

            while (Add[(int)chanel].TryDequeue(out var entry))
            {
                if (entry.Level <= 1)
                {
                    continue;
                }

                for (var i = 0; i < blockSides.Length; i++)
                {
                    var voxelCoordinate = entry.Coordinate + blockSides[i];
                    var chunkCoordinate = CoordinateUtility.ToChunk(voxelCoordinate);
                    var chunkEntity = buffer.GetEntity(chunkCoordinate);

                    if (chunkEntity != Entity.Null)
                    {
                        var localVoxelCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, voxelCoordinate);
                        var index = IndexUtility.CoordinateToIndex(localVoxelCoordinate, Chunk.Size, Chunk.Size);
                        var voxels = entityManager.GetComponentData<Chunk>(chunkEntity).Voxels;
                        var voxel = voxels[index];
                        var level = voxel.Lighting.Get(chanel);
                        var block = voxels[index].Block;
                        var absorption = blocks.Items[block].Absorption;

                        if (blocks.Items[block].IsTransparent && level + absorption + 1 < entry.Level)
                        {
                            var newLevel = (byte)(entry.Level - absorption - 1);
                            voxel.Lighting.Set(chanel, newLevel);
                            voxels[index] = voxel;
                            var addEntry = new LightingEntry(voxelCoordinate, newLevel);
                            Add[(int)chanel].Enqueue(addEntry);
                            entityManager.SetComponentEnabled<DirtyChunk>(chunkEntity, true);
                            entityManager.SetComponentEnabled<ImmediateChunk>(chunkEntity, true);
                            buffer.MarkDirtyIfNeededImmediate(entityManager, chunkCoordinate, localVoxelCoordinate);
                        }
                    }
                }
            }
        }

        public void Dispose()
        {
            foreach (var queue in Add)
            {
                queue.Dispose();
            }

            Add.Dispose();

            foreach (var queue in Remove)
            {
                queue.Dispose();
            }

            Remove.Dispose();
        }
    }
}
