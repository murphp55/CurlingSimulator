using System;
using UnityEngine;

namespace CurlingSimulator.Core
{
    [Serializable]
    public struct StoneState
    {
        // Position on the XZ plane in sheet-local coordinates (origin = centre of far house button)
        public Vector2 Position;
        public Vector2 Velocity;
        public float   AngularProgress; // visual spin, not gameplay-relevant
        public TeamId  Owner;
        public int     StoneIndex;      // 0–7 per team
        public bool    IsMoving;
        public bool    IsInPlay;        // false once it crosses a boundary or passes the hog line short
    }
}
