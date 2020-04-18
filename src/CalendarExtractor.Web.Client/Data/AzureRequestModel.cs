using System.ComponentModel.DataAnnotations;
using CalendarExtractor.Web.Client.Data.Validation;

namespace CalendarExtractor.Web.Client.Data
{
    public class AzureRequestModel
    {
        [Required(AllowEmptyStrings = false)]
        [GuidValidator]
        public string ClientId { get; set; }

        [Required(AllowEmptyStrings = false)]
        [GuidValidator]
        public string ClientSecret { get; set; }

        [Required(AllowEmptyStrings = false)]
        [GuidValidator]
        public string TenantId { get; set; }

        [Required(AllowEmptyStrings = false)]
        public string CalendarId { get; set; }
    }
}
