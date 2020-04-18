using System;
using System.ComponentModel.DataAnnotations;

namespace CalendarExtractor.Web.Client.Data.Validation
{
    public class GuidValidator : ValidationAttribute
    {
        public const string DefaultErrorMessage = "{0} not a valid GUID";
        public GuidValidator() : base(DefaultErrorMessage) { }

        public override bool IsValid(object value)
        {
            if (value == null)
            {
                return false;
            }

            if (value is string input)
            {
                if (Guid.TryParse(input, out var _))
                    return true;
            }

            return false;
        }
    }
}
