using System;

namespace CurlingSimulator.Network
{
    /// <summary>
    /// Abstraction over the chosen network library (Unity Netcode for GameObjects, Mirror, etc.).
    /// Implement this interface with the actual transport when adding online multiplayer.
    ///
    /// STUB — no implementation yet.
    /// </summary>
    public interface INetworkTransport
    {
        bool IsHost       { get; }
        bool IsConnected  { get; }

        void Send<T>(T data) where T : struct;
        void RegisterHandler<T>(Action<T> handler) where T : struct;

        event Action<int> OnPlayerConnected;
        event Action<int> OnPlayerDisconnected;
    }
}
