using System.ComponentModel.DataAnnotations;

namespace DAL.Models
{
    public class StoreSettings
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string StoreName { get; set; } = "TechXpress";

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string StoreEmail { get; set; } = "info@techxpress.com";

        [Required]
        [Phone]
        [StringLength(20)]
        public string StorePhone { get; set; } = "+1 (555) 123-4567";

        [Required]
        [StringLength(200)]
        public string StoreAddress { get; set; } = "123 Tech Street, Silicon Valley, CA 94043, USA";
    }
} // dasfasdfsdf