using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace DAL.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(50)]
        public string FirstName { get; set; }

        [Required]
        [StringLength(50)]
        public string LastName { get; set; }

        public string Address { get; set; }

        public string AccountStatus { get; set; } = "Active"; // Active, Suspended, Banned
        public DateTime? SuspensionEndDate { get; set; }
        public string SuspensionReason { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;

        public ICollection<Order> Orders { get; set; }
        public Cart Cart { get; set; }
        public ICollection<Review> Reviews { get; set; }
    }
}
