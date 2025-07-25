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
        }

        public static void ShowChunk(in EntityManager entityManager, in EntityCommandBuffer commandBuffer, in Entity chunkEntity)
        {
            if (!entityManager.HasComponent<DisableRendering>(chunkEntity))
            {
                return;
            }

            commandBuffer.RemoveComponent<DisableRendering>(chunkEntity);
        }
    }
}
