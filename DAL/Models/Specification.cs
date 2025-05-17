using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Models
{
    public class Specification
    {
        [Key]
        public int ProductId { get; set; }
        // Done
        public Product Product { get; set; }

        public string RAM { get; set; }
        public string Storage { get; set; }
        public string Display { get; set; }
        public string CPU { get; set; }
        public string GPU { get; set; }
        public string Color { get; set; }
        public string Battery { get; set; }
        public string Camera { get; set; }
    }

}
