// Designed by KINEMATION, 2024.

using KINEMATION.KAnimationCore.Runtime.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Demo.Scripts.Runtime
{
    public class MainMenu : MonoBehaviour
    {
        [SerializeField] private PlayerInput playerInput;
        [SerializeField, Range(0f, 600f)] private float panelWidth = 200;
        [SerializeField, Min(0f)] private float drawSpeed = 1f;
        [SerializeField] private EaseMode easing;
        
        [SerializeField] private RectTransform panel;
        private bool _showMenu;
        private float _panelPlayback;
        
        private void Update()
        {
            _panelPlayback = Mathf.Clamp01(_panelPlayback + (_showMenu ? 1f : -1f) * Time.deltaTime * drawSpeed);
            float width = KCurves.Ease(-panelWidth, -1f, _panelPlayback, easing);
            panel.anchoredPosition = new Vector2(width, 0f);
        }

        public void OpenProductPage()
        {
            Application.OpenURL("https://u3d.as/2XD3");
        }

        public void OpenDiscord()
        {
            Application.OpenURL("https://discord.gg/kinemation-1027338787958816860");
        }

        public void Quit()
        {
            Application.Quit();
        }
        
#if ENABLE_INPUT_SYSTEM
        public void OnToggleMenu()
        {
            _showMenu = !_showMenu;
            Cursor.lockState = _showMenu ? CursorLockMode.Confined : CursorLockMode.Locked;
            Cursor.visible = _showMenu;
            playerInput.enabled = !_showMenu;
        }
#endif
    }
}
