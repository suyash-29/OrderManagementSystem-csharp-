using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System;
using System.Collections.Generic;
using OrderManagementSystem.entity;

namespace OrderManagementSystem.entity
{
    public class Order
    {
        public int OrderId { get; set; }
        public int UserId { get; set; }
        public double TotalPrice { get; set; }
        public List<OrderItem> OrderItems { get; set; }

        public Order()
        {
            OrderItems = new List<OrderItem>();
        }
    }

    public class OrderItem
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }

}


