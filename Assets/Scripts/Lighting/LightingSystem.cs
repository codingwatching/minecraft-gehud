using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Voxilarium
{
    public struct Illumination : IComponentData
    {
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial struct LightingSystem : ISystem
    {
        private struct Task : IComponentData
        {
            public IlluminationJob Data;
            public JobHandle Job;
        }

        private unsafe bool TrySchedule(EntityManager entityManager, EntityCommandBuffer commandBuffer, in Chunks chunks, in Blocks blocks, in int2 column)
        {
            var clasterHeight = chunks.Height + 2;
            var clasterLength = 3 * 3 * clasterHeight;
            var claster = new NativeArray<IntPtr>(clasterLength, Allocator.Temp);
            var clasterEntities = new NativeArray<Entity>(clasterLength, Allocator.Temp);

            var origin = new int3
            {
                x = column.x - 1,
                y = -1,
                z = column.y - 1
            };

            for (var i = 0; i < clasterLength; i++)
            {
                var coordinate = origin + IndexUtility.IndexToCoordinate(i, 3, clasterHeight);
                var clasterEntity = chunks.GetEntity(coordinate);

                var isValidChunk = clasterEntity != Entity.Null
                    && !entityManager.HasComponent<NotGeneratedChunk>(clasterEntity)
                    && !entityManager.IsComponentEnabled<ThreadedChunk>(clasterEntity);

                if (isValidChunk)
                {
                    claster[i] = new(entityManager.GetComponentData<Chunk>(clasterEntity).Voxels.GetUnsafePtr());
                    clasterEntities[i] = clasterEntity;
                }
                else if (!isValidChunk
                    && coordinate.y != -1
                    && coordinate.y != chunks.Height)
                {
                    claster.Dispose();
                    clasterEntities.Dispose();
                    return false;
                }
            }

            var jobClaster = new NativeArray<IntPtr>(clasterLength, Allocator.Persistent);
            jobClaster.CopyFrom(claster);
            claster.Dispose();

            var jobClasterEntities = new NativeArray<Entity>(clasterLength, Allocator.Persistent);
            jobClasterEntities.CopyFrom(clasterEntities);
            clasterEntities.Dispose();

            foreach (var entity in jobClasterEntities)
            {
                if (entity != Entity.Null)
                {
                    entityManager.SetComponentEnabled<ThreadedChunk>(entity, true);
                }
            }

            var job = new IlluminationJob
            {
                Blocks = blocks,
                Column = column,
                Height = chunks.Height,
                Claster = jobClaster,
                ClasterEntities = jobClasterEntities,

                AddQueues = new NativeQueue<LightEntry>(Allocator.Persistent),
                RemoveQueues = new NativeQueue<LightEntry>(Allocator.Persistent),
            };

            var taskEntity = commandBuffer.CreateEntity();
            commandBuffer.AddComponent(taskEntity, new Task
            {
                Data = job,
                Job = job.Schedule()
            });

            return true;
        }

        private bool TryComplete(EntityManager entityManager, EntityCommandBuffer commandBuffer, in Chunks chunks, in Task task)
        {
            if (!task.Job.IsCompleted)
            {
                return false;
            }

            task.Job.Complete();

            for (var y = 0; y < chunks.Height; y++)
            {
                var chunkCoordinate = new int3(task.Data.Column.x, y, task.Data.Column.y);
                var chunkEntity = chunks.GetEntity(chunkCoordinate);
                commandBuffer.AddComponent<Illumination>(chunkEntity);
            }

            foreach (var entity in task.Data.ClasterEntities)
            {
                if (entity != Entity.Null)
                {
                    entityManager.SetComponentEnabled<ThreadedChunk>(entity, false);
                }
            }

            task.Data.Dispose();

            return true;
        }

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            state.EntityManager.AddComponentData(state.SystemHandle, new Lighting(Allocator.Persistent));
            state.RequireForUpdate<Blocks>();
            state.RequireForUpdate<Chunks>();
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var commandBuffer = SystemAPI
                .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            var blocks = SystemAPI.GetSingletonRW<Blocks>();
            var chunks = SystemAPI.GetSingletonRW<Chunks>();

            foreach (var (request, entity) in SystemAPI
                .Query<RefRO<IlluminationRequest>>()
                .WithEntityAccess())
            {
                if (TrySchedule(state.EntityManager, commandBuffer, chunks.ValueRO, blocks.ValueRO, request.ValueRO.Column))
                {
                    commandBuffer.DestroyEntity(entity);
                }
            }

            foreach (var (task, entity) in SystemAPI
                .Query<RefRO<Task>>()
                .WithEntityAccess())
            {
                if (TryComplete(state.EntityManager, commandBuffer, chunks.ValueRO, task.ValueRO))
                {
                    commandBuffer.DestroyEntity(entity);
                }
            }

            foreach (var (chunk, entity) in SystemAPI
                .Query<RefRO<Chunk>>()
                .WithAll<Illumination, NotIlluminatedChunk>()
                .WithEntityAccess())
            {
                var chunkCoordinate = chunk.ValueRO.Coordinate;
                var origin = chunkCoordinate - new int3(1, 1, 1);

                var isValidClaster = true;

                for (var j = 0; j < 3 * 3 * 3; j++)
                {
                    var coordinate = origin + IndexUtility.IndexToCoordinate(j, 3, 3);
                    var sideChunk = chunks.ValueRO.GetEntity(coordinate);
                    bool isValidChunk = sideChunk != Entity.Null
                        && (state.EntityManager.HasComponent<Illumination>(sideChunk)
                        || !state.EntityManager.HasComponent<NotGeneratedChunk>(sideChunk)
                        && !state.EntityManager.HasComponent<NotIlluminatedChunk>(sideChunk));

                    if (!isValidChunk && coordinate.y != -1 && coordinate.y != chunks.ValueRO.Height)
                    {
                        isValidClaster = false;
                        break;
                    }
                }

                if (isValidClaster)
                {
                    commandBuffer.RemoveComponent<NotIlluminatedChunk>(entity);
                }
            }
        }

        [BurstCompile]
        void ISystem.OnDestroy(ref SystemState state)
        {
            state.EntityManager.GetComponentData<Lighting>(state.SystemHandle).Dispose();

            foreach (var task in SystemAPI
                .Query<RefRO<Task>>())
            {
                task.ValueRO.Job.Complete();
                task.ValueRO.Data.Dispose();
            }
        }
    }
}
