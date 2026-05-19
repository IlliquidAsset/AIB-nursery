using UnityEngine;

namespace AIB
{
    [RequireComponent(typeof(Animator))]
    public class AbeLookAtIK : MonoBehaviour
    {
        public Transform lookTarget;
        
        private Animator _animator;
        private float _lookWeight = 0f;
        private float _targetLookWeight = 0f;
        private Vector3 _currentLookPosition;

        private void Start()
        {
            _animator = GetComponent<Animator>();
            _currentLookPosition = transform.position + transform.forward * 5f;
        }

        public void SetLookWeight(float weight)
        {
            _targetLookWeight = Mathf.Clamp01(weight);
        }

        private void Update()
        {
            _lookWeight = Mathf.Lerp(_lookWeight, _targetLookWeight, Time.deltaTime * 5f);
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (_animator == null) return;

            Vector3 targetPos;
            if (lookTarget != null)
            {
                targetPos = lookTarget.position;
            }
            else
            {
                // Default behavior: look toward movement direction
                targetPos = transform.position + transform.forward * 5f;
            }

            _currentLookPosition = Vector3.Lerp(_currentLookPosition, targetPos, Time.deltaTime * 5f);

            // bodyWeight: 0.2, headWeight: 0.6, eyesWeight: 0.8, clampWeight: 0.5
            _animator.SetLookAtWeight(_lookWeight, 0.2f, 0.6f, 0.8f, 0.5f);
            _animator.SetLookAtPosition(_currentLookPosition);
        }
    }
}
