using Unity.NetCode;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;

namespace Voxilarium
{
    [Preserve]
    public class Bootstrap : ClientServerBootstrap
    {
        public override bool Initialize(string defaultWorldName)
        {
            const string gameplayScene = "Overworld";

            if (!DetermineIfBootstrappingEnabled())
            {
                return false;
            }

            if (SceneManager.GetActiveScene().name == gameplayScene)
            {
                AutoConnectPort = 7979;
                CreateDefaultClientServerWorlds();
            }
            else
            {
                AutoConnectPort = 0;
                CreateLocalWorld(defaultWorldName);
            }

            return true;
        }
    }
}
