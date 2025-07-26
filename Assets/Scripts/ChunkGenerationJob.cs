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

            var height = (int)(result * Chunk.Size * Chunk.Size);
            if (coordinate.y <= height)
            {
                if (coordinate.y == height)
                {
                    Chunk[index] = new(3);
                }
                else if (coordinate.y >= height - 5)
                {
                    Chunk[index] = new(2);
                }
                else
                {
                    Chunk[index] = new(1);
                }
            }
        }
    }
}
