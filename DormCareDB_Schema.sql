/* =========================================================
   DORMCARE DATABASE
   Student Dormitory Management System
   SQL SERVER
   ========================================================= */

USE master;
GO

IF DB_ID(N'DormCareDB') IS NOT NULL
BEGIN
    ALTER DATABASE DormCareDB
    SET SINGLE_USER
    WITH ROLLBACK IMMEDIATE;

    DROP DATABASE DormCareDB;
END;
GO

CREATE DATABASE DormCareDB;
GO

USE DormCareDB;
GO

/* =========================================================
   3. USERS
   ========================================================= */
CREATE TABLE Users
(
    UserId INT IDENTITY(1,1) PRIMARY KEY,
    Username VARCHAR(50) NOT NULL,
    PasswordHash VARCHAR(255) NOT NULL,
    Email VARCHAR(100) NOT NULL,
    Phone VARCHAR(20) NULL,
    Role VARCHAR(20) NOT NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT 1,
    LastLoginAt DATETIME2 NULL,
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NULL,
    CONSTRAINT UQ_Users_Username UNIQUE (Username),
    CONSTRAINT UQ_Users_Email UNIQUE (Email),
    CONSTRAINT CK_Users_Role CHECK (Role IN ('Student', 'Manager'))
);
GO

/* =========================================================
   4. STUDENTS
   ========================================================= */
CREATE TABLE Students
(
    StudentId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    StudentCode VARCHAR(20) NOT NULL,
    FullName NVARCHAR(100) NOT NULL,
    DateOfBirth DATE NOT NULL,
    Gender VARCHAR(10) NOT NULL,
    Email VARCHAR(100) NOT NULL,
    Phone VARCHAR(20) NOT NULL,
    Major NVARCHAR(100) NOT NULL,
    ClassName VARCHAR(50) NOT NULL,
    Campus NVARCHAR(100) NOT NULL,
    EmergencyContactName NVARCHAR(100) NULL,
    EmergencyContactPhone VARCHAR(20) NULL,
    Address NVARCHAR(255) NULL,
    Status VARCHAR(20) NOT NULL CONSTRAINT DF_Students_Status DEFAULT 'Active',
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Students_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NULL,
    CONSTRAINT UQ_Students_UserId UNIQUE (UserId),
    CONSTRAINT UQ_Students_StudentCode UNIQUE (StudentCode),
    CONSTRAINT UQ_Students_Email UNIQUE (Email),
    CONSTRAINT CK_Students_Gender CHECK (Gender IN ('Male', 'Female', 'Other')),
    CONSTRAINT CK_Students_Status CHECK (Status IN ('Active', 'Inactive', 'Graduated', 'Suspended')),
    CONSTRAINT FK_Students_Users FOREIGN KEY (UserId) REFERENCES Users(UserId)
);
GO

/* =========================================================
   5. BUILDINGS
   ========================================================= */
CREATE TABLE Buildings
(
    BuildingId INT IDENTITY(1,1) PRIMARY KEY,
    BuildingCode VARCHAR(20) NOT NULL,
    BuildingName NVARCHAR(100) NOT NULL,
    Address NVARCHAR(255) NOT NULL,
    NumberOfFloors INT NOT NULL,
    Description NVARCHAR(500) NULL,
    Status VARCHAR(20) NOT NULL CONSTRAINT DF_Buildings_Status DEFAULT 'Active',
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Buildings_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NULL,
    CONSTRAINT UQ_Buildings_Code UNIQUE (BuildingCode),
    CONSTRAINT CK_Buildings_Floors CHECK (NumberOfFloors > 0),
    CONSTRAINT CK_Buildings_Status CHECK (Status IN ('Active', 'Inactive', 'Maintenance'))
);
GO

/* =========================================================
   6. ROOMS
   ========================================================= */
