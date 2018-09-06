using System;
using TechTalk.SpecFlow;

namespace ReportPortal.SpecFlowPlugin.Extensions
{
    public static class StepInfoExtensions
    {
        public static string GetFullText(this StepInfo stepInfo)
        {
            var fullText = stepInfo.StepDefinitionType + " " + stepInfo.Text;

            if (stepInfo.MultilineText != null)
            {
                fullText += Environment.NewLine + stepInfo.MultilineText;
            }

            if (stepInfo.Table != null)
            {
                fullText += Environment.NewLine + stepInfo.Table;
            }

            return fullText;
        }
    }
}
