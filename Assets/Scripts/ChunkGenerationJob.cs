using Voxilarium.Utilities;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace Voxilarium
{
    [BurstCompile]
    public struct ChunkGenerationJob : IJobParallelFor
    {
        public Chunk Chunk;
        [ReadOnly]
        public ChunkGenerationNoise Noise;

        void IJobParallelFor.Execute(int index)
        {
            var localCoordinate = IndexUtility.IndexToCoordinate(index, Chunk.Size, Chunk.Size);
            var coordinate = Chunk.Coordinate * Chunk.Size + localCoordinate;

            var continentalness = Noise.Continentalness.Sample2D(coordinate.x, coordinate.z);
            var erosion = Noise.Erosion.Sample2D(coordinate.x, coordinate.z);
            var peaksAndValleys = Noise.PeaksAndValleys.Sample2D(coordinate.x, coordinate.z);
            var result = continentalness * erosion * peaksAndValleys;

            if (coordinate.y <= (int)(result * Chunk.Size * Chunk.Size))
            {
                Chunk[index] = new(Voxel.Stone);
            }
        }
    }
}
