using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.ViewModels
{
    public class ShippingDto
    {
        public string ShippingState { get; set; }
        public string TrackingNumber { get; set; }
        public DateTime EstimatedDeliveryDate { get; set; }
        public DateTime? ActualDeliveryDate { get; set; }
    }
}
