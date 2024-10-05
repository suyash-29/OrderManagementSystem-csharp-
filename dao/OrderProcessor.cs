using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



using OrderManagementSystem.entity;
using OrderManagementSystem.exception;
using OrderManagementSystem.util;

namespace OrderManagementSystem.dao
{
    public class OrderProcessor : IOrderManagementRepository
    {
        private static SqlConnection conn;
        public void CreateUser(User user)
        {
            using (SqlConnection conn = DBUtil.GetDBConn())
            {
                string query = "INSERT INTO Users (username, password, role) VALUES (@username, @password, @role)";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@username", user.Username);
                cmd.Parameters.AddWithValue("@password", user.Password);
                cmd.Parameters.AddWithValue("@role", user.Role);
                cmd.ExecuteNonQuery();
            }
        }

        public void CreateProduct(User user, Product product)
        {
            // Checking if the user exists in the database
            User existingUser = GetUserByUsername(user.Username);

            if (existingUser == null)
            {
                throw new Exception("User does not exist.");
            }

            // Checking if the user is  admin
            if (existingUser.Role != "Admin")
            {
                throw new UnauthorizedAccessException("Only Admin users can create products.");
            }

            // Inserting product into the database
            using (SqlConnection conn = DBUtil.GetDBConn())
            {
                string query = "INSERT INTO Products (productName, description, price, quantityInStock, type) " +
                       "VALUES (@productName, @description, @price, @quantityInStock, @type); " +
                       "SELECT SCOPE_IDENTITY();";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@productName", product.ProductName);
                cmd.Parameters.AddWithValue("@description", product.Description);
                cmd.Parameters.AddWithValue("@price", product.Price);
                cmd.Parameters.AddWithValue("@quantityInStock", product.QuantityInStock);
                cmd.Parameters.AddWithValue("@type", product.Type);

                int productId = Convert.ToInt32(cmd.ExecuteScalar());

                // Insertinf into electronics or Clothing table
                if (product is Electronics electronics)
                {
                    string electronicsQuery = "INSERT INTO Electronics (ProductId, Brand, WarrantyPeriod) VALUES (@ProductId, @Brand, @WarrantyPeriod)";
                    SqlCommand electronicsCmd = new SqlCommand(electronicsQuery, conn);
                    electronicsCmd.Parameters.AddWithValue("@ProductId", productId);
                    electronicsCmd.Parameters.AddWithValue("@Brand", electronics.Brand);
                    electronicsCmd.Parameters.AddWithValue("@WarrantyPeriod", electronics.WarrantyPeriod);
                    electronicsCmd.ExecuteNonQuery();
                }
                else if (product is Clothing clothing)
                {
                    string clothingQuery = "INSERT INTO Clothing (ProductId, Size, Color) VALUES (@ProductId, @Size, @Color)";
                    SqlCommand clothingCmd = new SqlCommand(clothingQuery, conn);
                    clothingCmd.Parameters.AddWithValue("@ProductId", productId);
                    clothingCmd.Parameters.AddWithValue("@Size", clothing.Size);
                    clothingCmd.Parameters.AddWithValue("@Color", clothing.Color);
                    clothingCmd.ExecuteNonQuery();
                }
            }
        }


