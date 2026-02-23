using System;
using CurlingSimulator.Core;

namespace CurlingSimulator.Input
{
    /// <summary>
    /// Fully describes a single throw. Serializable so it can be sent over the network later.
    /// </summary>
    [Serializable]
    public struct ThrowData
    {
        /// <summary>Normalized throw power 0–1.</summary>
        public float Power;

        /// <summary>Aim offset in degrees from the sheet centre line. Negative = left, positive = right.</summary>
        public float DirectionAngle;

        /// <summary>Handle rotation direction (determines which way the stone curls).</summary>
        public CurlDirection Curl;

        /// <summary>Team performing this throw.</summary>
        public TeamId Thrower;

        /// <summary>0-based throw index within the current end (0–15).</summary>
        public int ThrowIndex;

        /// <summary>UTC millisecond timestamp — used for network ordering.</summary>
        public long Timestamp;
    }
}
