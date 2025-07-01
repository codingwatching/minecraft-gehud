using Minecraft.Utilities;
using Unity.Burst;
using Unity.Jobs;

namespace Minecraft
{
    [BurstCompile]
    public struct ChunkGenerationJob : IJobParallelFor
    {
        public Chunk Chunk;

        void IJobParallelFor.Execute(int index)
        {
            var coordinate = IndexUtility.IndexToCoordinate(index, Chunk.Size, Chunk.Size, Chunk.Area);

            var globalY = coordinate.y + Chunk.Coordinate.y * Chunk.Size;

            if (globalY >= 8)
            {
                Chunk[index] = Voxel.Air;
            }
            else
            {
                Chunk[index] = Voxel.Stone;
            }
        }
    }
}
