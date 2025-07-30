using Unity.Entities;
using UnityEngine;

namespace Voxilarium
{
    [DisallowMultipleComponent]
    public class NoiseDescriptorsAuthoring : MonoBehaviour
    {
        [SerializeField]
        private NoiseDescriptor continentalness;
        [SerializeField]
        private NoiseDescriptor erosion;
        [SerializeField]
        private NoiseDescriptor peaksAndValleys;

        private class Baker : Baker<NoiseDescriptorsAuthoring>
        {
            public override void Bake(NoiseDescriptorsAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                
                AddComponentObject
                (
                    entity,
                    new NoiseDescriptors
                    {
                        Continentalness = authoring.continentalness,
                        Erosion = authoring.erosion,
                        PeaksAndValleys = authoring.peaksAndValleys,
                    }
                );
            }
        }
    }
}
