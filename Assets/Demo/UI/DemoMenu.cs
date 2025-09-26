using UnityEngine;

namespace Demo.UI
{
    public class DemoMenu : MonoBehaviour
    {
        public GameObject controlsMenu;
        private bool _controlsEnabled;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                _controlsEnabled = !_controlsEnabled;
                controlsMenu.SetActive(_controlsEnabled);
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Application.Quit();
            }
        }
    }
}