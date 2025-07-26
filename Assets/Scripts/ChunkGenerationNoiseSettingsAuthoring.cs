using Unity.Entities;
using UnityEngine;

namespace Voxilarium
{
    [DisallowMultipleComponent]
    public class ChunkGenerationNoiseSettingsAuthoring : MonoBehaviour
    {
        [SerializeField]
        private NoiseSettings continentalness;
        [SerializeField]
        private NoiseSettings erosion;
        [SerializeField]
        private NoiseSettings peaksAndValleys;

        private class Baker : Baker<ChunkGenerationNoiseSettingsAuthoring>
        {
            public override void Bake(ChunkGenerationNoiseSettingsAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponentObject
                (
                    entity,
                    new ChunkGenerationNoiseSettings
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
