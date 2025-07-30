using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;
using Voxilarium;

namespace Voxilarium
{
    [BurstCompile]
    [UpdateAfter(typeof(ChunkGenerationSystem))]
    [UpdateAfter(typeof(BlockSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial struct ChunkMeshDataSystem : ISystem
    {
        private struct Task : IComponentData
        {
            public Entity Chunk;
            public JobHandle Job;
            public ChunkMeshDataJob Data;
        }

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Blocks>();
            state.RequireForUpdate<AtlasSize>();
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var commandBuffer = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            var buffer = SystemAPI.GetSingletonRW<ChunkBuffer>();

            var blocks = SystemAPI.GetSingleton<Blocks>();

            var atlasSize = SystemAPI.GetSingleton<AtlasSize>().Value;

            foreach (var (chunk, chunkEntity) in SystemAPI.Query<RefRO<Chunk>>()
                .WithAll<DirtyChunk>()
                .WithNone<DisableRendering, ThreadedChunk, ChunkMeshData>()
                .WithEntityAccess())
            {
                if (state.EntityManager.IsComponentEnabled<ThreadedChunk>(chunkEntity))
                {
                    continue;
                }

                const int clasterLength = 3 * 3 * 3;
                var claster = new NativeArray<NativeArray<Voxel>>(clasterLength, Allocator.Temp);
                var clasterEntities = new NativeArray<Entity>(clasterLength, Allocator.Temp);
                var origin = chunk.ValueRO.Coordinate - new int3(1, 1, 1);

                var isClasterValid = true;

                for (var i = 0; i < clasterLength; i++)
                {
                    var coordinate = origin + IndexUtility.IndexToCoordinate(i, 3, 3);
                    var clasterEntity = buffer.ValueRW.GetEntity(coordinate);

                    var isValidChunk = clasterEntity != Entity.Null
                        && !state.EntityManager.HasComponent<RawChunk>(clasterEntity)
                        && !state.EntityManager.IsComponentEnabled<ThreadedChunk>(clasterEntity);

                    if (isValidChunk)
                    {
                        claster[i] = state.EntityManager.GetComponentData<Chunk>(clasterEntity).Voxels;
                        clasterEntities[i] = clasterEntity;
                    }
                    else if (!isValidChunk
                        && coordinate.y != -1
                        && coordinate.y != buffer.ValueRO.Height)
                    {
                        isClasterValid = false;
                        break;
                    }
                }

                if (!isClasterValid)
                {
                    claster.Dispose();
                    continue;
                }

                var jobClaster = new NativeArray<Voxel>(clasterLength * Chunk.Volume, Allocator.Persistent);

                for (var i = 0; i < clasterLength; i++)
                {
                    var voxels = claster[i];
                    for (var j = 0; j < voxels.Length; j++)
                    {
                        jobClaster[i * Chunk.Volume + j] = voxels[j];
                    }
                }

                claster.Dispose();

                var jobClasterEntities = new NativeArray<Entity>(clasterLength, Allocator.Persistent);

                var job = new ChunkMeshDataJob
                {
                    Coordinate = chunk.ValueRO.Coordinate,
                    Claster = jobClaster,
                    ClasterEntities = jobClasterEntities,
                    Data = new ChunkMeshData
                    {
                        Vertices = new(Allocator.Persistent),
                        OpaqueIndices = new(Allocator.Persistent),
                        TransparentIndices = new(Allocator.Persistent),
                    },
                    Blocks = blocks,
                    AtlasSize = atlasSize
                };

                if (state.EntityManager.IsComponentEnabled<ImmediateChunk>(chunkEntity))
                {
                    job.Schedule().Complete();
                    commandBuffer.AddComponent(chunkEntity, job.Data);
                    job.Claster.Dispose();
                    job.ClasterEntities.Dispose();
                }
                else
                {
                    foreach (var clasterEntity in jobClasterEntities)
                    {
                        if (clasterEntity != Entity.Null)
                        {
                            state.EntityManager.SetComponentEnabled<ThreadedChunk>(clasterEntity, true);
                        }
                    }

                    var taskEntity = commandBuffer.CreateEntity();
                    commandBuffer.AddComponent(taskEntity, new Task
                    {
                        Chunk = chunkEntity,
                        Data = job,
                        Job = job.Schedule(),
                    });
                }
            }

            foreach (var (task, taskEntity) in SystemAPI.Query<RefRO<Task>>().WithEntityAccess())
            {
                if (!task.ValueRO.Job.IsCompleted)
                {
                    continue;
                }

                task.ValueRO.Job.Complete();

                commandBuffer.AddComponent(task.ValueRO.Chunk, task.ValueRO.Data.Data);

                foreach (var clasterEntity in task.ValueRO.Data.ClasterEntities)
                {
                    if (clasterEntity != Entity.Null)
                    {
                        commandBuffer.SetComponentEnabled<ThreadedChunk>(clasterEntity, false);
                    }
                }

                task.ValueRO.Data.Dispose();

                commandBuffer.DestroyEntity(taskEntity);
            }
        }

        [BurstCompile]
        void ISystem.OnDestroy(ref SystemState state)
        {
            foreach (var task in SystemAPI.Query<RefRO<Task>>())
            {
                task.ValueRO.Job.Complete();
                task.ValueRO.Data.Dispose();
                task.ValueRO.Data.Data.Dispose();
            }
        }
    }
}
