using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Rendering;
using static Unity.Entities.SystemAPI;

namespace Minecraft
{
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial struct ChunkMeshDataSystem : ISystem
    {
        private struct Task : IComponentData
        {
            public Entity Chunk;
            public JobHandle Job;
            public ChunkMeshData Data;
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var commandBuffer = GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (chunk, chunkEntity) in Query<RefRO<Chunk>>()
                .WithAll<DirtyChunk>()
                .WithNone<DisableRendering, ThreadedChunk, ChunkMeshData>()
                .WithEntityAccess())
            {
                var job = new ChunkMeshDataJob
                {
                    Chunk = chunk.ValueRO,
                    Data = new ChunkMeshData
                    {
                        Vertices = new(Allocator.Persistent),
                        OpaqueIndices = new(Allocator.Persistent),
                        TransparentIndices = new(Allocator.Persistent),
                    }
                };

                if (IsComponentEnabled<ImmediateChunk>(chunkEntity))
                {
                    job.Schedule().Complete();
                    commandBuffer.AddComponent(chunkEntity, job.Data);
                    SetComponentEnabled<ImmediateChunk>(chunkEntity, false);
                }
                else
                {
                    SetComponentEnabled<ThreadedChunk>(chunkEntity, true);
                    var taskEntity = commandBuffer.CreateEntity();
                    commandBuffer.AddComponent(taskEntity, new Task
                    {
                        Chunk = chunkEntity,
                        Data = job.Data,
                        Job = job.Schedule(),
                    });
                }
            }

            foreach (var (task, taskEntity) in Query<RefRO<Task>>().WithEntityAccess())
            {
                if (!task.ValueRO.Job.IsCompleted)
                {
                    continue;
                }

                task.ValueRO.Job.Complete();

                commandBuffer.AddComponent(task.ValueRO.Chunk, task.ValueRO.Data);
                SetComponentEnabled<ThreadedChunk>(task.ValueRO.Chunk, false);

                commandBuffer.DestroyEntity(taskEntity);
            }
        }
    }
}