CREATE TABLE Rooms
(
    RoomId INT IDENTITY(1,1) PRIMARY KEY,
    BuildingId INT NOT NULL,
    RoomNumber VARCHAR(20) NOT NULL,
    FloorNumber INT NOT NULL,
    RoomType VARCHAR(30) NOT NULL,
    Capacity INT NOT NULL,
    MonthlyRent DECIMAL(18,2) NOT NULL,
    GenderType VARCHAR(20) NOT NULL,
    Description NVARCHAR(500) NULL,
    Status VARCHAR(20) NOT NULL CONSTRAINT DF_Rooms_Status DEFAULT 'Available',
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Rooms_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NULL,
    CONSTRAINT UQ_Rooms_Building_Room UNIQUE (BuildingId, RoomNumber),
    CONSTRAINT CK_Rooms_Floor CHECK (FloorNumber > 0),
    CONSTRAINT CK_Rooms_Capacity CHECK (Capacity > 0 AND Capacity <= 20),
    CONSTRAINT CK_Rooms_Rent CHECK (MonthlyRent >= 0),
    CONSTRAINT CK_Rooms_Type CHECK (RoomType IN ('Standard', 'Premium', 'Accessible')),
    CONSTRAINT CK_Rooms_Gender CHECK (GenderType IN ('Male', 'Female', 'Mixed')),
    CONSTRAINT CK_Rooms_Status CHECK (Status IN ('Available', 'Full', 'Maintenance', 'Inactive')),
    CONSTRAINT FK_Rooms_Buildings FOREIGN KEY (BuildingId) REFERENCES Buildings(BuildingId)
);
GO

/* =========================================================
   7. BEDS
   ========================================================= */
CREATE TABLE Beds
(
    BedId INT IDENTITY(1,1) PRIMARY KEY,
    RoomId INT NOT NULL,
    BedNumber VARCHAR(20) NOT NULL,
    BedCode VARCHAR(30) NOT NULL,
    Status VARCHAR(20) NOT NULL CONSTRAINT DF_Beds_Status DEFAULT 'Available',
    Description NVARCHAR(255) NULL,
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Beds_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NULL,
    CONSTRAINT UQ_Beds_BedCode UNIQUE (BedCode),
    CONSTRAINT UQ_Beds_Room_BedNumber UNIQUE (RoomId, BedNumber),
    CONSTRAINT CK_Beds_Status CHECK (Status IN ('Available', 'Occupied', 'Reserved', 'Maintenance')),
    CONSTRAINT FK_Beds_Rooms FOREIGN KEY (RoomId) REFERENCES Rooms(RoomId)
);
GO

/* =========================================================
   8. ROOM APPLICATIONS
   ========================================================= */
CREATE TABLE RoomApplications
(
    ApplicationId INT IDENTITY(1,1) PRIMARY KEY,
    ApplicationCode VARCHAR(30) NOT NULL,
    StudentId INT NOT NULL,
    RoomId INT NOT NULL,
    PreferredBedId INT NULL,
    Reason NVARCHAR(500) NULL,
    ApplicationDate DATETIME2 NOT NULL CONSTRAINT DF_Applications_Date DEFAULT SYSUTCDATETIME(),
    ReviewedBy INT NULL,
    ReviewedAt DATETIME2 NULL,
    ReviewNote NVARCHAR(500) NULL,
    Status VARCHAR(20) NOT NULL CONSTRAINT DF_Applications_Status DEFAULT 'Pending',
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Applications_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT UQ_Applications_Code UNIQUE (ApplicationCode),
    CONSTRAINT CK_Applications_Status CHECK (Status IN ('Pending', 'Approved', 'Rejected', 'Cancelled')),
    CONSTRAINT FK_Applications_Student FOREIGN KEY (StudentId) REFERENCES Students(StudentId),
    CONSTRAINT FK_Applications_Room FOREIGN KEY (RoomId) REFERENCES Rooms(RoomId),
    CONSTRAINT FK_Applications_Bed FOREIGN KEY (PreferredBedId) REFERENCES Beds(BedId),
    CONSTRAINT FK_Applications_Reviewer FOREIGN KEY (ReviewedBy) REFERENCES Users(UserId)
);
GO

/* =========================================================
   9. ROOM ASSIGNMENTS
   ========================================================= */
