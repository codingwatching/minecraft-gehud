using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

namespace Voxilarium
{
    public struct PlayerView : IComponentData
    {
        public Entity Entity;
        public float Pitch;
        public float Yaw;
    }
}
