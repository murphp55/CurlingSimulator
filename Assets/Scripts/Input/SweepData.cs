using System;

namespace CurlingSimulator.Input
{
    /// <summary>
    /// Per-frame sweep state emitted while a stone is in motion.
    /// Serializable so it can be sent over the network later.
    /// </summary>
    [Serializable]
    public struct SweepData
    {
        /// <summary>Normalised sweep intensity 0–1. 0 = no sweep, 1 = maximum effective sweep.</summary>
        public float Intensity;

        /// <summary>Frame delta time at the moment this was sampled.</summary>
        public float DeltaTime;

        /// <summary>UTC millisecond timestamp.</summary>
        public long Timestamp;
    }
}