CREATE TABLE RoomAssignments
(
    AssignmentId INT IDENTITY(1,1) PRIMARY KEY,
    StudentId INT NOT NULL,
    RoomId INT NOT NULL,
    BedId INT NOT NULL,
    StartDate DATE NOT NULL,
    EndDate DATE NULL,
    AssignmentType VARCHAR(30) NOT NULL,
    Status VARCHAR(20) NOT NULL CONSTRAINT DF_Assignments_Status DEFAULT 'Active',
    AssignedBy INT NOT NULL,
    Note NVARCHAR(500) NULL,
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Assignments_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT CK_Assignments_Date CHECK (EndDate IS NULL OR EndDate >= StartDate),
    CONSTRAINT CK_Assignments_Type CHECK (AssignmentType IN ('InitialAssignment', 'RoomTransfer', 'Replacement')),
    CONSTRAINT CK_Assignments_Status CHECK (Status IN ('Active', 'Ended')),
    CONSTRAINT FK_Assignments_Student FOREIGN KEY (StudentId) REFERENCES Students(StudentId),
    CONSTRAINT FK_Assignments_Room FOREIGN KEY (RoomId) REFERENCES Rooms(RoomId),
    CONSTRAINT FK_Assignments_Bed FOREIGN KEY (BedId) REFERENCES Beds(BedId),
    CONSTRAINT FK_Assignments_Manager FOREIGN KEY (AssignedBy) REFERENCES Users(UserId)
);
GO

/* =========================================================
   10. INVOICES
   ========================================================= */
CREATE TABLE Invoices
(
    InvoiceId INT IDENTITY(1,1) PRIMARY KEY,
    InvoiceCode VARCHAR(30) NOT NULL,
    StudentId INT NOT NULL,
    RoomId INT NOT NULL,
    BillingMonth DATE NOT NULL,
    RoomFee DECIMAL(18,2) NOT NULL,
    ServiceFee DECIMAL(18,2) NOT NULL CONSTRAINT DF_Invoices_ServiceFee DEFAULT 0,
    OtherFee DECIMAL(18,2) NOT NULL CONSTRAINT DF_Invoices_OtherFee DEFAULT 0,
    DiscountAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_Invoices_Discount DEFAULT 0,
    TotalAmount AS (RoomFee + ServiceFee + OtherFee - DiscountAmount) PERSISTED,
    DueDate DATE NOT NULL,
    PaidAt DATETIME2 NULL,
    Status VARCHAR(20) NOT NULL CONSTRAINT DF_Invoices_Status DEFAULT 'Unpaid',
    Note NVARCHAR(500) NULL,
    CONSTRAINT UQ_Invoices_Code UNIQUE (InvoiceCode),
    CONSTRAINT CK_Invoices_Fee CHECK (RoomFee >= 0 AND ServiceFee >= 0 AND OtherFee >= 0 AND DiscountAmount >= 0),
    CONSTRAINT CK_Invoices_Status CHECK (Status IN ('Draft', 'Unpaid', 'PartiallyPaid', 'Paid', 'Overdue', 'Cancelled')),
    CONSTRAINT FK_Invoices_Student FOREIGN KEY (StudentId) REFERENCES Students(StudentId),
    CONSTRAINT FK_Invoices_Room FOREIGN KEY (RoomId) REFERENCES Rooms(RoomId)
);
GO

/* =========================================================
   11. PAYMENTS
   ========================================================= */
