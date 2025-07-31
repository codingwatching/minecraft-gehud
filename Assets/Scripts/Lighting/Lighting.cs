using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Voxilarium
{
    public struct Lighting : IComponentData, IDisposable
    {
        public const int ChanelCount = 4;

        public NativeArray<NativeQueue<LightEntry>> Add;
        public NativeArray<NativeQueue<LightEntry>> Remove;

        public Lighting(Allocator allocator)
        {
            Add = new NativeArray<NativeQueue<LightEntry>>(ChanelCount, allocator);
            for (int i = 0; i < Add.Length; i++)
            {
                Add[i] = new NativeQueue<LightEntry>(allocator);
            }

            Remove = new NativeArray<NativeQueue<LightEntry>>(ChanelCount, allocator);
            for (int i = 0; i < Remove.Length; i++)
            {
                Remove[i] = new NativeQueue<LightEntry>(allocator);
            }
        }

        public void AddLight(EntityManager entityManager, in Chunks chunks, in int3 coordinate, LightChanel chanel, byte level)
        {
            if (level <= 1)
            {
                return;
            }

            var chunkCoordinate = CoordinateUtility.ToChunk(coordinate);

            var chunkEntity = chunks.GetEntity(chunkCoordinate);

            if (chunkEntity == Entity.Null)
            {
                return;
            }

            var localCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, coordinate);
            var index = IndexUtility.CoordinateToIndex(localCoordinate, Chunk.Size, Chunk.Size);
            var voxels = entityManager.GetComponentData<Chunk>(chunkEntity).Voxels;
            var voxel = voxels[index];
            voxel.Light.Set(chanel, level);
            voxels[index] = voxel;

            entityManager.SetComponentEnabled<DirtyChunk>(chunkEntity, true);
            entityManager.SetComponentEnabled<ImmediateChunk>(chunkEntity, true);

            chunks.MarkDirtyIfNeededImmediate(entityManager, chunkCoordinate, localCoordinate);

            var entry = new LightEntry(coordinate, level);
            Add[(int)chanel].Enqueue(entry);
        }

        public void AddLight(EntityManager entityManager, in Chunks chunks, in int3 coordinate, LightChanel chanel)
        {
            var chunkCoordinate = CoordinateUtility.ToChunk(coordinate);
            var chunkEntity = chunks.GetEntity(chunkCoordinate);

            if (chunkEntity == Entity.Null)
            {
                return;
            }

            var localCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, coordinate);
            var index = IndexUtility.CoordinateToIndex(localCoordinate, Chunk.Size, Chunk.Size);
            var voxels = entityManager.GetComponentData<Chunk>(chunkEntity).Voxels;
            var level = voxels[index].Light.Get(chanel);

            if (level <= 1)
            {
                return;
            }

            var entry = new LightEntry(coordinate, level);
            Add[(int)chanel].Enqueue(entry);
        }

        public void RemoveLight(EntityManager entityManager, in Chunks chunks, in int3 coordinate, LightChanel chanel)
        {
            var chunkCoordinate = CoordinateUtility.ToChunk(coordinate);
            var entity = chunks.GetEntity(chunkCoordinate);

            if (entity == Entity.Null)
            {
                return;
            }

            var localCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, coordinate);
            var index = IndexUtility.CoordinateToIndex(localCoordinate, Chunk.Size, Chunk.Size);
            var voxels = entityManager.GetComponentData<Chunk>(entity).Voxels;
            var voxel = voxels[index];
            var level = voxel.Light.Get(chanel);

            if (level <= 1)
            {
                return;
            }

            voxel.Light.Set(chanel, Light.Min);
            voxels[index] = voxel;

            entityManager.SetComponentEnabled<DirtyChunk>(entity, true);
            entityManager.SetComponentEnabled<ImmediateChunk>(entity, true);

            chunks.MarkDirtyIfNeededImmediate(entityManager, chunkCoordinate, localCoordinate);

            var entry = new LightEntry(coordinate, level);
            Remove[(int)chanel].Enqueue(entry);
        }

        public void Calculate(EntityManager entityManager, in Chunks chunks, in Blocks blocks, LightChanel chanel)
        {
            while (Remove[(int)chanel].TryDequeue(out var entry))
            {
                for (var i = 0; i < Voxel.Sides.Length; i++)
                {
                    var voxelCoordinate = entry.Coordinate + Voxel.Sides[i];
                    var chunkCoordinate = CoordinateUtility.ToChunk(voxelCoordinate);
                    var chunkEntity = chunks.GetEntity(chunkCoordinate);

                    if (chunkEntity != Entity.Null)
                    {
                        var localVoxelCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, voxelCoordinate);
                        var index = IndexUtility.CoordinateToIndex(localVoxelCoordinate, Chunk.Size, Chunk.Size);
                        var voxels = entityManager.GetComponentData<Chunk>(chunkEntity).Voxels;
                        var voxel = voxels[index];
                        var level = voxel.Light.Get(chanel);
                        var block = voxels[index].Block;
                        var absorption = blocks.Items[(int)block].Absorption;

                        if (level != 0 && level == entry.Level - absorption - 1)
                        {
                            var removeEntry = new LightEntry(voxelCoordinate, level);
                            Remove[(int)chanel].Enqueue(removeEntry);
                            voxel.Light.Set(chanel, Light.Min);
                            voxels[index] = voxel;
                            entityManager.SetComponentEnabled<DirtyChunk>(chunkEntity, true);
                            entityManager.SetComponentEnabled<ImmediateChunk>(chunkEntity, true);
                            chunks.MarkDirtyIfNeededImmediate(entityManager, chunkCoordinate, localVoxelCoordinate);
                        }
                        else if (level >= entry.Level)
                        {
                            var addEntry = new LightEntry(voxelCoordinate, level);
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

                for (var i = 0; i < Voxel.Sides.Length; i++)
                {
                    var voxelCoordinate = entry.Coordinate + Voxel.Sides[i];
                    var chunkCoordinate = CoordinateUtility.ToChunk(voxelCoordinate);
                    var chunkEntity = chunks.GetEntity(chunkCoordinate);

                    if (chunkEntity != Entity.Null)
                    {
                        var localVoxelCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, voxelCoordinate);
                        var index = IndexUtility.CoordinateToIndex(localVoxelCoordinate, Chunk.Size, Chunk.Size);
                        var voxels = entityManager.GetComponentData<Chunk>(chunkEntity).Voxels;
                        var voxel = voxels[index];
                        var level = voxel.Light.Get(chanel);
                        var block = voxels[index].Block;
                        var absorption = blocks.Items[block].Absorption;

                        if (blocks.Items[block].IsTransparent && level + absorption + 1 < entry.Level)
                        {
                            var newLevel = (byte)(entry.Level - absorption - 1);
                            voxel.Light.Set(chanel, newLevel);
                            voxels[index] = voxel;
                            var addEntry = new LightEntry(voxelCoordinate, newLevel);
                            Add[(int)chanel].Enqueue(addEntry);
                            entityManager.SetComponentEnabled<DirtyChunk>(chunkEntity, true);
                            entityManager.SetComponentEnabled<ImmediateChunk>(chunkEntity, true);
                            chunks.MarkDirtyIfNeededImmediate(entityManager, chunkCoordinate, localVoxelCoordinate);
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