        public void CreateOrder(User user, List<(Product product, int quantity)> orderItems)
        {
            User existingUser = GetUserById(user.UserId);

            if (existingUser == null)
            {
                throw new UserNotFoundException("User not found.");
            }

            double totalPrice = 0.0;
            List<(string ProductName, string ProductType, int Quantity, double Price)> orderedProducts = new List<(string, string, int, double)>();

            using (SqlConnection conn = DBUtil.GetDBConn())
            {
                using (SqlTransaction transaction = conn.BeginTransaction())
                {
                    try
                    {
                        foreach (var item in orderItems)
                        {
                            var product = item.product;
                            int quantity = item.quantity;

                            // checking product is available or not
                            Product existingProduct = GetProductById(product.ProductId);
                            if (existingProduct == null || existingProduct.QuantityInStock < quantity)
                            {
                                throw new Exception($"Product {existingProduct?.ProductName} is not available in the requested quantity: {quantity}.");
                            }

                            // calculating total price
                            double itemTotalPrice = existingProduct.Price * quantity;
                            totalPrice += itemTotalPrice;

                            // stroing order deatils to display 
                            orderedProducts.Add((existingProduct.ProductName, existingProduct.Type, quantity, itemTotalPrice));
                        }

                        // Update product stock and insert the order
                        foreach (var item in orderItems)
                        {
                            var product = item.product;
                            int quantity = item.quantity;

                            // getting existing product  to update the stock
                            Product existingProduct = GetProductById(product.ProductId);

                            // Updating stock
                            existingProduct.QuantityInStock -= quantity;
                            UpdateProduct(existingProduct);
                        }

                        // Inserting ordertables
                        string orderQuery = "INSERT INTO Orders (UserId, TotalPrice) VALUES (@UserId, @TotalPrice); SELECT SCOPE_IDENTITY();";
                        SqlCommand orderCmd = new SqlCommand(orderQuery, conn, transaction);
                        orderCmd.Parameters.AddWithValue("@UserId", existingUser.UserId);
                        orderCmd.Parameters.AddWithValue("@TotalPrice", totalPrice);

                        int orderId = Convert.ToInt32(orderCmd.ExecuteScalar());

                        // Inserting order juunction table
                        foreach (var item in orderItems)
                        {
                            var product = item.product;
                            int quantity = item.quantity;

                            string orderItemQuery = "INSERT INTO OrderProducts (orderId, productId, quantity) VALUES (@OrderId, @ProductId, @Quantity)";
                            SqlCommand orderItemCmd = new SqlCommand(orderItemQuery, conn, transaction);
                            orderItemCmd.Parameters.AddWithValue("@OrderId", orderId);
                            orderItemCmd.Parameters.AddWithValue("@ProductId", product.ProductId);
                            orderItemCmd.Parameters.AddWithValue("@Quantity", quantity);
                            orderItemCmd.ExecuteNonQuery();
                        }

                        transaction.Commit();

                        // Displayorder details
                        Console.WriteLine("Order created successfully.");
                        Console.WriteLine($"Total Price: {totalPrice:C}");
                        Console.WriteLine("Ordered Products:");
                        foreach (var orderedProduct in orderedProducts)
                        {
                            Console.WriteLine($"Name: {orderedProduct.ProductName}, Quantity: {orderedProduct.Quantity}, Product Price: {orderedProduct.Price:C}, ProductType: {orderedProduct.ProductType}");
                        }
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Console.WriteLine($"Error occurred: {ex.Message}"); // Print the exception message to the console
                         // Optionally rethrow the exception if needed
                    }
                }
            }
        }



        public void CancelOrder(int userId, int orderId)
        {
            using (SqlConnection conn = DBUtil.GetDBConn())
            {
                
                using (SqlTransaction transaction = conn.BeginTransaction())
                {
                    try
                    {
                        string orderProductsQuery = "SELECT ProductId, Quantity FROM OrderProducts WHERE OrderId = @orderId";
                        SqlCommand orderProductsCmd = new SqlCommand(orderProductsQuery, conn, transaction);
                        orderProductsCmd.Parameters.AddWithValue("@orderId", orderId);

                        var orderItems = new List<(int ProductId, int Quantity)>();

                        using (SqlDataReader reader = orderProductsCmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int productId = reader.GetInt32(0);
                                int quantity = reader.GetInt32(1);
                                orderItems.Add((productId, quantity));
                            }
                        }

                        // Step 2: Update stock
                        foreach (var item in orderItems)
                        {
                            int productId = item.ProductId;
                            int quantity = item.Quantity;

                            // getting the existing stock to update
                            Product existingProduct = GetProductById(productId);
                            if (existingProduct != null)
                            {
                                
                                existingProduct.QuantityInStock += quantity;

                                // Calling the update
                                UpdateProduct(existingProduct);
                            }
                        }

                        // deleting the order
                        string deleteOrderQuery = "DELETE FROM Orders WHERE UserId = @userId AND OrderId = @orderId";
                        SqlCommand deleteOrderCmd = new SqlCommand(deleteOrderQuery, conn, transaction);
                        deleteOrderCmd.Parameters.AddWithValue("@userId", userId);
                        deleteOrderCmd.Parameters.AddWithValue("@orderId", orderId);
                        int rowsAffected = deleteOrderCmd.ExecuteNonQuery();

                        if (rowsAffected == 0)
                        {
                            throw new OrderNotFoundException("Order not found for this user.");
                        }

                        
                        transaction.Commit();
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }


        public List<Product> GetAllProducts()
        {
            List<Product> products = new List<Product>();

            using (SqlConnection conn = DBUtil.GetDBConn())
            {
                string query = "SELECT * FROM Products";
                SqlCommand cmd = new SqlCommand(query, conn);
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    Product product = new Product()
                    {
                        ProductId = (int)reader["productId"],
                        ProductName = (string)reader["productName"],
                        Description = (string)reader["description"],
                        Price = Convert.ToDouble(reader["price"]),
                        QuantityInStock = (int)reader["quantityInStock"],
                        Type = (string)reader["type"]
                    };
                    products.Add(product);
                }
            }

            return products;
        }

        public List<Order> GetOrderByUser(User user)
        {
            List<Order> orders = new List<Order>();

            using (SqlConnection conn = DBUtil.GetDBConn())
            {
                string query = @"
            SELECT o.OrderId, o.UserId, o.TotalPrice, op.ProductId, op.Quantity 
            FROM Orders o 
            JOIN OrderProducts op ON o.OrderId = op.OrderId 
            WHERE o.UserId = @UserId";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@UserId", user.UserId);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    
                    if (!reader.HasRows)
                    {
                        Console.WriteLine("No orders found for this user.");
                        return orders; 
                    }

                    while (reader.Read())
                    {
                        Order order = new Order
                        {
                            OrderId = reader["OrderId"] is DBNull ? 0 : Convert.ToInt32(reader["OrderId"]),
                            UserId = reader["UserId"] is DBNull ? 0 : Convert.ToInt32(reader["UserId"]),
                            TotalPrice = reader["TotalPrice"] is DBNull ? 0.0 : Convert.ToDouble(reader["TotalPrice"])
                        };

                        // Check for Quantity and ProductId 
                        if (reader["ProductId"] is DBNull || reader["Quantity"] is DBNull)
                        {
                            continue; // Skiping
                        }

                        order.OrderItems.Add(new OrderItem
                        {
                            ProductId = Convert.ToInt32(reader["ProductId"]),
                            Quantity = Convert.ToInt32(reader["Quantity"])
                        });

                        orders.Add(order);
                    }
                }
            }

