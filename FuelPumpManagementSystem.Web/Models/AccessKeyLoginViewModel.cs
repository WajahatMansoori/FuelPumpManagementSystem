using System.ComponentModel.DataAnnotations;

namespace FuelPumpManagementSystem.Web.Models
{
    public class AccessKeyLoginViewModel
    {
        [Required(ErrorMessage = "Access key is required")]
        public string AccessKey { get; set; } = string.Empty;
    }
}
