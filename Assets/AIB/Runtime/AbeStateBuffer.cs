using System;

namespace AIB
{
    public class AbeStateBuffer
    {
        private static readonly Lazy<AbeStateBuffer> _instance = new Lazy<AbeStateBuffer>(() => new AbeStateBuffer());
        public static AbeStateBuffer Instance => _instance.Value;

        private AbeStatePayload _frontBuffer;
        private AbeStatePayload _backBuffer;
        private readonly object _lock = new object();
        private bool _isDirty = false;

        public bool IsConnected { get; set; }
        public string ConnectionStatus { get; set; } = "Connecting...";

        public AbeStatePayload CurrentState => _frontBuffer;

        private AbeStateBuffer()
        {
            _frontBuffer = AbeStatePayload.Default();
            _backBuffer = AbeStatePayload.Default();
        }

        public void Write(AbeStatePayload payload)
        {
            lock (_lock)
            {
                // Copy values to back buffer
                _backBuffer.posX = payload.posX;
                _backBuffer.posY = payload.posY;
                _backBuffer.posZ = payload.posZ;
                _backBuffer.rotationY = payload.rotationY;
                
                _backBuffer.currentActionForward = payload.currentActionForward;
                _backBuffer.currentActionRotate = payload.currentActionRotate;
                
                _backBuffer.health = payload.health;
                _backBuffer.deaths = payload.deaths;
                _backBuffer.episode = payload.episode;
                _backBuffer.lavaDistance = payload.lavaDistance;
                _backBuffer.lavaDistanceDelta = payload.lavaDistanceDelta;
                
                _backBuffer.dopamine = payload.dopamine;
                _backBuffer.cortisol = payload.cortisol;
                _backBuffer.oxytocin = payload.oxytocin;
                _backBuffer.serotonin = payload.serotonin;
                _backBuffer.norepinephrine = payload.norepinephrine;
                _backBuffer.endorphins = payload.endorphins;
                
                _backBuffer.curiosity = payload.curiosity;
                _backBuffer.stress = payload.stress;
                _backBuffer.plasticity = payload.plasticity;
                _backBuffer.alertness = payload.alertness;
                _backBuffer.focus = payload.focus;
                _backBuffer.inhibition = payload.inhibition;
                _backBuffer.bonding = payload.bonding;
                
                _backBuffer.predictionError = payload.predictionError;
                _backBuffer.rewardThisTick = payload.rewardThisTick;
                _backBuffer.naturalReward = payload.naturalReward;
                _backBuffer.shapedReward = payload.shapedReward;
                
                _backBuffer.tick = payload.tick;
                _backBuffer.phase = payload.phase;

                _isDirty = true;
            }
        }

        public void SwapBuffers()
        {
            if (!_isDirty) return;

            lock (_lock)
            {
                // Swap references
                AbeStatePayload temp = _frontBuffer;
                _frontBuffer = _backBuffer;
                _backBuffer = temp;
                
                _isDirty = false;
            }
        }
    }
}