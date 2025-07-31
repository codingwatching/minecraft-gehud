using Unity.Mathematics;

namespace Voxilarium
{
    public static class CoordinateUtility
    {
        public static int3 ToChunk(in int3 voxelCoordinate)
        {
            return (int3)math.floor(voxelCoordinate / new float3(Chunk.Size));
        }

        public static int3 ToLocal(in int3 chunkCoordinate, in int3 voxelCoordinate)
        {
            return voxelCoordinate - chunkCoordinate * Chunk.Size;
        }
    }
}
