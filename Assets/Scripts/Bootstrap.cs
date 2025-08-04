using Unity.NetCode;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;

namespace Voxilarium
{
    [Preserve]
    public class Bootstrap : ClientServerBootstrap
    {
        public const string GameplayScene = "Overworld";


        public override bool Initialize(string defaultWorldName)
        {

            if (!DetermineIfBootstrappingEnabled())
            {
                return false;
            }

            if (SceneManager.GetActiveScene().name == GameplayScene)
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
