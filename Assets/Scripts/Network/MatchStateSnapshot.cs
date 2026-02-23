using System;
using System.Collections.Generic;
using CurlingSimulator.Core;
using CurlingSimulator.Input;

namespace CurlingSimulator.Network
{
    /// <summary>
    /// A fully serializable snapshot of match state used for:
    ///   - Late join (send to connecting client)
    ///   - Resync after packet loss (server rebroadcasts authoritative state)
    ///   - Replay (store ThrowHistory for post-game review)
    ///
    /// All fields are [Serializable] value types or structs.
    /// STUB — no network transmission logic yet.
    /// </summary>
    [Serializable]
    public struct MatchStateSnapshot
    {
        public MatchState         State;
        public long               SnapshotTick;    // monotonically increasing server tick
        public List<ThrowData>    ThrowHistory;    // every committed throw this match
    }
}
