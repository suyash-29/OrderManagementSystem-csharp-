using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace OrderManagementSystem.exception
{
    public class OrderNotFoundException : System.Exception
    {
        public OrderNotFoundException(string message) : base(message)
        {
        }
    }
}

