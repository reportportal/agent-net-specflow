using ReportPortal.SpecFlowPlugin.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Infrastructure;

namespace ReportPortal.SpecFlowPlugin
{
    public class SkippedStepsHandler : ISkippedStepHandler
    {
        public void Handle(ScenarioContext scenarioContext)
        {
            var scenarioReporter = ReportPortalAddin.GetScenarioTestReporter(scenarioContext);

            var skippedStepReporter = scenarioReporter.StartChildTestReporter(new Client.Abstractions.Requests.StartTestItemRequest
            {
                Name = scenarioContext.StepContext.StepInfo.GetCaption(),
                StartTime = DateTime.UtcNow,
                Type = Client.Abstractions.Models.TestItemType.Step,
                HasStats = false
            });
            
            skippedStepReporter.Finish(new Client.Abstractions.Requests.FinishTestItemRequest
            {
                EndTime = DateTime.UtcNow,
                Status = Client.Abstractions.Models.Status.Skipped
            });
        }
    }
}
