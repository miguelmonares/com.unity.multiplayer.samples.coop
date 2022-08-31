using System;
using Unity.Netcode;
using VContainer;

namespace Unity.Multiplayer.Samples.BossRoom.Server
{
    public class PostGameStateData : NetworkBehaviour
    {
        public NetworkVariable<WinState> WinState = new NetworkVariable<WinState>();

        [Inject]
        public void Construct(PersistentGameState persistentGameState)
        {
            if (NetworkManager.Singleton.IsServer)
            {
                WinState.Value = persistentGameState.WinState;
            }
        }
    }
}
