using Unity.Mathematics;
using Unity.NetCode;

namespace Voxilarium
{
    public struct PlayerInput : IInputComponentData
    {
        public float2 Movement;
        public float Elevation;
        public float2 Look;
    }
}
