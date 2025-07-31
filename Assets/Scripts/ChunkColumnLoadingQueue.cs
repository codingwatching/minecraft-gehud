using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Voxilarium
{
    public struct ChunkColumnLoadingQueue : IComponentData, IDisposable
    {
        public NativeList<int2> Value;

        public void UpdateLoading(EntityManager entityManager, ref Chunks chunks, in int2 center, in EntityCommandBuffer commandBuffer)
        {
            Value.Clear();

            var startX = center.x - chunks.Distance - Chunks.BufferDistance;
            var endX = center.x + chunks.Distance + Chunks.BufferDistance;
            var startZ = center.y - chunks.Distance - Chunks.BufferDistance;
            var endZ = center.y + chunks.Distance + Chunks.BufferDistance;

            var x = startX;
            var z = endZ;

            int startXBound = startX;
            int endXBound = endX;
            int startZBound = startZ;
            int endZBound = endZ;

            var size = chunks.Distance * 2 + 1 + Chunks.BufferDistance * 2;
            int length = size * size;
            int direction = 0;

            for (int i = 0; i < length; i++)
            {
                bool isVisible = x != startX && x != endX && z != startZ && z != endZ;

                if (isVisible)
                {
                    Value.Add(new int2(x, z));
                }

                for (int y = 0; y < chunks.Height; y++)
                {
                    var chunkCoordinate = new int3(x, y, z);

                    var chunkEntity = chunks.GetEntity(chunkCoordinate);

                    if (chunkEntity != Entity.Null)
                    {
                        if (isVisible)
                        {
                            ChunkUtility.ShowChunk(entityManager, commandBuffer, chunkEntity);
                        }
                        else
                        {
                            ChunkUtility.HideChunk(entityManager, commandBuffer, chunkEntity);
                        }
                    }
                }

                switch (direction)
                {
                    case 0:
                        ++x;
                        break;
                    case 1:
                        --z;
                        break;
                    case 2:
                        --x;
                        break;
                    case 3:
                        ++z;
                        break;
                }

                if (direction == 0 && x == endXBound)
                {
                    direction = 1;
                }
                else if (direction == 1 && z == startZBound)
                {
                    direction = 2;
                    ++startZBound;
                }
                else if (direction == 2 && x == startXBound)
                {
                    direction = 3;
                    --endZBound;
                    ++startXBound;
                }
                else if (direction == 3 && z == endZBound)
                {
                    direction = 0;
                    --endXBound;
                }
            }
        }

        public void Dispose()
        {
            Value.Dispose();
        }
    }
}
