// Copyright (c) 2026 KINEMATION.
// All rights reserved.

namespace KINEMATION.Shared.KAnimationCore.Editor.Tools
{
    public interface IEditorTool
    {
        public void Init();
        public void Render();
        public string GetToolCategory();
        public string GetToolName();
        public string GetDocsURL();
        public string GetToolDescription();
    }
}