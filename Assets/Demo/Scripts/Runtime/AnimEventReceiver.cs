// Designed by Kinemation, 2023

using UnityEngine;

namespace Demo.Scripts.Runtime
{
    public class AnimEventReceiver : MonoBehaviour
    {
        [SerializeField] private FPSController controller;

        private void Start()
        {
            if (controller == null)
            {
                controller = GetComponentInParent<FPSController>();
            }
        }
        
        public void SetActionActive(int isActive)
        {
            if(isActive == 0) controller.ResetActionState();
        }

        public void ChangeWeapon()
        {
        }

        public void RefreshStagedState()
        {
            controller.RefreshStagedState();
        }

        public void ResetStagedState()
        {
            controller.ResetStagedState();
        }
    }
}