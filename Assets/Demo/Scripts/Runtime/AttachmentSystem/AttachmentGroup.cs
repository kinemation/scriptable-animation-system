// Designed by KINEMATION, 2024.

using KINEMATION.FPSAnimationFramework.Runtime.Core;

using System;
using System.Collections.Generic;

namespace Demo.Scripts.Runtime.AttachmentSystem
{
    [Serializable]
    public class AttachmentGroup<T> where T : BaseAttachment
    {
        public List<T> attachments = new List<T>();
        private int _activeIndex;

        public T GetActiveAttachment()
        {
            return attachments.Count == 0 ? null : attachments[_activeIndex];
        }
        
        public void Initialize(FPSAnimator fpsAnimator)
        {
            if (attachments.Count == 0) return;
            
            // Enable the first attachment in the list.
            
            var attachment = GetActiveAttachment();
            attachment.gameObject.SetActive(true);

            var settings = attachment.attachmentLayerSettings;
            foreach (var setting in settings) fpsAnimator.LinkAnimatorLayer(setting);
        }

        public void CycleAttachments(FPSAnimator fpsAnimator)
        {
            if (attachments.Count == 0) return;
            
            // 1. Hide previous attachment.
            attachments[_activeIndex].gameObject.SetActive(false);
            
            // 2. Increment the current attachment index.
            _activeIndex++;
            _activeIndex = _activeIndex > attachments.Count - 1 ? 0 : _activeIndex;
            
            // 3. Enable the active attachment.
            attachments[_activeIndex].gameObject.SetActive(true);
            
            var settings = attachments[_activeIndex].attachmentLayerSettings;
            foreach (var setting in settings) fpsAnimator.LinkAnimatorLayer(setting);
        }
    }
}