CREATE TABLE Payments
(
    PaymentId INT IDENTITY(1,1) PRIMARY KEY,
    PaymentCode VARCHAR(30) NOT NULL,
    InvoiceId INT NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    PaymentMethod VARCHAR(30) NOT NULL,
    TransactionReference VARCHAR(100) NULL,
    PaymentDate DATETIME2 NOT NULL CONSTRAINT DF_Payments_Date DEFAULT SYSUTCDATETIME(),
    ReceivedBy INT NULL,
    Status VARCHAR(20) NOT NULL CONSTRAINT DF_Payments_Status DEFAULT 'Completed',
    Note NVARCHAR(500) NULL,
    CONSTRAINT UQ_Payments_Code UNIQUE (PaymentCode),
    CONSTRAINT CK_Payments_Amount CHECK (Amount > 0),
    CONSTRAINT CK_Payments_Method CHECK (PaymentMethod IN ('Cash', 'BankTransfer', 'MockPayment')),
    CONSTRAINT CK_Payments_Status CHECK (Status IN ('Pending', 'Completed', 'Failed', 'Refunded')),
    CONSTRAINT FK_Payments_Invoice FOREIGN KEY (InvoiceId) REFERENCES Invoices(InvoiceId),
    CONSTRAINT FK_Payments_ReceivedBy FOREIGN KEY (ReceivedBy) REFERENCES Users(UserId)
);
GO

/* =========================================================
   12. MAINTENANCE REQUESTS
   ========================================================= */
CREATE TABLE MaintenanceRequests
(
    RequestId INT IDENTITY(1,1) PRIMARY KEY,
    RequestCode VARCHAR(30) NOT NULL,
    StudentId INT NOT NULL,
    RoomId INT NOT NULL,
    Category VARCHAR(50) NOT NULL,
    Title NVARCHAR(200) NOT NULL,
    Description NVARCHAR(1000) NOT NULL,
    Priority VARCHAR(20) NOT NULL CONSTRAINT DF_Maintenance_Priority DEFAULT 'Medium',
    Status VARCHAR(30) NOT NULL CONSTRAINT DF_Maintenance_Status DEFAULT 'Submitted',
    AssignedTo INT NULL,
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Maintenance_CreatedAt DEFAULT SYSUTCDATETIME(),
    AssignedAt DATETIME2 NULL,
    ResolvedAt DATETIME2 NULL,
    ClosedAt DATETIME2 NULL,
    ResolutionNote NVARCHAR(1000) NULL,
    StudentRating INT NULL,
    StudentFeedback NVARCHAR(500) NULL,
    CONSTRAINT UQ_Maintenance_Code UNIQUE (RequestCode),
    CONSTRAINT CK_Maintenance_Priority CHECK (Priority IN ('Low', 'Medium', 'High', 'Critical')),
    CONSTRAINT CK_Maintenance_Status CHECK (Status IN ('Submitted', 'Assigned', 'InProgress', 'Resolved', 'Closed', 'Rejected')),
    CONSTRAINT CK_Maintenance_Rating CHECK (StudentRating IS NULL OR StudentRating BETWEEN 1 AND 5),
    CONSTRAINT FK_Maintenance_Student FOREIGN KEY (StudentId) REFERENCES Students(StudentId),
    CONSTRAINT FK_Maintenance_Room FOREIGN KEY (RoomId) REFERENCES Rooms(RoomId),
    CONSTRAINT FK_Maintenance_Assignee FOREIGN KEY (AssignedTo) REFERENCES Users(UserId)
);
GO

/* =========================================================
   13. NOTIFICATIONS
   ========================================================= */
CREATE TABLE Notifications
(
    NotificationId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    Title NVARCHAR(200) NOT NULL,
    Message NVARCHAR(1000) NOT NULL,
    NotificationType VARCHAR(30) NOT NULL,
    ReferenceId INT NULL,
    IsRead BIT NOT NULL CONSTRAINT DF_Notifications_IsRead DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Notifications_CreatedAt DEFAULT SYSUTCDATETIME(),
    ReadAt DATETIME2 NULL,
    CONSTRAINT FK_Notifications_User FOREIGN KEY (UserId) REFERENCES Users(UserId)
);
GO

/* =========================================================
   14. AUDIT LOGS
   ========================================================= */
CREATE TABLE AuditLogs
(
    AuditLogId BIGINT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NULL,
    Action VARCHAR(50) NOT NULL,
    EntityName VARCHAR(100) NOT NULL,
    EntityId INT NULL,
    OldValues NVARCHAR(MAX) NULL,
    NewValues NVARCHAR(MAX) NULL,
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_AuditLogs_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_AuditLogs_User FOREIGN KEY (UserId) REFERENCES Users(UserId)
);
GO