            return orders;
        }


        public User GetUserByUsername(string username)
        {
            User user = null;
            SqlConnection conn = DBUtil.GetDBConn();

            try
            {
                string query = "SELECT * FROM Users WHERE Username = @Username";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Username", username);

                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    user = new User
                    {
                        UserId = Convert.ToInt32(reader["UserId"]),
                        Username = reader["Username"].ToString(),
                        Password = reader["Password"].ToString(),
                        Role = reader["Role"].ToString()
                    };
                }
                reader.Close();
            }
            finally
            {
                conn.Close();
            }

            return user;
        }

        public User GetUserById(int userId)
        {
            User user = null;
            SqlConnection conn = DBUtil.GetDBConn();

            try
            {
                string query = "SELECT * FROM Users WHERE UserId = @UserId";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@UserId", userId);

                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    user = new User
                    {
                        UserId = Convert.ToInt32(reader["UserId"]),
                        Username = reader["Username"].ToString(),
                        Password = reader["Password"].ToString(),
                        Role = reader["Role"].ToString()
                    };
                }
                reader.Close();
            }
            finally
            {
                conn.Close();
            }

            return user;
        }



        public bool IsAdmin(User user)
        {
            // checking the database to check if the user is an admin
            User dbUser = GetUserByUsername(user.Username);
            return dbUser != null && dbUser.Role == "Admin";
        }

        public bool IsUserExists(User user)
        {
            // chekinh the database to check if a user with the same username already exists
            User existingUser = GetUserByUsername(user.Username);
            return existingUser != null;
        }

        public Product GetProductById(int productId)
        {
            using (SqlConnection conn = DBUtil.GetDBConn())
            {
                 
                string query = "SELECT * FROM Products WHERE ProductId = @ProductId";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ProductId", productId);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        string productName = reader["productName"].ToString();
                        string description = reader["description"].ToString();
                        double price = Convert.ToDouble(reader["price"]);
                        int quantityInStock = Convert.ToInt32(reader["quantityInStock"]);
                        string type = reader["type"].ToString();

                        reader.Close();

                        // swtch for type togetdetails
                        if (type == "Electronics")
                        {
                            string electronicsQuery = "SELECT * FROM Electronics WHERE ProductId = @ProductId";
                            SqlCommand electronicsCmd = new SqlCommand(electronicsQuery, conn);
                            electronicsCmd.Parameters.AddWithValue("@ProductId", productId);

                            using (SqlDataReader electronicsReader = electronicsCmd.ExecuteReader())
                            {
                                if (electronicsReader.Read())
                                {
                                    string brand = electronicsReader["Brand"].ToString();
                                    int warrantyPeriod = Convert.ToInt32(electronicsReader["WarrantyPeriod"]);
                                    return new Electronics(productId, productName, description, price, quantityInStock, brand, warrantyPeriod);
                                }
                            }
                        }
                        else if (type == "Clothing")
                        {
                            string clothingQuery = "SELECT * FROM Clothing WHERE ProductId = @ProductId";
                            SqlCommand clothingCmd = new SqlCommand(clothingQuery, conn);
                            clothingCmd.Parameters.AddWithValue("@ProductId", productId);

                            using (SqlDataReader clothingReader = clothingCmd.ExecuteReader())
                            {
                                if (clothingReader.Read())
                                {
                                    string size = clothingReader["Size"].ToString();
                                    string color = clothingReader["Color"].ToString();
                                    return new Clothing(productId, productName, description, price, quantityInStock, size, color);
                                }
                            }
                        }

                       
                        return new Product(productId, productName, description, price, quantityInStock, type);
                    }
                }
            }

            return null; 
        }


        public void UpdateProduct(Product product)
        {
            using (SqlConnection conn = DBUtil.GetDBConn())
            {
                string query = "UPDATE Products SET quantityInStock = @quantityInStock WHERE ProductId = @ProductId";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@quantityInStock", product.QuantityInStock);
                cmd.Parameters.AddWithValue("@ProductId", product.ProductId);
                cmd.ExecuteNonQuery();
            }
        }


    }
}

