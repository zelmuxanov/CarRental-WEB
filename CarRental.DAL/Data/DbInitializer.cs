using CarRental.DAL.Data;
using CarRental.Domain.Entities;
using CarRental.Domain.Enums;

namespace CarRental.DAL.Data;

public static class DbInitializer
{
    public static void Initialize(ApplicationDbContext context)
    {
        //ВРЕМЕННО закомментируем проверку для теста
        if (context.Cars.Any())
        {
            return; 
        }

        try
        {
            var cars = new Car[]
            {
                // Volkswagen Caddy
                new Car 
                { 
                    Id = Guid.NewGuid(),
                    Brand = "Volkswagen", 
                    Model = "Caddy", 
                    Year = 2018,
                    PricePerDay = 4000,
                    IsAvailable = true,
                    Class = CarClass.Minivan, 
                    Transmission = TransmissionType.Manual, 
                    FuelType = FuelType.Petrol,
                    Seats = 5,
                    EngineCapacity = 2.0,
                    Color = "белый",
                    Description = "Volkswagen Caddy 2018 года. Надежный минивэн для перевозок.",
                    CreatedAt = DateTime.UtcNow
                },
                // Haval Jolion
                new Car 
                { 
                    Id = Guid.NewGuid(),
                    Brand = "HAVAL", 
                    Model = "JOLION", 
                    Year = 2022,
                    PricePerDay = 5000,
                    IsAvailable = true,
                    Class = CarClass.SUV,
                    Transmission = TransmissionType.Automatic, 
                    FuelType = FuelType.Petrol,
                    Seats = 5,
                    EngineCapacity = 1.5,
                    Color = "белый",
                    Description = "HAVAL JOLION 2022 года. Современный кроссовер с автоматической коробкой.",
                    CreatedAt = DateTime.UtcNow
                }
                // Оставьте только 2 автомобиля для теста
            };

            context.Cars.AddRange(cars);
            context.SaveChanges();
            
            Console.WriteLine($"✅ Added {cars.Length} cars to database");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error seeding database: {ex.Message}");
            // Не бросаем исключение дальше - позволяем приложению запуститься
        }
    }

    private static string GenerateVIN()
    {
        return Guid.NewGuid().ToString("N").Substring(0, 17).ToUpper();
    }
}