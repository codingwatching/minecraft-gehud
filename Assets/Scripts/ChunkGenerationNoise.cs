using System;
using Unity.Entities;

namespace Voxilarium
{
    public struct ChunkGenerationNoise : IComponentData, IDisposable
    {
        public Noise Continentalness;
        public Noise Erosion;
        public Noise PeaksAndValleys;

        public void Dispose()
        {
            Continentalness.Dispose();
            Erosion.Dispose();
            PeaksAndValleys.Dispose();
        }
    }
}
