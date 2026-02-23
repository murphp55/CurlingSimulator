using CurlingSimulator.Input;

namespace CurlingSimulator.Network
{
    /// <summary>
    /// Future: implement this with Unity Netcode or Mirror to receive ThrowData and SweepData
    /// from the remote player and fire the same IInputProvider events.
    /// GameManager never changes — it always consumes IInputProvider.
    ///
    /// STUB — no implementation yet.
    /// </summary>
    public interface INetworkInputProvider : IInputProvider
    {
        void SendThrowToRemote(ThrowData data);
        void SendSweepToRemote(SweepData data);
        bool IsLocalPlayer    { get; }
        int  NetworkPlayerId  { get; }
    }
}
