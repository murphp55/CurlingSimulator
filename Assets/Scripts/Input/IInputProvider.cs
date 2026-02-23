using System;

namespace CurlingSimulator.Input
{
    /// <summary>
    /// Abstraction over all input sources: human player, AI, or (future) network.
    /// GameManager consumes only this interface — it never knows whether input comes
    /// from a mouse, an AI algorithm, or a network packet.
    /// </summary>
    public interface IInputProvider
    {
        /// <summary>Fired once when a throw is committed (LMB released, AI decides, or network receives).</summary>
        event Action<ThrowData> OnThrowCommitted;

        /// <summary>Fired every frame while a stone is in motion to convey sweep state.</summary>
        event Action<SweepData> OnSweepUpdate;

        /// <summary>True while this provider is actively sweeping.</summary>
        bool IsSweepActive { get; }

        /// <summary>
        /// Called by GameManager when it is this provider's turn to throw.
        /// For human input this enables the throw UI; for AI this starts the think coroutine.
        /// </summary>
        void BeginThrowInput(ThrowData context);

        /// <summary>
        /// Called by GameManager when a stone is in motion so this provider can emit sweep data.
        /// </summary>
        void BeginSweepInput();

        /// <summary>Called when sweeping should stop (stone has come to rest).</summary>
        void EndSweepInput();
    }
}
