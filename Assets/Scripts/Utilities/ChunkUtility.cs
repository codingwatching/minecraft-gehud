using Unity.Entities;
using Unity.Rendering;

namespace Minecraft.Utilities
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

            if (!entityManager.HasBuffer<SubChunk>(chunkEntity))
            {
                return;
            }

            var subchunks = entityManager.GetBuffer<SubChunk>(chunkEntity);
            foreach (var subchunk in subchunks)
            {
                if (!entityManager.HasComponent<DisableRendering>(subchunk.Value))
                {
                    commandBuffer.AddComponent<DisableRendering>(subchunk.Value);
                }
            }
        }

        public static void ShowChunk(in EntityManager entityManager, in EntityCommandBuffer commandBuffer, in Entity chunkEntity)
        {
            if (!entityManager.HasComponent<DisableRendering>(chunkEntity))
            {
                return;
            }

            commandBuffer.RemoveComponent<DisableRendering>(chunkEntity);

            if (!entityManager.HasBuffer<SubChunk>(chunkEntity))
            {
                return;
            }

            var subchunks = entityManager.GetBuffer<SubChunk>(chunkEntity);
            foreach (var subchunk in subchunks)
            {
                if (entityManager.HasComponent<DisableRendering>(subchunk.Value))
                {
                    commandBuffer.RemoveComponent<DisableRendering>(subchunk.Value);
                }
            }
        }

        public static void DestroyChunk(in EntityManager entityManager, in EntityCommandBuffer commandBuffer, in Entity chunkEntity)
        {
            commandBuffer.DestroyEntity(chunkEntity);

            if (!entityManager.HasBuffer<SubChunk>(chunkEntity))
            {
                return;
            }

            var subchunks = entityManager.GetBuffer<SubChunk>(chunkEntity);
            foreach (var subchunk in subchunks)
            {
                if (entityManager.Exists(subchunk.Value))
                {
                    commandBuffer.DestroyEntity(subchunk.Value);
                }
            }
        }
    }
}