/* =========================================================
   15. INDEXES
   ========================================================= */
CREATE INDEX IX_Students_FullName ON Students(FullName);
CREATE INDEX IX_Students_Status ON Students(Status);
CREATE INDEX IX_Rooms_Status ON Rooms(Status);
CREATE INDEX IX_Rooms_BuildingId ON Rooms(BuildingId);
CREATE INDEX IX_Beds_Room_Status ON Beds(RoomId, Status);
CREATE INDEX IX_Applications_Status ON RoomApplications(Status);
CREATE INDEX IX_Applications_Student ON RoomApplications(StudentId);
CREATE INDEX IX_Invoices_Status ON Invoices(Status);
CREATE INDEX IX_Invoices_DueDate ON Invoices(DueDate);
CREATE UNIQUE NONCLUSTERED INDEX UQ_Invoices_Student_Month ON Invoices(StudentId, BillingMonth) WHERE Status <> 'Cancelled';
CREATE INDEX IX_Payments_Invoice ON Payments(InvoiceId);
CREATE INDEX IX_Maintenance_Status_Priority ON MaintenanceRequests(Status, Priority);
CREATE INDEX IX_Notifications_User_Read ON Notifications(UserId, IsRead);
GO

/* =========================================================
   16. INSERT USERS
   ========================================================= */
INSERT INTO Users (Username, PasswordHash, Email, Phone, Role) VALUES
('manager01', 'HASH_MANAGER_01', 'manager01@dormcare.com', '0901000001', 'Manager'),
('manager02', 'HASH_MANAGER_02', 'manager02@dormcare.com', '0901000002', 'Manager');
GO

DECLARE @i INT = 1;
WHILE @i <= 30
BEGIN
    INSERT INTO Users (Username, PasswordHash, Email, Phone, Role) VALUES
    (CONCAT('student', @i), CONCAT('HASH_STUDENT_', @i), CONCAT('student', @i, '@fpt.edu.vn'), CONCAT('0902', RIGHT('000000' + CAST(@i AS VARCHAR(6)), 6)), 'Student');
    SET @i = @i + 1;
END;
GO

/* =========================================================
   17. INSERT STUDENTS
   ========================================================= */
INSERT INTO Students (UserId, StudentCode, FullName, DateOfBirth, Gender, Email, Phone, Major, ClassName, Campus, EmergencyContactName, EmergencyContactPhone, Address, Status)
SELECT
    UserId,
    CONCAT('SE', RIGHT('000000' + CAST(UserId AS VARCHAR(6)), 6)),
    CASE
        WHEN UserId % 5 = 0 THEN N'Nguyễn Văn Dạng'
        WHEN UserId % 5 = 1 THEN N'Trần Minh Thịnh'
        WHEN UserId % 5 = 2 THEN N'Lê Quốc Vinh'
        WHEN UserId % 5 = 3 THEN N'Phạm Minh Hậu'
        ELSE N'Nguyễn Trung'
    END + N' ' + CAST(UserId AS NVARCHAR(10)),
    DATEADD(DAY, -(UserId * 100), CAST('2006-01-01' AS DATE)),
    CASE WHEN UserId % 2 = 0 THEN 'Male' ELSE 'Female' END,
    Email,
    Phone,
    CASE
        WHEN UserId % 3 = 0 THEN N'Software Engineering'
        WHEN UserId % 3 = 1 THEN N'Information Technology'
        ELSE N'Artificial Intelligence'
    END,
    CONCAT('SE', 1800 + UserId),
    N'FPT University Da Nang',
    CONCAT(N'Người liên hệ ', UserId),
    CONCAT('0915', RIGHT('000000' + CAST(UserId AS VARCHAR(6)), 6)),
    CASE WHEN UserId % 3 = 0 THEN N'Đà Nẵng' WHEN UserId % 3 = 1 THEN N'Quảng Nam' ELSE N'Huế' END,
    'Active'
FROM Users WHERE Role = 'Student';
GO

