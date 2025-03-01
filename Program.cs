using System;
using System.Collections.Generic;
using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;

class Program
{
    private const string ConnectionString = "test_conn";

    static void Main()
    {

        static void CreateCarsTable(IDbConnection db)
        {
            string sql = @"IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Cars' AND xtype='U')
                        CREATE TABLE Cars (
                            Id INT IDENTITY(1,1) PRIMARY KEY,
                            Brand NVARCHAR(50),
                            Model NVARCHAR(50),
                            Year INT,
                            Price DECIMAL(18,2)
                        );";
            db.Execute(sql);
        }

        static int InsertCar(IDbConnection db, string brand, string model, int year, decimal price)
        {
            string sql = "INSERT INTO Cars (Brand, Model, Year, Price) VALUES (@Brand, @Model, @Year, @Price); SELECT CAST(SCOPE_IDENTITY() as int);";
            return db.QuerySingle<int>(sql, new { Brand = brand, Model = model, Year = year, Price = price });
        }

        static void UpdateCarPrice(IDbConnection db, int carId, decimal newPrice)
        {
            string sql = "UPDATE Cars SET Price = @NewPrice WHERE Id = @CarId";
            db.Execute(sql, new { NewPrice = newPrice, CarId = carId });
        }

        static void DeleteCar(IDbConnection db, int carId)
        {
            string sql = "DELETE FROM Cars WHERE Id = @CarId";
            db.Execute(sql, new { CarId = carId });
        }

        static List<Car> GetAllCars(IDbConnection db)
        {
            string sql = "SELECT * FROM Cars";
            return db.Query<Car>(sql).AsList();
        }

        static List<Car> GetCarsByBrand(IDbConnection db, string brand)
        {
            string sql = "SELECT * FROM Cars WHERE Brand = @BrandName";
            return db.Query<Car>(sql, new { BrandName = brand }).AsList();
        }

        static void RunTransaction(IDbConnection db)
        {
            using (var transaction = db.BeginTransaction())
            {
                try
                {
                    int carId = InsertCar(db, "Honda", "Civic", 2021, 25000);
                    Console.WriteLine($"Transaction: Inserted Honda Civic with ID: {carId}");

                    UpdateCarPrice(db, carId, 26000);
                    Console.WriteLine($"Transaction: Updated Honda Civic price");

                    DeleteCar(db, carId - 1); 
                    Console.WriteLine("Transaction: Deleted previous car");

                    transaction.Commit();
                    Console.WriteLine("Transaction committed successfully.");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Console.WriteLine("Transaction rolled back due to error: " + ex.Message);
                }
            }
        }

        using (IDbConnection db = new SqlConnection(ConnectionString))
        {
            db.Open();

            CreateCarsTable(db);

            int carId = InsertCar(db, "Toyota", "Corolla", 2020, 20000);
            Console.WriteLine($"Inserted car with ID: {carId}");

            UpdateCarPrice(db, carId, 22000);
            Console.WriteLine($"Updated price of car ID: {carId}");

            List<Car> cars = GetAllCars(db);
            Console.WriteLine("Cars in database:");
            foreach (var car in cars)
            {
                Console.WriteLine($"{car.Id}: {car.Brand} {car.Model} - ${car.Price}");
            }

            List<Car> toyotaCars = GetCarsByBrand(db, "Toyota");
            Console.WriteLine("Toyota Cars:");
            foreach (var car in toyotaCars)
            {
                Console.WriteLine($"{car.Id}: {car.Brand} {car.Model} - ${car.Price}");
            }

            DeleteCar(db, carId);
            Console.WriteLine($"Deleted car with ID: {carId}");

            RunTransaction(db);
        }

    }
}

class Car
{
    public int Id { get; set; }
    public string Brand { get; set; }
    public string Model { get; set; }
    public int Year { get; set; }
    public decimal Price { get; set; }
}
