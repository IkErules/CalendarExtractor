using System;
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
        public string ClientSecret { get; set; } 

        [Required(AllowEmptyStrings = false)]
        [GuidValidator]
        public string TenantId { get; set; } 

        [Required(AllowEmptyStrings = false)]
        public string CalendarId { get; set; }

        [Required]
        public DateTime StartTime { get; set; } = DateTime.Now;

        [Required]
        public DateTime EndTime { get; set; } = DateTime.Now.AddHours(1);
    }
}