/* =========================================================
   18. INSERT BUILDINGS
   ========================================================= */
INSERT INTO Buildings (BuildingCode, BuildingName, Address, NumberOfFloors, Description, Status) VALUES
('A', N'Tòa nhà A', N'FPT University Dormitory, Da Nang', 5, N'Tòa ký túc xá dành cho sinh viên nam', 'Active'),
('B', N'Tòa nhà B', N'FPT University Dormitory, Da Nang', 5, N'Tòa ký túc xá dành cho sinh viên nữ', 'Active'),
('C', N'Tòa nhà C', N'FPT University Dormitory, Da Nang', 6, N'Tòa ký túc xá hỗn hợp', 'Active');
GO

/* =========================================================
   19. INSERT ROOMS (30 Rooms)
   ========================================================= */
DECLARE @BuildingId INT, @BuildingCode VARCHAR(20), @RoomIndex INT;
DECLARE BuildingCursor CURSOR FOR SELECT BuildingId, BuildingCode FROM Buildings;
OPEN BuildingCursor;
FETCH NEXT FROM BuildingCursor INTO @BuildingId, @BuildingCode;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @RoomIndex = 1;
    WHILE @RoomIndex <= 10
    BEGIN
        INSERT INTO Rooms (BuildingId, RoomNumber, FloorNumber, RoomType, Capacity, MonthlyRent, GenderType, Description, Status) VALUES
        (
            @BuildingId,
            CONCAT(@BuildingCode, RIGHT('00' + CAST(@RoomIndex AS VARCHAR(2)), 2)),
            CASE WHEN @RoomIndex <= 2 THEN 1 WHEN @RoomIndex <= 4 THEN 2 WHEN @RoomIndex <= 6 THEN 3 WHEN @RoomIndex <= 8 THEN 4 ELSE 5 END,
            CASE WHEN @RoomIndex = 10 THEN 'Premium' ELSE 'Standard' END,
            6,
            CASE WHEN @RoomIndex = 10 THEN 2000000 ELSE 1500000 END,
            CASE WHEN @BuildingCode = 'A' THEN 'Male' WHEN @BuildingCode = 'B' THEN 'Female' ELSE 'Mixed' END,
            N'Phòng ký túc xá được trang bị giường, bàn học và tủ cá nhân',
            CASE WHEN @RoomIndex = 9 THEN 'Maintenance' ELSE 'Available' END
        );
        SET @RoomIndex = @RoomIndex + 1;
    END;
    FETCH NEXT FROM BuildingCursor INTO @BuildingId, @BuildingCode;
END;
CLOSE BuildingCursor;
DEALLOCATE BuildingCursor;
GO

/* =========================================================
   20. INSERT 180 BEDS
   ========================================================= */
INSERT INTO Beds (RoomId, BedNumber, BedCode, Status, Description)
SELECT
    r.RoomId,
    CONCAT('B', n.BedNumber),
    CONCAT(r.RoomNumber, '-B', n.BedNumber),
    CASE WHEN r.RoomId % 11 = 0 AND n.BedNumber = 1 THEN 'Maintenance' ELSE 'Available' END,
    CONCAT(N'Giường ', n.BedNumber, N' của phòng ', r.RoomNumber)
FROM Rooms AS r
CROSS JOIN (
    SELECT 1 AS BedNumber UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4 UNION ALL SELECT 5 UNION ALL SELECT 6
) AS n;
GO

/* =========================================================
   21. ROOM APPLICATIONS
   ========================================================= */
INSERT INTO RoomApplications (ApplicationCode, StudentId, RoomId, PreferredBedId, Reason, ApplicationDate, Status)
SELECT TOP 20
    CONCAT('APP-2026-', RIGHT('0000' + CAST(s.StudentId AS VARCHAR(4)), 4)),
    s.StudentId,
    ar.RoomId,
    NULL,
    CASE WHEN s.StudentId % 3 = 0 THEN N'Gần khu học tập và thuận tiện đi lại' WHEN s.StudentId % 3 = 1 THEN N'Cần đăng ký chỗ ở trong học kỳ mới' ELSE N'Mong muốn ở ký túc xá để tiết kiệm chi phí' END,
    DATEADD(DAY, -(s.StudentId % 30), SYSUTCDATETIME()),
    CASE WHEN s.StudentId % 7 = 0 THEN 'Rejected' ELSE 'Pending' END
