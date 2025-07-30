using Unity.Entities;
using UnityEngine;

namespace Voxilarium
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial struct PlayerCursorSystem : ISystem
    {
        void ISystem.OnCreate(ref SystemState state)
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
}
