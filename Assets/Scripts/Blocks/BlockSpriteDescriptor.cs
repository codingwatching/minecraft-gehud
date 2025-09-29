using System;
using UnityEngine;

namespace Voxilarium
{
    [Serializable]
    public struct BlockSpriteDescriptor
    {
        public Sprite Right => right;
        public Sprite Left => left;
        public Sprite Top => top;
        public Sprite Bottom => bottom;
        public Sprite Front => front;
        public Sprite Back => back;

        [SerializeField]
        private Sprite right;
        [SerializeField]
        private Sprite left;
        [SerializeField]
        private Sprite top;
        [SerializeField]
        private Sprite bottom;
        [SerializeField]
        private Sprite front;
        [SerializeField]
        private Sprite back;

        public Sprite[] All()
        {
            return new Sprite[]
            {
                Right,
                Left,
                Top,
                Bottom,
                Front,
                Back
            };
        }
    }
}
