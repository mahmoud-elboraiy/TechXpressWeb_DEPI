using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Models
{
    public class Shipping
    {
        public int Id { get; set; }

        public int OrderId { get; set; }
        // Done
        public Order Order { get; set; }

        public string ShippingState { get; set; }
        public string TrackingNumber { get; set; }
        public DateTime EstimatedDeliveryDate { get; set; }
        public DateTime? ActualDeliveryDate { get; set; }
    }

}
