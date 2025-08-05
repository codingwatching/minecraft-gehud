using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Voxilarium
{
    [BurstCompile]
    public struct ChunkGenerationJob : IJobParallelFor
    {
        public const int Offset = 5;

        [ReadOnly]
        public int3 Coordinate;
        [ReadOnly]
        public int Height;
        [ReadOnly]
        public Noises Noise;
        [WriteOnly]
        public NativeArray<Voxel> Voxels;

        void IJobParallelFor.Execute(int index)
        {
            var localCoordinate = IndexUtility.IndexToCoordinate(index, Chunk.Size, Chunk.Size);
            var coordinate = Coordinate * Chunk.Size + localCoordinate;

            var continentalness = Noise.Continentalness.Sample2D(coordinate.x, coordinate.z);
            var erosion = Noise.Erosion.Sample2D(coordinate.x, coordinate.z);
            var peaksAndValleys = Noise.PeaksAndValleys.Sample2D(coordinate.x, coordinate.z);
            var result = continentalness * erosion * peaksAndValleys;

            var height = Offset * Chunk.Size + (int)(result * Chunk.Size * (Height - Offset));
            if (coordinate.y <= height)
            {
                if (coordinate.y == height)
                {
                    Voxels[index] = new(3);
                }
                else if (coordinate.y >= height - 5)
                {
                    Voxels[index] = new(2);
                }
                else
                {
                    Voxels[index] = new(1);
                }
            }
        }
    }
}
