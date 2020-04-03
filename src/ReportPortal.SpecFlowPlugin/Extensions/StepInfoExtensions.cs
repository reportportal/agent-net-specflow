using System;
using System.Linq;
using TechTalk.SpecFlow;

namespace ReportPortal.SpecFlowPlugin.Extensions
{
    public static class StepInfoExtensions
    {
        public static string GetFormattedParameters(this StepInfo stepInfo)
        {
            var fullText = "";

            if (stepInfo.StepInstance.MultilineTextArgument != null)
            {
                fullText = stepInfo.StepInstance.MultilineTextArgument;
            }
            // format table
            else if (stepInfo.StepInstance.TableArgument != null)
            {
                fullText = "| **" + string.Join("** | **", stepInfo.StepInstance.TableArgument.Header) + "** |";
                fullText += Environment.NewLine + "| " + string.Join(" | ", stepInfo.StepInstance.TableArgument.Header.Select(c => "---")) + " |";

                foreach (var row in stepInfo.StepInstance.TableArgument.Rows)
                {
                    fullText += Environment.NewLine + "| " + string.Join(" | ", row.Values) + " |";
                }
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
