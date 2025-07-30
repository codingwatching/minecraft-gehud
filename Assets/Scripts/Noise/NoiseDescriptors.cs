using Unity.Entities;

namespace Voxilarium
{
    public class NoiseDescriptors : IComponentData
    {
        public NoiseDescriptor Continentalness;
        public NoiseDescriptor Erosion;
        public NoiseDescriptor PeaksAndValleys;
    }
}
