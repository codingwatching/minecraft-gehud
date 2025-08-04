using TMPro;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Voxilarium
{
    public class MainMenuManager : MonoBehaviour
    {
        public const ushort DefaultPort = 7979;

        [SerializeField]
        private GameObject selector;

        [Header("Host")]
        [SerializeField]
        private GameObject hostForm;
        [SerializeField]
        private TMP_InputField hostPort;

        [Header("Connect")]
        [SerializeField]
        private GameObject connectForm;
        [SerializeField]
        private TMP_InputField connectAddress;
        [SerializeField]
        private TMP_InputField connectPort;

        public void ShowHostForm()
        {
            selector.SetActive(false);
            hostForm.SetActive(true);
        }

        public void HideHostForm()
        {
            selector.SetActive(true);
            hostForm.SetActive(false);
        }

        public void ShowConnectForm()
        {
            selector.SetActive(false);
            connectForm.SetActive(true);
        }

        public void HideConnectForm()
        {
            selector.SetActive(true);
            connectForm.SetActive(false);
        }

        private void Host()
        {
            if (ClientServerBootstrap.RequestedPlayType != ClientServerBootstrap.PlayType.ClientAndServer)
            {
                Debug.LogError($"Creating client/server worlds is not allowed if playmode is set to {ClientServerBootstrap.RequestedPlayType}");
                return;
            }

            var server = ClientServerBootstrap.CreateServerWorld("ServerWorld");
            var client = ClientServerBootstrap.CreateClientWorld("ClientWorld");

            DestroyLocalSimulationWorld();
            World.DefaultGameObjectInjectionWorld ??= server;

            SceneManager.LoadScene(Bootstrap.GameplayScene);

            var port = ParsePortOrDefault(hostPort.text);

            NetworkEndpoint ep = NetworkEndpoint.AnyIpv4.WithPort(port);
            {
                using var drvQuery = server.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
                drvQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW.Listen(ep);
            }

            ep = NetworkEndpoint.LoopbackIpv4.WithPort(port);
            {
                using var drvQuery = client.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
                drvQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW.Connect(client.EntityManager, ep);
            }
        }

        public void Connect()
        {
            var client = ClientServerBootstrap.CreateClientWorld("ClientWorld");
            DestroyLocalSimulationWorld();

            World.DefaultGameObjectInjectionWorld ??= client;
            SceneManager.LoadScene(Bootstrap.GameplayScene);

            var ep = NetworkEndpoint.Parse(connectAddress.text, ParsePortOrDefault(connectPort.text));
            {
                using var drvQuery = client.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
                drvQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW.Connect(client.EntityManager, ep);
            }
        }

        protected void DestroyLocalSimulationWorld()
        {
            foreach (var world in World.All)
            {
                if (world.Flags == WorldFlags.Game)
                {
                    world.Dispose();
                    break;
                }
            }
        }

        private ushort ParsePortOrDefault(string s)
        {
            if (!ushort.TryParse(s, out var port))
            {
                Debug.LogWarning($"Unable to parse port, using default port {DefaultPort}");
                return DefaultPort;
            }

            return port;
        }
    }
}