FROM Students AS s
CROSS JOIN (SELECT TOP 5 RoomId FROM Rooms WHERE Status = 'Available' ORDER BY RoomId) AS ar
ORDER BY s.StudentId;
GO

/* =========================================================
   22. ROOM ASSIGNMENTS
   ========================================================= */
;WITH AvailableBeds AS (
    SELECT BedId, RoomId, ROW_NUMBER() OVER(ORDER BY BedId) AS RowNumber
    FROM Beds WHERE Status = 'Available'
)
INSERT INTO RoomAssignments (StudentId, RoomId, BedId, StartDate, AssignmentType, Status, AssignedBy, Note)
SELECT
    s.StudentId, ab.RoomId, ab.BedId,
    DATEADD(DAY, -(s.StudentId * 3), CAST(GETUTCDATE() AS DATE)),
    'InitialAssignment', 'Active', 1, N'Phân phòng ban đầu cho sinh viên'
FROM Students AS s
INNER JOIN AvailableBeds AS ab ON ab.RowNumber = s.StudentId
WHERE s.StudentId <= 20;
GO

/* =========================================================
   23. UPDATE OCCUPIED BEDS
   ========================================================= */
UPDATE Beds SET Status = 'Occupied', UpdatedAt = SYSUTCDATETIME()
WHERE BedId IN (SELECT BedId FROM RoomAssignments WHERE Status = 'Active');
GO

/* =========================================================
   24. UPDATE ROOM STATUS
   ========================================================= */
UPDATE Rooms SET Status = 'Full', UpdatedAt = SYSUTCDATETIME()
WHERE RoomId IN (
    SELECT RoomId FROM Beds GROUP BY RoomId HAVING COUNT(CASE WHEN Status = 'Available' THEN 1 END) = 0
);
GO

/* =========================================================
   25. INVOICES
   ========================================================= */
INSERT INTO Invoices (InvoiceCode, StudentId, RoomId, BillingMonth, RoomFee, ServiceFee, OtherFee, DiscountAmount, DueDate, PaidAt, Status, Note)
SELECT
    CONCAT('INV-2026-', RIGHT('0000' + CAST(ra.StudentId AS VARCHAR(4)), 4)),
    ra.StudentId, ra.RoomId, DATEFROMPARTS(2026, 7, 1), r.MonthlyRent, 150000,
    CASE WHEN ra.StudentId % 5 = 0 THEN 50000 ELSE 0 END,
    CASE WHEN ra.StudentId % 10 = 0 THEN 100000 ELSE 0 END,
    DATEFROMPARTS(2026, 7, 10),
    CASE WHEN ra.StudentId % 4 = 0 THEN SYSUTCDATETIME() ELSE NULL END,
    CASE WHEN ra.StudentId % 4 = 0 THEN 'Paid' WHEN ra.StudentId % 5 = 0 THEN 'Overdue' ELSE 'Unpaid' END,
    N'Phí ký túc xá tháng 07/2026'
FROM RoomAssignments AS ra
INNER JOIN Rooms AS r ON ra.RoomId = r.RoomId
WHERE ra.Status = 'Active';
GO

/* =========================================================
   26. PAYMENTS
   ========================================================= */
INSERT INTO Payments (PaymentCode, InvoiceId, Amount, PaymentMethod, TransactionReference, PaymentDate, ReceivedBy, Status, Note)
SELECT
    CONCAT('PAY-2026-', RIGHT('0000' + CAST(i.InvoiceId AS VARCHAR(4)), 4)),
    i.InvoiceId, i.TotalAmount,
    CASE WHEN i.InvoiceId % 2 = 0 THEN 'BankTransfer' ELSE 'MockPayment' END,
    CONCAT('TXN-', i.InvoiceCode),
    ISNULL(i.PaidAt, SYSUTCDATETIME()), 1, 'Completed', N'Thanh toán phí ký túc xá thành công'
