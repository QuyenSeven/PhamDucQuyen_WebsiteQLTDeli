USE master;
IF EXISTS (SELECT * FROM sys.databases WHERE name = 'TDeliteaDB')
BEGIN
    ALTER DATABASE TDeliteaDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE TDeliteaDB;
    PRINT 'Database TDeliteaDB đã được xóa thành công.';
END
ELSE
BEGIN
    PRINT 'Database TDeliteaDB không tồn tại.';
END

CREATE DATABASE TDeliteaDB;
USE TDeliteaDB;

-- Bảng người dùng (nhân viên, quản lý)
-- Bảng người dùng
CREATE TABLE Users (
    UserID NVARCHAR(50) PRIMARY KEY NOT NULL,
    FullName NVARCHAR(100) NOT NULL,
    ContactInfo NVARCHAR(255) UNIQUE NOT NULL,
    PasswordHash VARCHAR(255) NOT NULL,
    Role NVARCHAR(20) CHECK (Role IN (N'Quản lý', N'Nhân viên', N'Thủ kho', N'CSKH')) NOT NULL,
    Status NVARCHAR(50) CHECK (Status IN (N'Hoạt động', N'Đợi phê duyệt')) NOT NULL,
    CreatedAt DATETIME DEFAULT GETDATE()
);

-- Bảng khách hàng
CREATE TABLE Customers (
    CustomerID NVARCHAR(50) PRIMARY KEY NOT NULL,
    FullName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(100) NOT NULL UNIQUE,
    Phone NVARCHAR(20),
    Address NVARCHAR(255),
    ProfilePicture NVARCHAR(255),
    PasswordHash NVARCHAR(255),
    CreatedAt DATETIME DEFAULT GETDATE(),
);

-- Bảng danh mục sản phẩm
CREATE TABLE Categories (
    CategoryID NVARCHAR(50) PRIMARY KEY NOT NULL,
    CategoryName NVARCHAR(100) NOT NULL,
    Description NVARCHAR(255)
);

-- Bảng sản phẩm
CREATE TABLE Products (
    ProductID NVARCHAR(50) PRIMARY KEY NOT NULL,
    ProductName NVARCHAR(150) NOT NULL,
    ImageURL NVARCHAR(255),
    Price DECIMAL(10,2) NOT NULL,
    Status NVARCHAR(20) CHECK (Status IN (N'Còn hàng', N'Hết hàng')) NOT NULL DEFAULT N'Còn hàng',
    CategoryID NVARCHAR(50),
    ProductDes NVARCHAR(MAX),
    CONSTRAINT FK_Products_Categories FOREIGN KEY (CategoryID) REFERENCES Categories(CategoryID)
);

-- Bảng bàn
CREATE TABLE Tables (
    TableID NVARCHAR(50) PRIMARY KEY NOT NULL,
    TableName NVARCHAR(50) NOT NULL,
    Status NVARCHAR(20) CHECK (Status IN (N'Trống', N'Đang sử dụng')) NOT NULL DEFAULT N'Trống'
);
drop table OrderDetails
drop table AppliedDiscounts
drop table Complaints
drop table Orders

-- Bảng hóa đơn
CREATE TABLE Orders (
    OrderID NVARCHAR(50) PRIMARY KEY NOT NULL,
    CustomerID NVARCHAR(50),
    TableID NVARCHAR(50),
    OrderStatus NVARCHAR(20) CHECK (OrderStatus IN (N'Chưa thanh toán', N'Đã thanh toán', N'Đã đặt đơn', N'Đang vận chuyển', N'Đã giao')) DEFAULT N'Chưa thanh toán',
    Total DECIMAL(10,2) DEFAULT 0,
    Discount DECIMAL(10,2) DEFAULT 0,
	ShipPrice DECIMAL(10,2) DEFAULT 0,
    TotalAmount DECIMAL(10,2) DEFAULT 0,
	Payment NVARCHAR(50) CHECK (Payment IN (N'COD', N'Chuyển khoản')) NOT NULL DEFAULT N'COD',
    CreatedAt DATETIME DEFAULT GETDATE(),
	UpdatedAt DATETIME DEFAULT GETDATE(),
	ODDescription NVARCHAR(MAX) ,

	  -- Thông tin VNPAY
    PaymentTransactionId NVARCHAR(100), -- vnp_TransactionNo
    VnPayOrderId NVARCHAR(100),         -- vnp_TxnRef
    VnPayResponseCode NVARCHAR(10),     -- vnp_ResponseCode
    OrderDescription NVARCHAR(500),     -- vnp_OrderInfo

    FOREIGN KEY (CustomerID) REFERENCES Customers(CustomerID),
    FOREIGN KEY (TableID) REFERENCES Tables(TableID)
);



-- Bảng chi tiết hóa đơn
CREATE TABLE OrderDetails (
    OrderDetailID NVARCHAR(50) PRIMARY KEY NOT NULL,
    OrderID NVARCHAR(50),
    ProductID NVARCHAR(50),
    Quantity INT NOT NULL,
    Price DECIMAL(10,2) NOT NULL,
    FOREIGN KEY (OrderID) REFERENCES Orders(OrderID) ON DELETE CASCADE,
    FOREIGN KEY (ProductID) REFERENCES Products(ProductID)
);


-- Bảng tồn kho
CREATE TABLE Inventory (
    InventoryID NVARCHAR(50) PRIMARY KEY NOT NULL,
    ProductID NVARCHAR(50),
	Supplier NVARCHAR(50),
    Quantity INT NOT NULL DEFAULT 0,
	InPrice DECIMAL(10,2) NOT NULL,
    LastUpdated DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (ProductID) REFERENCES Products(ProductID), 
);



