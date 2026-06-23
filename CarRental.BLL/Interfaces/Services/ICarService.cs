using CarRental.BLL.DTOs.Car;
using CarRental.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace CarRental.BLL.Interfaces.Services;

public interface ICarService
{
    // Основные методы для каталога
    Task<IEnumerable<CarDto>> GetAllCarsAsync();
    Task<CarDto?> GetCarByIdAsync(Guid id);
    Task<IEnumerable<CarDto>> GetCarsByFilterAsync(CarFilterDto filter);
    Task<IEnumerable<string>> GetUniqueBrandsAsync();
    Task<IEnumerable<string>> GetUniqueModelsAsync();
    Task<bool> IsCarAvailableAsync(Guid carId, DateTime startDate, DateTime endDate);
    Task<IEnumerable<CarDto>> GetAvailableCarsForDateRangeAsync(DateTime startDate, DateTime endDate);
    
    // Методы для админ-панели
    Task<IEnumerable<CarDto>> GetAvailableCarsAsync();
    Task<CarDto> CreateCarAsync(CarCreateDto createDto);
    Task<bool> UpdateCarAsync(Guid id, CarUpdateDto updateDto);
    Task<bool> DeleteCarAsync(Guid id);
    Task<int> GetMinSeatsAsync();
    Task<int> GetMaxSeatsAsync();
    
    // Методы для фильтров
    Task<IEnumerable<CarClass>> GetUniqueClassesAsync();
    Task<IEnumerable<FuelType>> GetUniqueFuelTypesAsync();
    Task<IEnumerable<TransmissionType>> GetUniqueTransmissionsAsync();
    
    // Управление изображениями
    Task<string> UploadTempImageAsync(IFormFile file);
    Task<bool> DeleteCarImageAsync(Guid imageId);
    Task<bool> SetMainImageAsync(Guid imageId);
    Task<IEnumerable<CarImageDto>> GetCarImagesAsync(Guid carId);
    Task<bool> ReorderCarImagesAsync(List<Guid> orderedImageIds);
    Task<bool> UpdateImageOrderAsync(Guid imageId, int newOrder);
    Task<bool> UpdateImagesOrderAsync(List<Guid> orderedImageIds);
    Task<IEnumerable<CarDto>> SearchCarsAsync(string query);
}