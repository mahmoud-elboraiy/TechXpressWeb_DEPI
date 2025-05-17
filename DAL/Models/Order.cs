using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    public class Order
    {
        public Order()
        {
            OrderItems = new List<OrderItem>();
        }

        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }
        // Done
        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.Now;
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }
        [Required]
        public string Status { get; set; }
        // Done
        public ICollection<OrderItem> OrderItems { get; set; }
        // Done
        public Payment Payment { get; set; }
        // Done
        public Shipping Shipping { get; set; }
    }
}