-- Bảng mã giảm giá
CREATE TABLE DiscountCodes (
    DiscountCodeID NVARCHAR(50) PRIMARY KEY NOT NULL,
    Code NVARCHAR(50) NOT NULL UNIQUE,
    DiscountAmount DECIMAL(18, 2) NOT NULL,
    DiscountType NVARCHAR(20) CHECK (DiscountType IN (N'Phần trăm', N'Cố định')) NOT NULL,
    ExpiryDate DATETIME NOT NULL,
    MinimumOrderValue DECIMAL(18, 2) DEFAULT 0,
    IsPublic BIT DEFAULT 1, -- 1: công khai, 0: riêng tư
    UsageLimit INT NULL,    -- null = không giới hạn, hoặc giới hạn toàn hệ thống
    UsedCount INT DEFAULT 0, -- Số lần mã này đã được sử dụng
    MaxDiscountAmount DECIMAL(18, 2) DEFAULT NULL, -- Giới hạn giá trị giảm tối đa (null = không giới hạn)
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME DEFAULT GETDATE(),
    UpdatedAt DATETIME DEFAULT GETDATE()
);

CREATE TABLE CustomerDiscountCodes (
    CustomerDiscountID NVARCHAR(50) PRIMARY KEY NOT NULL,
    CustomerID NVARCHAR(50) NOT NULL,
    DiscountCodeID NVARCHAR(50) NOT NULL,
    AssignedAt DATETIME DEFAULT GETDATE(),

    CONSTRAINT FK_Customer_Discount FOREIGN KEY (DiscountCodeID) REFERENCES DiscountCodes(DiscountCodeID) ON DELETE CASCADE,
    CONSTRAINT FK_Discount_Customer FOREIGN KEY (CustomerID) REFERENCES Customers(CustomerID) ON DELETE CASCADE,

    CONSTRAINT UQ_Customer_Discount UNIQUE (CustomerID, DiscountCodeID) -- một người chỉ được gán 1 lần
);



-- Bảng mã giảm giá đã áp dụng
CREATE TABLE AppliedDiscounts (
    AppliedDiscountID NVARCHAR(50) PRIMARY KEY NOT NULL,
    OrderID NVARCHAR(50) NOT NULL,
    DiscountCodeID NVARCHAR(50) NOT NULL,
    CustomerID NVARCHAR(50) NOT NULL,
    DiscountAmount DECIMAL(18, 2) NOT NULL,
    AppliedAt DATETIME DEFAULT GETDATE(),

    FOREIGN KEY (DiscountCodeID) REFERENCES DiscountCodes(DiscountCodeID),
    FOREIGN KEY (CustomerID) REFERENCES Customers(CustomerID),
    FOREIGN KEY (OrderID) REFERENCES Orders(OrderID),

    CONSTRAINT UQ_Discount_UsedOncePerCustomer UNIQUE (DiscountCodeID, CustomerID)
);


-- Bảng khiếu nại
CREATE TABLE Complaints (
    ComplaintID NVARCHAR(50) PRIMARY KEY NOT NULL,
	Email NVARCHAR(50),
    CustomerID NVARCHAR(50) ,
    OrderID NVARCHAR(50),
    Subject NVARCHAR(255) NOT NULL,
    Description NVARCHAR(MAX) NOT NULL,
    Status NVARCHAR(20) CHECK (Status IN (N'Chưa xử lý', N'Đã xử lý')) DEFAULT N'Chưa xử lý',
    CreatedAt DATETIME DEFAULT GETDATE(),
    ResolvedAt DATETIME,
    AdminComment NVARCHAR(MAX),
    FOREIGN KEY (CustomerID) REFERENCES Customers(CustomerID),
    FOREIGN KEY (OrderID) REFERENCES Orders(OrderID)
);


-- Bảng bài viết
CREATE TABLE Posts (
    PostID NVARCHAR(50) PRIMARY KEY NOT NULL,
    Title NVARCHAR(255) NOT NULL,
    Summary NVARCHAR(500) NOT NULL,
    Content NVARCHAR(MAX) NOT NULL,
    ImageUrl NVARCHAR(255),
    PostType NVARCHAR(50) CHECK (PostType IN (N'Sản phẩm', N'Sự kiện')) DEFAULT N'Sự kiện',
    CreatedAt DATETIME DEFAULT GETDATE(),
    UpdatedAt DATETIME DEFAULT GETDATE(),
    UserID NVARCHAR(50),
    AverageRating FLOAT, -- Cột lưu trung bình sao (cập nhật thủ công bằng ứng dụng),
    FOREIGN KEY (UserID) REFERENCES Users(UserID)
);

-- Bảng bình luận
CREATE TABLE Comments (
    CommentID NVARCHAR(50) PRIMARY KEY NOT NULL,
    PostID NVARCHAR(50) NOT NULL,
    UserID NVARCHAR(50) NOT NULL,
    UserType NVARCHAR(20) NOT NULL, -- "User" hoặc "Customer"
	Rating INT CHECK (Rating BETWEEN 1 AND 5) NULL,
    Content NVARCHAR(1000) NOT NULL,
    CreatedAt DATETIME DEFAULT GETDATE()
    -- KHÔNG tạo FOREIGN KEY ở đây, sẽ xử lý logic trong code
);




