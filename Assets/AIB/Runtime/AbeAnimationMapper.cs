using UnityEngine;

namespace AIB
{
    public static class AbeAnimationMapper
    {
        public static readonly int SpeedParam = Animator.StringToHash("Speed");
        public static readonly int IsMovingParam = Animator.StringToHash("IsMoving");
        public static readonly int IsBackwardParam = Animator.StringToHash("IsBackward");
        public static readonly int HealthNormalizedParam = Animator.StringToHash("HealthNormalized");
        public static readonly int PhaseParam = Animator.StringToHash("Phase");
        public static readonly int AlertnessParam = Animator.StringToHash("Alertness");
        public static readonly int CuriosityParam = Animator.StringToHash("Curiosity");

        public static void ApplyState(Animator animator, AbeStatePayload state)
        {
            if (animator == null || state == null) return;

            bool isMoving = state.currentActionForward != 0 || state.currentActionRotate != 0;
            bool isBackward = state.currentActionForward == 2;
            
            float speed = 0f;
            if (isMoving)
            {
                speed = 0.5f; // Default walk speed
                
                // High alertness -> Running
                if (state.alertness > 0.7f && !isBackward)
                {
                    speed = 1.0f;
                }
            }

            animator.SetBool(IsMovingParam, isMoving);
            animator.SetBool(IsBackwardParam, isBackward);
            animator.SetFloat(SpeedParam, speed);

            // Health mapping (0 to 1)
            // health > 70 -> normal (1.0)
            // 30 < health <= 70 -> elderly/shaky (0.5)
            // health <= 30 -> stumble/unsteady (0.0)
            float healthNormalized = Mathf.Clamp01(state.health / 100f);
            
            // Phase mapping overrides
            if (state.phase == "POST-BIRTH" && state.tick < 1000)
            {
                // Force unsteady walk for early post-birth
                healthNormalized = 0.0f;
            }
            
            animator.SetFloat(HealthNormalizedParam, healthNormalized);

            // Phase mapping
            int phaseHash = Animator.StringToHash(state.phase);
            animator.SetInteger(PhaseParam, phaseHash);
            // AIB-IsPreGABA-AUTOPATCH 2026-05-07: pre-GABA global wave for tick<1000.
            animator.SetBool("IsPreGABA", state.tick < 1000);

            // Neurochemical mapping
            animator.SetFloat(AlertnessParam, state.alertness);
            animator.SetFloat(CuriosityParam, state.curiosity);
        }
    }
}
