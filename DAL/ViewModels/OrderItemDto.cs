using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.ViewModels
{
    public class OrderItemDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }  // Assuming Product.Name exists
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal TotalItemsPrice { get; set; }
    }

}
