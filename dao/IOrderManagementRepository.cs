using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OrderManagementSystem.entity;

namespace OrderManagementSystem.dao

{
    public interface IOrderManagementRepository
    {
        void CreateUser(User user);
        void CreateProduct(User user, Product product);
        void CancelOrder(int userId, int orderId);
        List<Product> GetAllProducts();
        bool IsUserExists(User user);
        User GetUserById(int userId);

        User GetUserByUsername(string username);

        void CreateOrder(User user, List<(Product product, int quantity)> orderItems);

        

        public List<Order> GetOrderByUser(User user);
        Product GetProductById(int productId);
    }
}

