using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

namespace Voxilarium
{
    public struct PlayerInput : IComponentData
    {
        public float2 Movement;
        public float Elevation;
        public float2 Look;
        public InputEvent Attack;
        public InputEvent Defend;
        public InputEvent Jump;
    }
}
