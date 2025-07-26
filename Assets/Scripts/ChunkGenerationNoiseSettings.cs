using Unity.Entities;

namespace Voxilarium
{
    public class ChunkGenerationNoiseSettings : IComponentData
    {
        public NoiseSettings Continentalness;
        public NoiseSettings Erosion;
        public NoiseSettings PeaksAndValleys;
    }
}
