using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

namespace Minecraft
{
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial struct ChunkSpawnSystem : ISystem
    {
        [BurstCompile]
        readonly void ISystem.OnUpdate(ref SystemState state)
        {
            var commandBufferSystem = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var commandBuffer = commandBufferSystem.CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (request, requestEntity) in SystemAPI.Query<ChunkSpawnRequest>().WithEntityAccess())
            {
                var chunkEntity = commandBuffer.CreateEntity();

                if (!request.IsVisible)
                {
                    commandBuffer.AddComponent<DisableRendering>(chunkEntity);
                }

                var position = request.Coordinate * Chunk.Size;

                commandBuffer.AddComponent(chunkEntity, new LocalToWorld
                {
                    Value = float4x4.Translate(position)
                });

                commandBuffer.SetName(chunkEntity, $"Chunk({request.Coordinate.x}, {request.Coordinate.y}, {request.Coordinate.z})");

                commandBuffer.AddComponent(chunkEntity, new Chunk
                {
                    Coordinate = request.Coordinate,
                    Voxels = new(Chunk.Volume, Allocator.Persistent)
                });

                commandBuffer.AddComponent<RawChunk>(chunkEntity);
                commandBuffer.AddComponent<ThreadedChunk>(chunkEntity);
                commandBuffer.SetComponentEnabled<ThreadedChunk>(chunkEntity, false);
                commandBuffer.AddComponent<DirtyChunk>(chunkEntity);
                commandBuffer.SetComponentEnabled<DirtyChunk>(chunkEntity, false);
                commandBuffer.AddComponent<ImmediateChunk>(chunkEntity);
                commandBuffer.SetComponentEnabled<ImmediateChunk>(chunkEntity, false);

                commandBuffer.DestroyEntity(requestEntity);
            }
        }
    }
}
