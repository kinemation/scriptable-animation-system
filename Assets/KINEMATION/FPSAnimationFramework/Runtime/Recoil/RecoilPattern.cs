// Designed by KINEMATION, 2024.

using KINEMATION.Shared.KAnimationCore.Runtime.Attributes;
using KINEMATION.Shared.KAnimationCore.Runtime.Core;
using KINEMATION.Shared.KAnimationCore.Runtime.Input;
using UnityEngine;

namespace KINEMATION.FPSAnimationFramework.Runtime.Recoil
{
    [HelpURL("https://kinemation.gitbook.io/scriptable-animation-system/recoil-system/recoil-pattern")]
    public class RecoilPattern : MonoBehaviour
    {
        [SerializeField] private RecoilPatternSettings recoilSettings;
        [SerializeField] [InputProperty] private string deltaLookInputProperty;
        
        private UserInputController _userInputController;
        
        private Vector2 _compensation;
        private Vector2 _targetRecoil;
        private Vector2 _recoil;
        private Vector2 _cachedRecoil;
        private Vector2 _accumulatedRecoil;
        private bool _isFiring;

        private int _deltaLookInputPropertyIndex;

        private float Compensate(float recoil, float compensation)
        {
            float multiplier = 1f;
            bool isOpposite = recoil * compensation <= 0f;
	
            if(!Mathf.Approximately(compensation, 0f) && isOpposite)
            {
                multiplier -= Mathf.Clamp01(Mathf.Abs(compensation / recoil));
            }
            
            return multiplier;
        }

        private void Start()
        {
            _userInputController = GetComponent<UserInputController>();
            _deltaLookInputPropertyIndex = _userInputController.GetPropertyIndex(deltaLookInputProperty);
        }

        private void Update()
        {
            if (recoilSettings == null) return;
            
            if (_isFiring)
            {
                // Accumulate player delta input when firing.
                Vector4 deltaInput = _userInputController.GetValue<Vector4>(_deltaLookInputPropertyIndex);
                _compensation.x += deltaInput.x;
                _compensation.y += deltaInput.y;
            }
            
            float alpha = KMath.ExpDecayAlpha(recoilSettings.horizontalSmoothing, Time.deltaTime);
            _recoil.x = Mathf.Lerp(_recoil.x, _targetRecoil.x, alpha);
            
            alpha = KMath.ExpDecayAlpha(recoilSettings.verticalSmoothing, Time.deltaTime);
            _recoil.y = Mathf.Lerp(_recoil.y, _targetRecoil.y, alpha);

            if (!_isFiring)
            {
                alpha = KMath.ExpDecayAlpha(recoilSettings.damping, Time.deltaTime);
                _targetRecoil = Vector2.Lerp(_targetRecoil, Vector2.zero, alpha);
            }
            
            _accumulatedRecoil = _recoil - _cachedRecoil;
            _cachedRecoil = _recoil;
        }

        public void OnFireStart()
        {
            if (recoilSettings == null) return;
            
            if (!_isFiring)
            {
                _compensation = _accumulatedRecoil = Vector2.zero;
            }
            
            _isFiring = true;
            _targetRecoil.x += Random.Range(recoilSettings.horizontalRecoil.x, recoilSettings.horizontalRecoil.y);
            _targetRecoil.y += Random.Range(recoilSettings.verticalRecoil.x, recoilSettings.verticalRecoil.y);
        }

        public void OnFireEnd()
        {
            _isFiring = false;
            
            _recoil.x *= Compensate(_recoil.x, _compensation.x);
            _recoil.y *= Compensate(_recoil.y, _compensation.y);
            _cachedRecoil = _recoil;
            _targetRecoil = _recoil;
        }

        public void Init(RecoilPatternSettings settings)
        {
            recoilSettings = settings;
            _compensation = _accumulatedRecoil = _targetRecoil = _cachedRecoil = _recoil = Vector2.zero;
        }

        public Vector2 GetRecoilDelta()
        {
            return _accumulatedRecoil;
        }
    }
}