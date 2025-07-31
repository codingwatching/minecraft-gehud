using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

namespace Voxilarium
{
    public static class ChunkUtility
    {
        public static void HideChunk(in EntityManager entityManager, in EntityCommandBuffer commandBuffer, in Entity chunkEntity)
        {
            if (entityManager.HasComponent<DisableRendering>(chunkEntity))
            {
                return;
            }

            commandBuffer.AddComponent<DisableRendering>(chunkEntity);
        }

        public static void ShowChunk(in EntityManager entityManager, in EntityCommandBuffer commandBuffer, in Entity chunkEntity)
        {
            if (!entityManager.HasComponent<DisableRendering>(chunkEntity))
            {
                return;
            }

            commandBuffer.RemoveComponent<DisableRendering>(chunkEntity);
        }

        public static Entity SpawnChunk(EntityManager entityManager, in int3 coordinate, bool isVisible = true)
        {
            var chunkEntity = entityManager.CreateEntity();

            var commandBuffer = new EntityCommandBuffer(Allocator.Temp);

            if (!isVisible)
            {
                commandBuffer.AddComponent<DisableRendering>(chunkEntity);
            }

            var position = coordinate * Chunk.Size;

            commandBuffer.AddComponent(chunkEntity, new LocalToWorld
            {
                Value = float4x4.Translate(position)
            });

            commandBuffer.SetName(chunkEntity, "Chunk");

            commandBuffer.AddComponent(chunkEntity, new Chunk
            {
                Coordinate = coordinate,
                Voxels = new(Chunk.Volume, Allocator.Persistent)
            });

            commandBuffer.AddComponent<NotGeneratedChunk>(chunkEntity);
            commandBuffer.AddComponent<NotIlluminatedChunk>(chunkEntity);
            commandBuffer.AddComponent<ThreadedChunk>(chunkEntity);
            commandBuffer.SetComponentEnabled<ThreadedChunk>(chunkEntity, false);
            commandBuffer.AddComponent<DirtyChunk>(chunkEntity);
            commandBuffer.SetComponentEnabled<DirtyChunk>(chunkEntity, true);
            commandBuffer.AddComponent<ImmediateChunk>(chunkEntity);
            commandBuffer.SetComponentEnabled<ImmediateChunk>(chunkEntity, false);

            commandBuffer.Playback(entityManager);

            return chunkEntity;
        }
    }
}
