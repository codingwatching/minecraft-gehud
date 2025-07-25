using Unity.Entities;

namespace Minecraft
{
    public class ChunkGenerationNoiseSettings : IComponentData
    {
        public NoiseSettings Continentalness;
        public NoiseSettings Erosion;
        public NoiseSettings PeaksAndValleys;
    }
}