FROM Invoices AS i WHERE i.Status = 'Paid';
GO

/* =========================================================
   27. MAINTENANCE REQUESTS
   ========================================================= */
INSERT INTO MaintenanceRequests (RequestCode, StudentId, RoomId, Category, Title, Description, Priority, Status, AssignedTo, AssignedAt, ResolvedAt, ResolutionNote)
SELECT TOP 20
    CONCAT('REQ-2026-', RIGHT('0000' + CAST(ra.StudentId AS VARCHAR(4)), 4)),
    ra.StudentId, ra.RoomId,
    CASE WHEN ra.StudentId % 4 = 0 THEN 'Electricity' WHEN ra.StudentId % 4 = 1 THEN 'Water' WHEN ra.StudentId % 4 = 2 THEN 'AirConditioner' ELSE 'Furniture' END,
    CASE WHEN ra.StudentId % 4 = 0 THEN N'Đèn trong phòng không hoạt động' WHEN ra.StudentId % 4 = 1 THEN N'Vòi nước bị rò rỉ' WHEN ra.StudentId % 4 = 2 THEN N'Máy lạnh không hoạt động' ELSE N'Bàn học bị hỏng' END,
    N'Sinh viên báo cáo sự cố cần được kiểm tra và xử lý',
    CASE WHEN ra.StudentId % 5 = 0 THEN 'High' WHEN ra.StudentId % 3 = 0 THEN 'Low' ELSE 'Medium' END,
    CASE WHEN ra.StudentId % 4 = 0 THEN 'Resolved' WHEN ra.StudentId % 3 = 0 THEN 'InProgress' ELSE 'Submitted' END,
    CASE WHEN ra.StudentId % 4 = 0 THEN 1 ELSE NULL END,
    CASE WHEN ra.StudentId % 4 = 0 THEN DATEADD(DAY, -2, SYSUTCDATETIME()) ELSE NULL END,
    CASE WHEN ra.StudentId % 4 = 0 THEN DATEADD(DAY, -1, SYSUTCDATETIME()) ELSE NULL END,
    CASE WHEN ra.StudentId % 4 = 0 THEN N'Đã xử lý và kiểm tra hoàn tất' ELSE NULL END
FROM RoomAssignments AS ra WHERE ra.Status = 'Active' ORDER BY ra.StudentId;
GO

/* =========================================================
   28. NOTIFICATIONS & AUDIT LOGS
   ========================================================= */
INSERT INTO Notifications (UserId, Title, Message, NotificationType, ReferenceId, IsRead)
SELECT s.UserId, N'Chào mừng đến với DormCare', N'Tài khoản của bạn đã được tạo thành công trên hệ thống quản lý ký túc xá.', 'System', NULL, CASE WHEN s.StudentId % 3 = 0 THEN 1 ELSE 0 END
FROM Students AS s;
GO

INSERT INTO Notifications (UserId, Title, Message, NotificationType, ReferenceId, IsRead)
SELECT TOP 10 s.UserId, N'Thông báo phí ký túc xá', N'Hóa đơn phí ký túc xá tháng 07/2026 đã được tạo. Vui lòng kiểm tra và thanh toán đúng hạn.', 'Invoice', i.InvoiceId, 0
FROM Invoices AS i INNER JOIN Students AS s ON i.StudentId = s.StudentId ORDER BY i.InvoiceId;
GO

INSERT INTO AuditLogs (UserId, Action, EntityName, EntityId, OldValues, NewValues) VALUES
(1, 'CREATE', 'Building', 1, NULL, N'Created Building A'),
(1, 'CREATE', 'Building', 2, NULL, N'Created Building B'),
(1, 'CREATE', 'Building', 3, NULL, N'Created Building C'),
(1, 'SEED', 'Database', NULL, NULL, N'Initial DormCare database seed completed');
GO

PRINT 'DORMCARE DATABASE CREATED SUCCESSFULLY!';
