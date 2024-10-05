using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using OrderManagementSystem.dao;
using OrderManagementSystem.entity;
using OrderManagementSystem.exception;

namespace OrderManagementSystem.main
{
    class MainModule
    {
        static void Main(string[] args)
        {
            IOrderManagementRepository orderProcessor = new OrderProcessor();

            while (true)
            {
                Console.WriteLine("\nOrder Management System Menu:");
                Console.WriteLine("1. Create User");
                Console.WriteLine("2. Create Product");
                Console.WriteLine("3. Create Order");
                Console.WriteLine("4. Cancel Order");
                Console.WriteLine("5. View All Products");
                Console.WriteLine("6. View Orders by User");
                Console.WriteLine("7. Exit");

                Console.Write("Enter your choice: ");
                int choice = int.Parse(Console.ReadLine());

                switch (choice)
                {
                    case 1:
                        CreateUser(orderProcessor);
                        break;
                    case 2:
                        CreateProduct(orderProcessor);
                        break;
                    case 3:
                        CreateOrder(orderProcessor);
                        break;
                    case 4:
                        CancelOrder(orderProcessor);
                        break;
                    case 5:
                        ViewAllProducts(orderProcessor);
                        break;
                    case 6:
                        ViewOrdersByUser(orderProcessor);
                        break;
                    case 7:
                        return;
                }
            }
        }

        private static void CreateUser(IOrderManagementRepository orderProcessor)
        {
            Console.Write("Enter Username: ");
            string username = Console.ReadLine();
            Console.Write("Enter Password: ");
            string password = Console.ReadLine();
            Console.Write("Enter Role (Admin/User): ");
            string role = Console.ReadLine();

            User newUser = new User { Username = username, Password = password, Role = role };

            if (orderProcessor.IsUserExists(newUser))
            {
                Console.WriteLine("Error: User with the same username already exists.");
                return;
            }

            orderProcessor.CreateUser(newUser);
            Console.WriteLine("User created successfully.");
        }


        private static void CreateProduct(IOrderManagementRepository orderProcessor)
        {
            Console.Write("Enter Admin Username: ");
            string adminUsername = Console.ReadLine();
            Console.Write("Enter Admin Password: ");
            string adminPassword = Console.ReadLine();

            User admin = orderProcessor.GetUserByUsername(adminUsername);

            // Checking if the user exists
            if (admin == null)
            {
                Console.WriteLine("Error: User does not exist.");
                return;
            }

            // Checkign if the user is an admin
            if (admin.Role != "Admin")
            {
                Console.WriteLine("Error: Only Admin users can create products.");
                return;
            }

            // Validating password
            if (admin.Password != adminPassword)
            {
                Console.WriteLine("Error: Incorrect password.");
                return;
            }

            // Collect product details
            Console.Write("Enter Product Name: ");
            string productName = Console.ReadLine();
            Console.Write("Enter Description: ");
            string description = Console.ReadLine();
            Console.Write("Enter Price: ");
            double price = double.Parse(Console.ReadLine());
            Console.Write("Enter Quantity in Stock: ");
            int quantityInStock = int.Parse(Console.ReadLine());
            Console.Write("Enter Product Type (Electronics/Clothing): ");
            string type = Console.ReadLine();

            if (type == "Electronics")
            {
                Console.Write("Enter Brand: ");
                string brand = Console.ReadLine();
                Console.Write("Enter Warranty Period (months): ");
                int warrantyPeriod = int.Parse(Console.ReadLine());

                Electronics electronics = new Electronics(0, productName, description, price, quantityInStock, brand, warrantyPeriod);
                orderProcessor.CreateProduct(admin, electronics);
            }
            else if (type == "Clothing")
            {
                Console.Write("Enter Size: ");
                string size = Console.ReadLine();
                Console.Write("Enter Color: ");
                string color = Console.ReadLine();

                Clothing clothing = new Clothing(0, productName, description, price, quantityInStock, size, color);
                orderProcessor.CreateProduct(admin, clothing);
            }

            Console.WriteLine("Product created successfully.");
        }


        private static void CreateOrder(IOrderManagementRepository orderProcessor)
        {
            Console.Write("Enter User ID: ");
            int userId = int.Parse(Console.ReadLine());

            User user = new User { UserId = userId };

            List<(Product product, int quantity)> orderItems = new List<(Product product, int quantity)>();
            while (true)
            {
                Console.Write("Enter Product ID to add to the order (or -1 to finish): ");
                int productId = int.Parse(Console.ReadLine());
                if (productId == -1) break;

                Console.Write("Enter Quantity: ");
                int quantity = int.Parse(Console.ReadLine());

                Product product = new Product { ProductId = productId };
                orderItems.Add((product, quantity));
            }

            orderProcessor.CreateOrder(user, orderItems);
        }



        private static void CancelOrder(IOrderManagementRepository orderProcessor)
        {
            Console.Write("Enter User ID: ");
            int userId = int.Parse(Console.ReadLine());
            Console.Write("Enter Order ID: ");
            int orderId = int.Parse(Console.ReadLine());

            try
            {
                orderProcessor.CancelOrder(userId, orderId);
                Console.WriteLine("Order cancelled successfully.");
            }
            catch (OrderNotFoundException e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private static void ViewAllProducts(IOrderManagementRepository orderProcessor)
        {
            List<Product> products = orderProcessor.GetAllProducts();
            foreach (var product in products)
            {
                
                 Console.WriteLine($"ProductID:{product.ProductId},Name: {product.ProductName} ,Description {product.Description}, Price: {product.Price},Stock: {product.QuantityInStock} , Type: {product.Type}");
            }
        }

        private static void ViewOrdersByUser(IOrderManagementRepository orderProcessor)
        {
            Console.Write("Enter User ID: ");
            int userId = int.Parse(Console.ReadLine());

            User user = new User { UserId = userId };

            // Geting all orders for the user
            List<Order> orders = orderProcessor.GetOrderByUser(user);
            if (orders.Count > 0)
            {
                Console.WriteLine($"Orders for User ID {userId}:");
                foreach (var order in orders)
                {
                    Console.WriteLine($"Order ID: {order.OrderId}, Total Price: {order.TotalPrice:C}");
                    Console.WriteLine("Products Ordered:");
                    foreach (var item in order.OrderItems)
                    {
                        // getting product details
                        Product product = orderProcessor.GetProductById(item.ProductId); // Call to your existing method
                        if (product != null)
                        {
                            Console.WriteLine($" - ProductID: {product.ProductId}, Name: {product.ProductName}, Description: {product.Description}, Price: {product.Price:C}, Type: {product.Type}, Quantity: {item.Quantity}");
                        }
                        else
                        {
                            Console.WriteLine($" - Product with ID {item.ProductId} not found.");
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("No orders found for this user.");
            }
        }



    }
}

