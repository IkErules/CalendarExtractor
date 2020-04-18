using System.ComponentModel.DataAnnotations;
using CalendarExtractor.Web.Client.Data.Validation;

namespace CalendarExtractor.Web.Client.Data
{
    public class AzureRequestModel
    {
        [Required(AllowEmptyStrings = false)]
        [GuidValidator]
        public string ClientId { get; set; } = "816c3a23-7568-40a1-b12a-c65795fcbd92";

        [Required(AllowEmptyStrings = false)]
        public string ClientSecret { get; set; } = "my-g4z-t4.M-zOR8OBBrkjq66=UtK8T1";

        [Required(AllowEmptyStrings = false)]
        [GuidValidator]
        public string TenantId { get; set; } = "0dcc5a15-9225-4982-9557-c7f8d345bd63";

        [Required(AllowEmptyStrings = false)]
        public string CalendarId { get; set; } = "meetingroombig@flotha.onmicrosoft.com";

        [Required]
        public int Minutes { get; set; }
    }
}
