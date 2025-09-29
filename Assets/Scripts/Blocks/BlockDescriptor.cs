using UnityEngine;

namespace Voxilarium
{
    [CreateAssetMenu(fileName = "Block", menuName = "Voxilarium/Block Descriptor")]
    public class BlockDescriptor : ScriptableObject
    {
        public bool IsSolid => isSolid;
        public bool IsTransparent => isTransparent;
        public LightColor Emission => emission;
        public int Absorption => absorption;
        public BlockSpriteDescriptor Sprites => sprites;

        [SerializeField]
        private bool isSolid = true;
        [SerializeField]
        private bool isTransparent = false;
        [SerializeField]
        private LightColor emission;
        [SerializeField, Range(0, 15)]
        private int absorption = 0;
        [SerializeField]
        private BlockSpriteDescriptor sprites;
    }
}
