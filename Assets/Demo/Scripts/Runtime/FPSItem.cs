// Designed by KINEMATION, 2024.

using UnityEngine;

namespace Demo.Scripts.Runtime
{
    public abstract class FPSItem : MonoBehaviour
    {
        public virtual void OnEquip() { }
        
        public virtual void OnUnEquip() { }

        public virtual void OnUnarmedEnabled() { }

        public virtual void OnUnarmedDisabled() { }

        public virtual bool OnAimPressed() { return false; }

        public virtual bool OnAimReleased() { return false; }

        public virtual bool OnFirePressed() { return false; }

        public virtual bool OnFireReleased() { return false; }

        public virtual bool OnReload() { return false; }

        public virtual bool OnGrenadeThrow() { return false; }

        public virtual void OnCycleScope() { }

        public virtual void OnChangeFireMode() { }
    }
}