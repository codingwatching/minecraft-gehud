using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Voxilarium;

namespace Voxilarium
{
    [UpdateAfter(typeof(NoiseSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial struct ChunkGenerationSystem : ISystem
    {
        private struct Task : IComponentData
        {
            public Entity Chunk;
            public JobHandle Job;
        }

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Noises>();
            state.RequireForUpdate<Chunks>();
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var commandBuffer = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            var noise = SystemAPI.GetSingletonRW<Noises>();
            var chunks = SystemAPI.GetSingletonRW<Chunks>();

            foreach (var (chunk, chunkEntity) in SystemAPI
                .Query<RefRW<Chunk>>()
                .WithAll<NotGeneratedChunk>()
                .WithNone<ThreadedChunk>()
                .WithEntityAccess())
            {
                var job = new ChunkGenerationJob
                {
                    Coordinate = chunk.ValueRO.Coordinate,
                    Height = chunks.ValueRO.Height,
                    Noise = noise.ValueRO,
                    Voxels = chunk.ValueRW.Voxels,
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
            commandBuffer.SetComponentEnabled<ThreadedChunk>(entity, false);
            commandBuffer.RemoveComponent<NotGeneratedChunk>(entity);
        }

        [BurstCompile]
        void ISystem.OnDestroy(ref SystemState state)
        {
            foreach (var task in SystemAPI.Query<RefRO<Task>>())
            {
                task.ValueRO.Job.Complete();
            }

            foreach (var chunk in SystemAPI.Query<RefRO<Chunk>>())
            {
                chunk.ValueRO.Dispose();
            }
        }
    }
}
