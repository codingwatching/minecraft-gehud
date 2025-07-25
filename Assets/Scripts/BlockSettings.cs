using UnityEngine;

namespace Minecraft
{
    [CreateAssetMenu(fileName = "Block", menuName = "Voxilarium/Block Settings")]
    public class BlockSettings : ScriptableObject
    {
        public bool IsSolid => isSolid;
        public bool IsTransparent => isTransparent;
        public int Absorption => absorption;

        [SerializeField]
        private bool isSolid = true;
        [SerializeField]
        private bool isTransparent = false;
        [SerializeField, Range(0, 15)]
        private int absorption = 0;
    }
}
