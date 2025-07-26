using Unity.NetCode;
using UnityEngine.Scripting;

namespace Voxilarium
{
    [Preserve]
    public class Bootstrap : ClientServerBootstrap
    {
        public override bool Initialize(string defaultWorldName)
        {
            AutoConnectPort = 7979;
            return base.Initialize(defaultWorldName);
        }
    }
}
