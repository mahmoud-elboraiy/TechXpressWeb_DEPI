using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Models
{
    public class OrderItem
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        // Done
        public Order Order { get; set; }

        public int ProductId { get; set; }
        // Done 
        public Product Product { get; set; }

        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal TotalItemsPrice { get; set; }
    }

}
