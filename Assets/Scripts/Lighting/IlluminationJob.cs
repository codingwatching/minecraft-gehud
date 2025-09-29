using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Voxilarium
{
    [BurstCompile]
    public unsafe struct IlluminationJob : IJob, IDisposable
    {
        [ReadOnly]
        public Blocks Blocks;
        [ReadOnly]
        public int2 Column;
        [ReadOnly]
        public int Height;
        [ReadOnly]
        public NativeArray<Entity> ClasterEntities;
        public NativeArray<IntPtr> Claster;

        public NativeQueue<LightEntry> AddQueues;
        public NativeQueue<LightEntry> RemoveQueues;

        public void Execute()
        {
            var startX = Column.x * Chunk.Size;
            var endX = Column.x * Chunk.Size + Chunk.Size;
            var startZ = Column.y * Chunk.Size;
            var endZ = Column.y * Chunk.Size + Chunk.Size;

            for (var x = startX; x < endX; x++)
            {
                for (var z = startZ; z < endZ; z++)
                {
                    for (var y = Height * Chunk.Size - 1; y >= 0; y--)
                    {
                        var voxelCoordinate = new int3(x, y, z);
                        var chunkCoordinate = CoordinateUtility.ToChunk(voxelCoordinate);

                        if (!TryGetVoxels(chunkCoordinate, out var voxels))
                        {
                            continue;
                        }

                        var localVoxelCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, voxelCoordinate);
                        var index = IndexUtility.CoordinateToIndex(localVoxelCoordinate, Chunk.Size, Chunk.Size);
                        var voxel = voxels[index];
                        var block = Blocks.Items[voxel.Block];

                        if (!block.IsTransparent || block.Absorption > 0)
                        {
                            break;
                        }

                        voxel.Light.Set(LightChanel.Sun, Light.Max);
                        voxels[index] = voxel;
                        var entry = new LightEntry(voxelCoordinate, Light.Max);
                        AddQueues.Enqueue(entry);
                    }
                }
            }

            while (RemoveQueues.TryDequeue(out var entry))
            {
                for (var i = 0; i < Voxel.Sides.Length; i++)
                {
                    var voxelCoordinate = entry.Coordinate + Voxel.Sides[i];
                    var chunkCoordinate = CoordinateUtility.ToChunk(voxelCoordinate);

                    if (!TryGetVoxels(chunkCoordinate, out var voxels))
                    {
                        continue;
                    }

                    var localVoxelCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, voxelCoordinate);
                    var index = IndexUtility.CoordinateToIndex(localVoxelCoordinate, Chunk.Size, Chunk.Size);
                    var voxel = voxels[index];
                    var level = voxel.Light.Get(LightChanel.Sun);
                    var block = voxels[index].Block;
                    var absorption = Blocks.Items[block].Absorption;

                    if (level != 0 && level == entry.Level - absorption - 1)
                    {
                        var removeEntry = new LightEntry(voxelCoordinate, level);
                        RemoveQueues.Enqueue(removeEntry);
                        voxel.Light.Set(LightChanel.Sun, Light.Min);
                        voxels[index] = voxel;
                    }
                    else if (level >= entry.Level)
                    {
                        var addEntry = new LightEntry(voxelCoordinate, level);
                        AddQueues.Enqueue(addEntry);
                    }
                }
            }

            while (AddQueues.TryDequeue(out var entry))
            {
                if (entry.Level <= 1)
                {
                    continue;
                }

                for (var i = 0; i < Voxel.Sides.Length; i++)
                {
                    var voxelCoordinate = entry.Coordinate + Voxel.Sides[i];
                    var chunkCoordinate = CoordinateUtility.ToChunk(voxelCoordinate);

                    if (!TryGetVoxels(chunkCoordinate, out var voxels))
                    {
                        continue;
                    }

                    var localVoxelCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, voxelCoordinate);
                    var index = IndexUtility.CoordinateToIndex(localVoxelCoordinate, Chunk.Size, Chunk.Size);
                    var voxel = voxels[index];
                    var level = voxel.Light.Get(LightChanel.Sun);
                    var block = voxels[index].Block;
                    var absorption = Blocks.Items[block].Absorption;

                    if (Blocks.Items[block].IsTransparent && level + absorption + 1 < entry.Level)
                    {
                        var newLevel = (byte)(entry.Level - absorption - 1);
                        voxel.Light.Set(LightChanel.Sun, newLevel);
                        voxels[index] = voxel;
                        var addEntry = new LightEntry(voxelCoordinate, newLevel);
                        AddQueues.Enqueue(addEntry);
                    }
                }
            }
        }

        private bool TryGetVoxels(in int3 chunkCoordinate, out Voxel* voxels)
        {
            var clasterCoordinate = new int3
            {
                x = chunkCoordinate.x - Column.x + 1,
                y = chunkCoordinate.y + 1,
                z = chunkCoordinate.z - Column.y + 1
            };

            var clasterIndex = IndexUtility.CoordinateToIndex(clasterCoordinate, 3, Height + 2);
            voxels = (Voxel*)Claster[clasterIndex];
            return voxels != null;
        }

        public void Dispose()
        {
            Claster.Dispose();
            ClasterEntities.Dispose();
            AddQueues.Dispose();
            RemoveQueues.Dispose();
        }
    }
}
