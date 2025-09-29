using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

namespace Voxilarium
{
    public struct Hitbox : IComponentData
    {
        public float3 Velocity;
        public bool UseGravity;
        public AABB Bounds;
    }

    public struct HitboxPosition : IInputComponentData
    {
        public float3 Value;
    }
}
