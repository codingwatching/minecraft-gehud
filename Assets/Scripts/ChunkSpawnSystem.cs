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
        void ISystem.OnUpdate(ref SystemState state)
        {
            var commandBuffer = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (request, chunkEntity) in SystemAPI.Query<RefRO<ChunkSpawnRequest>>().WithEntityAccess())
            {
                if (!request.ValueRO.IsVisible)
                {
                    commandBuffer.AddComponent<DisableRendering>(chunkEntity);
                }

                var position = request.ValueRO.Coordinate * Chunk.Size;

                commandBuffer.AddComponent(chunkEntity, new LocalToWorld
                {
                    Value = float4x4.Translate(position)
                });

                commandBuffer.SetName(chunkEntity, $"Chunk({request.ValueRO.Coordinate.x}, {request.ValueRO.Coordinate.y}, {request.ValueRO.Coordinate.z})");

                commandBuffer.AddComponent(chunkEntity, new Chunk
                {
                    Coordinate = request.ValueRO.Coordinate,
                    Voxels = new(Chunk.Volume, Allocator.Persistent)
                });

                commandBuffer.AddComponent<RawChunk>(chunkEntity);
                commandBuffer.AddComponent<ThreadedChunk>(chunkEntity);
                commandBuffer.SetComponentEnabled<ThreadedChunk>(chunkEntity, false);
                commandBuffer.AddComponent<DirtyChunk>(chunkEntity);
                commandBuffer.SetComponentEnabled<DirtyChunk>(chunkEntity, false);
                commandBuffer.AddComponent<ImmediateChunk>(chunkEntity);
                commandBuffer.SetComponentEnabled<ImmediateChunk>(chunkEntity, false);

                commandBuffer.RemoveComponent<ChunkSpawnRequest>(chunkEntity);
            }
        }
    }
}
