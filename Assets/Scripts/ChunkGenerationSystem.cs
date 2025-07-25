using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Minecraft
{
    [UpdateAfter(typeof(ChunkInitializationSystem))]
    [UpdateAfter(typeof(ChunkGenerationNoiseSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial struct ChunkGenerationSystem : ISystem
    {
        private struct Task : IComponentData
        {
            public Entity Chunk;
            public JobHandle Job;
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var commandBuffer = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            var noise = SystemAPI.GetSingleton<ChunkGenerationNoise>();

            foreach (var (chunk, chunkEntity) in SystemAPI
                .Query<RefRO<Chunk>>()
                .WithAll<RawChunk>()
                .WithNone<ThreadedChunk>()
                .WithEntityAccess())
            {
                var job = new ChunkGenerationJob
                {
                    Chunk = chunk.ValueRO,
                    Noise = noise
                };

                var handle = job.Schedule(Chunk.Volume, 1);

                if (state.EntityManager.IsComponentEnabled<ImmediateChunk>(chunkEntity))
                {
                    handle.Complete();
                    ApplyJob(chunkEntity, commandBuffer);
                }
                else
                {
                    var taskEntity = commandBuffer.CreateEntity();

                    commandBuffer.AddComponent(taskEntity, new Task
                    {
                        Chunk = chunkEntity,
                        Job = handle,
                    });

                    state.EntityManager.SetComponentEnabled<ThreadedChunk>(chunkEntity, true);
                }
            }

            foreach (var (task, entity) in SystemAPI.Query<RefRO<Task>>().WithEntityAccess())
            {
                if (!task.ValueRO.Job.IsCompleted)
                {
                    continue;
                }

                task.ValueRO.Job.Complete();

                ApplyJob(task.ValueRO.Chunk, commandBuffer);

                commandBuffer.DestroyEntity(entity);
            }
        }

        private void ApplyJob(Entity entity, in EntityCommandBuffer commandBuffer)
        {
            commandBuffer.RemoveComponent<RawChunk>(entity);
            commandBuffer.SetComponentEnabled<ThreadedChunk>(entity, false);
            commandBuffer.SetComponentEnabled<DirtyChunk>(entity, true);
        }
    }
}
