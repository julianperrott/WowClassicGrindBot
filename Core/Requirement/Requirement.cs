using System;

namespace Core
{
    public static class RequirementExt
    {
        public static Requirement Or(this Requirement f1, Requirement f2)
        {
            return new Requirement
            {
                HasRequirement = () => f1.HasRequirement() || f2.HasRequirement(),
                LogMessage = () => string.IsNullOrEmpty(f1.LogMessage()) ?
                f2.LogMessage() :
                string.Join(" or ", f1.LogMessage(), f2.LogMessage())
            };
        }

        public static Requirement And(this Requirement f1, Requirement f2)
        {
            return new Requirement
            {
                HasRequirement = () => f1.HasRequirement() && f2.HasRequirement(),
                LogMessage = () => string.IsNullOrEmpty(f1.LogMessage()) ?
                f2.LogMessage() :
                string.Join(" and ", f1.LogMessage(), f2.LogMessage())
            };
        }

        public static Requirement Negate(this Requirement f, string keyword)
        {
            return new Requirement
            {
                HasRequirement = () => !f.HasRequirement(),
                LogMessage = () => $"{keyword}{f.LogMessage()}"
            };
        }
    }

    public class Requirement
    {
        public Func<bool> HasRequirement { get; set; } = () => false;
        public Func<string> LogMessage { get; set; } = () => "Unknown requirement";
        public bool VisibleIfHasRequirement { get; set; } = true;
    }
}