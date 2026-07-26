using System;
using Microsoft.Data.SqlClient;

class Program
{
    static void Main()
    {
        string connStr = "Server=DANG;Database=DormCareDB;User Id=sa;Password=123456;TrustServerCertificate=True;Encrypt=False;";
        Console.WriteLine("Connecting to SQL Server DormCareDB...");

        try
        {
            using var conn = new SqlConnection(connStr);
            conn.Open();
            Console.WriteLine("SUCCESS: Connected to SQL Server successfully!");

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT 
                    (SELECT COUNT(*) FROM Users) AS TotalUsers,
                    (SELECT COUNT(*) FROM Students) AS TotalStudents,
                    (SELECT COUNT(*) FROM Buildings) AS TotalBuildings,
                    (SELECT COUNT(*) FROM Rooms) AS TotalRooms,
                    (SELECT COUNT(*) FROM Beds) AS TotalBeds;
            ";

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                Console.WriteLine($"--- DATABASE STATS IN DORMCAREDB ---");
                Console.WriteLine($"Users:     {reader["TotalUsers"]}");
                Console.WriteLine($"Students:  {reader["TotalStudents"]}");
                Console.WriteLine($"Buildings: {reader["TotalBuildings"]}");
                Console.WriteLine($"Rooms:     {reader["TotalRooms"]}");
                Console.WriteLine($"Beds:      {reader["TotalBeds"]}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR Connecting to SQL Server: {ex.Message}");
        }
    }
}
