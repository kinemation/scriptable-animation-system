using System.Collections.Generic;
using KINEMATION.Shared.KAnimationCore.Editor.Tools;

namespace KINEMATION.FPSAnimationFramework.Editor.Tools
{
    public class FPSAnimationDemoContent : DemoDownloaderTool
    {
        protected override string GetPackageUrl()
        {
            return "https://github.com/kinemation/demoes/releases/download/fpsaf/FPSAnimationFramework_Demo.unitypackage";
        }

        protected override string GetPackageFileName()
        {
            return "FPSAnimationFramework_Demo";
        }

        protected override List<ContentLicense> GetContentLicenses()
        {
            return new List<ContentLicense>()
            {
                new ContentLicense()
                {
                    contentName = "Animations, Models",
                    tags = new List<Tag>()
                    {
                        new Tag("Mixamo"),
                        new Tag("Adobe License")
                    }
                }
            };
        }

        public override string GetToolName()
        {
            return "FPS Animation Framework";
        }

        public override string GetToolDescription()
        {
            return "Animations, weapon models and examples.";
        }
    }
}