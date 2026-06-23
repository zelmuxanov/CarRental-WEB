using CarRental.Domain.Entities;
using CarRental.Domain.Enums;

namespace CarRental.Domain.Interfaces.Repositories;

public interface ICarRepository : IRepository<Car>
{
    Task<IEnumerable<Car>> GetAvailableCarsAsync();
    Task<IEnumerable<Car>> GetCarsByFilterAsync(string? brand, string? model, decimal? maxPrice);
    Task<IEnumerable<Car>> GetFilteredCarsAsync(
        string? brand,
        string? model,
        decimal? maxPrice,
        CarClass? carClass,
        FuelType? fuelType,
        TransmissionType? transmission,
        int? minSeats,
        int? maxSeats,
        string? sortBy);
    Task<bool> IsCarAvailableAsync(Guid carId, DateTime startDate, DateTime endDate);
    Task<IEnumerable<string>> GetUniqueBrandsAsync();
    Task<IEnumerable<string>> GetUniqueModelsAsync();
    Task<int> GetMinSeatsAsync();
    Task<int> GetMaxSeatsAsync();
    Task<Car?> GetByIdWithImagesAsync(Guid id);
    Task<IEnumerable<Car>> GetAllWithImagesAsync();
    Task<IEnumerable<Car>> GetAvailableForDateRangeAsync(DateTime startDate, DateTime endDate);
}