using System;
using UnityEngine;

namespace AIB
{
    /// <summary>
    /// The single source of truth for every field transmitted from the experiment build
    /// to each connected observer client once per tick.
    /// Serializes to / from JSON via Unity's JsonUtility.
    /// </summary>
    [Serializable]
    public class AbeStatePayload
    {
        // ── Transform ──────────────────────────────────────────────
        public float posX;
        public float posY;
        public float posZ;
        public float rotationY;

        // ── Action ─────────────────────────────────────────────────
        /// <summary>0 = none, 1 = forward, 2 = backward</summary>
        public int currentActionForward;
        /// <summary>0 = none, 1 = right, 2 = left</summary>
        public int currentActionRotate;

        // ── Vitals ─────────────────────────────────────────────────
        public float health;
        public int deaths;
        public int episode;
        public float lavaDistance;
        public float lavaDistanceDelta;

        // ── Neurochemicals ─────────────────────────────────────────
        public float dopamine;
        public float cortisol;
        public float oxytocin;
        public float serotonin;
        public float norepinephrine;
        public float endorphins;

        // ── Signals ────────────────────────────────────────────────
        public float curiosity;
        public float stress;
        public float plasticity;
        public float alertness;
        public float focus;
        public float inhibition;
        public float bonding;

        // ── Rewards ────────────────────────────────────────────────
        public float predictionError;
        public float rewardThisTick;
        public float naturalReward;
        public float shapedReward;

        // ── Mother ──────────────────────────────────────────────────
        public float motherStrength;

        // ── Meta ───────────────────────────────────────────────────
        public int tick;
        public string phase; // "WOMB", "POST-BIRTH", etc.

        // ── Helpers ────────────────────────────────────────────────

        public Vector3 Position
        {
            get => new Vector3(posX, posY, posZ);
            set { posX = value.x; posY = value.y; posZ = value.z; }
        }

        public string ToJson()
        {
            return JsonUtility.ToJson(this);
        }

        public static AbeStatePayload FromJson(string json)
        {
            return JsonUtility.FromJson<AbeStatePayload>(json);
        }

        /// <summary>
        /// Returns a payload pre-filled with safe defaults (zeros / empty strings)
        /// so the HUD can render even before the first real tick arrives.
        /// </summary>
        public static AbeStatePayload Default()
        {
            return new AbeStatePayload
            {
                health = 100f,
                phase = "UNKNOWN",
                tick = 0
            };
        }
    }
}
