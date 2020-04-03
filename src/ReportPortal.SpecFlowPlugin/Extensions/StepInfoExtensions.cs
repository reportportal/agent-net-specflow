using System;
using TechTalk.SpecFlow;

namespace ReportPortal.SpecFlowPlugin.Extensions
{
    public static class StepInfoExtensions
    {
        public static string GetFullText(this StepInfo stepInfo)
        {
            var fullText = "";

            if (stepInfo.StepInstance.MultilineTextArgument != null)
            {
                fullText += Environment.NewLine + stepInfo.StepInstance.MultilineTextArgument;
            }

            if (stepInfo.StepInstance.TableArgument != null)
            {
                fullText += Environment.NewLine + stepInfo.StepInstance.TableArgument;
            }

            return fullText;
        }

        public static string GetCaption(this StepInfo stepInfo)
        {
            var caption = stepInfo.StepInstance.StepDefinitionKeyword + " " + stepInfo.StepInstance.Text;

            return caption;
        }
    }
}
