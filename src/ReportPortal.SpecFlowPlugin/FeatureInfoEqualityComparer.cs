using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TechTalk.SpecFlow;

namespace ReportPortal.SpecFlowPlugin
{
    internal class FeatureInfoEqualityComparer : IEqualityComparer<FeatureInfo>
    {
        public bool Equals(FeatureInfo x, FeatureInfo y)
        {
            if (x == null && y == null)
            {
                return true;
            }

            if (x?.Title == y?.Title
                && x?.Description == y?.Description
                && x?.GenerationTargetLanguage == y?.GenerationTargetLanguage
                && x.Tags.SequenceEqual(y.Tags))
            {
                return true;
            }

            return false;
        }

        public int GetHashCode(FeatureInfo obj)
        {
            return GetFeatureInfoHashCode(obj);
        }

        public static int GetFeatureInfoHashCode(FeatureInfo obj)
        {
            return obj.Title.GetHashCode()
                   ^ obj.Description.GetHashCode()
                   ^ obj.GenerationTargetLanguage.GetHashCode()
                   ^ obj.Language.DisplayName.GetHashCode()
                   ^ ((IStructuralEquatable)obj.Tags).GetHashCode(EqualityComparer<string>.Default);
        }
    }
}