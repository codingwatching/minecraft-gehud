using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using static Unity.Entities.SystemAPI;

namespace Minecraft
{
    [BurstCompile]
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
            var commandBufferSystem = GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var commandBuffer = commandBufferSystem.CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (chunk, chunkEntity) in Query<Chunk>()
                .WithAll<RawChunk>()
                .WithNone<ThreadedChunk>()
                .WithEntityAccess())
            {
                var job = new ChunkGenerationJob
                {
                    Chunk = chunk,
                };

                var handle = job.Schedule(Chunk.Volume, 1);

                var taskEntity = commandBuffer.CreateEntity();
                commandBuffer.AddComponent(taskEntity, new Task
                {
                    Chunk = chunkEntity,
                    Job = handle,
                });

                SetComponentEnabled<ThreadedChunk>(chunkEntity, true);
            }

            foreach (var (task, taskEntity) in Query<Task>().WithEntityAccess())
            {
                if (!task.Job.IsCompleted)
                {
                    continue;
                }

                task.Job.Complete();

                commandBuffer.RemoveComponent<RawChunk>(task.Chunk);
                SetComponentEnabled<ThreadedChunk>(task.Chunk, false);
                SetComponentEnabled<DirtyChunk>(task.Chunk, true);

                commandBuffer.DestroyEntity(taskEntity);
            }
        }
    }
}
