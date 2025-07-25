using Unity.Mathematics;
using UnityEngine;

namespace Minecraft
{
    [CreateAssetMenu(menuName = "Voxilarium/Noise Settings")]
    public class NoiseSettings : ScriptableObject
    {
        public int3 Offset => offset;

        public float3 Scale => scale;

        public int Octaves => octaves;

        public float Lacunarity => lacunarity;

        public float Persistance => persistance;

        public AnimationCurve Modification => modification;

        [SerializeField]
        private int3 offset;
        [SerializeField]
        private float3 scale = new(1.0f, 1.0f, 1.0f);
        [SerializeField, Min(1)]
        private int octaves = 3;
        [SerializeField, Min(0.0f)]
        private float lacunarity = 2;
        [SerializeField, Range(0.0f, 1.0f)]
        private float persistance = 0.5f;
        [SerializeField]
        private AnimationCurve modification = AnimationCurve.Linear(0.0f, 0.0f, 1.0f, 1.0f);
    }
}
