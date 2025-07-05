using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;

namespace Minecraft
{
    [BurstCompile]
    [UpdateAfter(typeof(ChunkSpawnSystem))]
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

            foreach (var (chunk, chunkEntity) in SystemAPI
                .Query<RefRO<Chunk>>()
                .WithAll<RawChunk>()
                .WithNone<ThreadedChunk>()
                .WithEntityAccess())
            {
                var job = new ChunkGenerationJob
                {
                    Chunk = chunk.ValueRO,
                };

                var handle = job.Schedule(Chunk.Volume, 1);

                var taskEntity = commandBuffer.CreateEntity();
                commandBuffer.AddComponent(taskEntity, new Task
                {
                    Chunk = chunkEntity,
                    Job = handle,
                });

                state.EntityManager.SetComponentEnabled<ThreadedChunk>(chunkEntity, true);
            }

            foreach (var (task, entity) in SystemAPI.Query<RefRO<Task>>().WithEntityAccess())
            {
                if (!task.ValueRO.Job.IsCompleted)
                {
                    continue;
                }

                task.ValueRO.Job.Complete();

                commandBuffer.RemoveComponent<RawChunk>(task.ValueRO.Chunk);
                state.EntityManager.SetComponentEnabled<ThreadedChunk>(task.ValueRO.Chunk, false);
                state.EntityManager.SetComponentEnabled<DirtyChunk>(task.ValueRO.Chunk, true);

                commandBuffer.DestroyEntity(entity);
            }
        }
    }
}
