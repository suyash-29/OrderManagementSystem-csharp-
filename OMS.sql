CREATE DATABASE OMS
use OMS

CREATE TABLE Users (
    userId INT PRIMARY KEY IDENTITY,
    username NVARCHAR(50) NOT NULL,
    password NVARCHAR(50) NOT NULL,
    role NVARCHAR(10) CHECK (role IN ('Admin', 'User')) NOT NULL
);

CREATE TABLE Products (
    productId INT PRIMARY KEY IDENTITY,
    productName NVARCHAR(100) NOT NULL,
    description NVARCHAR(255),
    price DECIMAL(10, 2) NOT NULL,
    quantityInStock INT NOT NULL,
    type NVARCHAR(20) CHECK (type IN ('Electronics', 'Clothing')) NOT NULL
);

CREATE TABLE Orders (
    orderId INT PRIMARY KEY IDENTITY,
    userId INT FOREIGN KEY REFERENCES Users(userId),
    orderDate DATETIME DEFAULT GETDATE()
    TotalPrice DECIMAL(10, 2);
);

CREATE TABLE OrderProducts (
    orderProductId INT PRIMARY KEY IDENTITY,
    orderId INT FOREIGN KEY REFERENCES Orders(orderId) ON DELETE CASCADE,
    productId INT FOREIGN KEY REFERENCES Products(productId),
    quantity INT NOT NULL
);

CREATE TABLE Electronics (
    ProductId INT PRIMARY KEY,
    Brand VARCHAR(50),
    WarrantyPeriod INT,
    FOREIGN KEY (ProductId) REFERENCES Products(ProductId) ON DELETE CASCADE
);

CREATE TABLE Clothing (
    ProductId INT PRIMARY KEY,
    Size VARCHAR(10),
    Color VARCHAR(30),
    FOREIGN KEY (ProductId) REFERENCES Products(ProductId) ON DELETE CASCADE
);




SELECT*from Products
SELECT*from Users
SELECT*from Orders
SELECT*from OrderProducts
SELECT * from Clothing
SELECT * from Electronics
