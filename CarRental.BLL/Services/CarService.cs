using AutoMapper;
using CarRental.BLL.Interfaces.Services;
using CarRental.BLL.DTOs.Car;
using CarRental.Domain.Interfaces.Repositories;
using CarRental.Domain.Entities;
using CarRental.Domain.Enums;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CarRental.BLL.Services;

public class CarService : ICarService
{
    private readonly ICarRepository _carRepository;
    private readonly IMapper _mapper;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<CarService> _logger;
    private readonly ICarImageRepository _carImageRepository;
    private readonly IFileStorageService _fileStorageService;

    public CarService(
        ICarRepository carRepository, 
        IMapper mapper,
        IWebHostEnvironment environment,
        ILogger<CarService> logger,
        ICarImageRepository carImageRepository,
        IFileStorageService fileStorageService)
    {
        _carRepository = carRepository;
        _mapper = mapper;
        _environment = environment;
        _logger = logger;
        _carImageRepository = carImageRepository;
        _fileStorageService = fileStorageService;
    }

    public async Task<CarDto> CreateCarAsync(CarCreateDto createDto)
    {
        if (createDto == null) throw new ArgumentNullException(nameof(createDto));

        var car = _mapper.Map<Car>(createDto);
        car.Id = Guid.NewGuid();
        car.CreatedAt = DateTime.UtcNow;

        // Сохраняем автомобиль
        await _carRepository.AddAsync(car);
        await _carRepository.SaveChangesAsync();

        // Загружаем изображения, если есть
        if (createDto.ImageFiles != null && createDto.ImageFiles.Any())
        {
            await UploadCarImagesAsync(car.Id, createDto.ImageFiles);
        }

        return _mapper.Map<CarDto>(car);
    }

    private async Task<string> SaveCarImageAsync(IFormFile imageFile, Guid carId)
    {
        try
        {
            // Проверяем файл
            if (imageFile == null || imageFile.Length == 0)
                throw new ArgumentException("Файл не выбран");
            
            // Проверяем размер (макс. 5MB)
            const long maxFileSize = 5 * 1024 * 1024;
            if (imageFile.Length > maxFileSize)
                throw new InvalidOperationException("Размер файла не должен превышать 5MB");
            
            // Проверяем расширение
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
            
            if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension))
                throw new InvalidOperationException(
                    "Допустимые форматы файлов: JPG, JPEG, PNG, GIF");
            
            // Создаем папку для загрузок
            var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads", "cars");
            
            if (!Directory.Exists(uploadsPath))
            {
                Directory.CreateDirectory(uploadsPath);
            }
            
            // Генерируем уникальное имя файла
            var fileName = $"{carId}_{DateTime.UtcNow:yyyyMMddHHmmss}{extension}";
            var filePath = Path.Combine(uploadsPath, fileName);
            
            // Сохраняем файл
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(stream);
            }
            
            // Возвращаем относительный путь
            return $"/uploads/cars/{fileName}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Ошибка при сохранении изображения");
            throw;
        }
    }

    public async Task<bool> UpdateCarAsync(Guid id, CarUpdateDto updateDto)
    {
        var car = await _carRepository.GetByIdAsync(id);
        if (car == null) return false;

        // Обновление полей (можно вынести в маппер, но оставим как у вас)
        if (!string.IsNullOrEmpty(updateDto.Brand)) car.Brand = updateDto.Brand;
        if (!string.IsNullOrEmpty(updateDto.Model)) car.Model = updateDto.Model;
        if (updateDto.Year.HasValue) car.Year = updateDto.Year.Value;
        if (!string.IsNullOrEmpty(updateDto.Color)) car.Color = updateDto.Color;
        if (updateDto.PricePerDay.HasValue) car.PricePerDay = updateDto.PricePerDay.Value;
        if (updateDto.IsAvailable.HasValue) car.IsAvailable = updateDto.IsAvailable.Value;
        if (!string.IsNullOrEmpty(updateDto.Description)) car.Description = updateDto.Description;
        if (updateDto.Class.HasValue) car.Class = updateDto.Class.Value;
        if (updateDto.Transmission.HasValue) car.Transmission = updateDto.Transmission.Value;
        if (updateDto.FuelType.HasValue) car.FuelType = updateDto.FuelType.Value;
        if (updateDto.Seats.HasValue) car.Seats = updateDto.Seats.Value;
        if (updateDto.EngineCapacity.HasValue) car.EngineCapacity = updateDto.EngineCapacity.Value;
        if (!string.IsNullOrEmpty(updateDto.LicensePlate)) car.LicensePlate = updateDto.LicensePlate;   
        if (!string.IsNullOrEmpty(updateDto.VIN)) car.VIN = updateDto.VIN;
        if (updateDto.PricePerDay15.HasValue) car.PricePerDay15 = updateDto.PricePerDay15.Value;
        if (updateDto.PricePerDay30.HasValue) car.PricePerDay30 = updateDto.PricePerDay30.Value;
        if (updateDto.Deposit.HasValue) car.Deposit = updateDto.Deposit.Value;
        if (updateDto.MileageLimitPerDay.HasValue) car.MileageLimitPerDay = updateDto.MileageLimitPerDay.Value;
        if (updateDto.OverMileagePricePerKm.HasValue) car.OverMileagePricePerKm = updateDto.OverMileagePricePerKm.Value;
        if (updateDto.UnlimitedMileagePrice.HasValue) car.UnlimitedMileagePrice = updateDto.UnlimitedMileagePrice.Value;
            

        car.UpdatedAt = DateTime.UtcNow;

        // Добавление новых изображений
        if (updateDto.ImageFiles != null && updateDto.ImageFiles.Any())
        {
            var currentCount = await _carImageRepository.CountByCarIdAsync(id);
            if (currentCount + updateDto.ImageFiles.Count > 10)
                throw new InvalidOperationException("Максимальное количество изображений – 10");
            await UploadCarImagesAsync(id, updateDto.ImageFiles);
        }

        _carRepository.Update(car);
        return await _carRepository.SaveChangesAsync();
    }

    private void DeleteCarImage(string imageUrl)
    {
        try
        {
            if (string.IsNullOrEmpty(imageUrl))
                return;
            
            // Преобразуем относительный URL в абсолютный путь
            var relativePath = imageUrl.TrimStart('/');
            var absolutePath = Path.Combine(_environment.WebRootPath, relativePath);
            
            if (File.Exists(absolutePath))
            {
                File.Delete(absolutePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ Не удалось удалить старое изображение");
        }
    }

    public async Task<bool> DeleteCarAsync(Guid id)
    {
        var car = await _carRepository.GetByIdWithImagesAsync(id);
        if (car == null) return false;

        foreach (var img in car.Images)
            _fileStorageService.DeleteFile(img.ImageUrl);

        _carRepository.Delete(car);
        return await _carRepository.SaveChangesAsync();
    }

    // Остальные методы
    public async Task<IEnumerable<CarDto>> GetAllCarsAsync()
    {
        var cars = await _carRepository.GetAllWithImagesAsync();
        return _mapper.Map<IEnumerable<CarDto>>(cars);
    }

    public async Task<CarDto?> GetCarByIdAsync(Guid id)
    {
        var car = await _carRepository.GetByIdWithImagesAsync(id);
        return _mapper.Map<CarDto?>(car);
    }

    public async Task<IEnumerable<CarDto>> GetCarsByFilterAsync(CarFilterDto filter)
    {
        try
        {
            _logger.LogInformation("🔍 Применение фильтра: Brand={Brand}, Model={Model}, MaxPrice={MaxPrice}, Class={Class}", 
                filter.Brand, filter.Model, filter.MaxPrice, filter.Class);

            // Преобразуем строковые значения в Enum
            CarClass? carClass = null;
            if (!string.IsNullOrEmpty(filter.Class) && 
                Enum.TryParse<CarClass>(filter.Class, out var parsedClass))
            {
                carClass = parsedClass;
            }

            FuelType? fuelType = null;
            if (!string.IsNullOrEmpty(filter.FuelType) && 
                Enum.TryParse<FuelType>(filter.FuelType, out var parsedFuelType))
            {
                fuelType = parsedFuelType;
            }

            TransmissionType? transmission = null;
            if (!string.IsNullOrEmpty(filter.Transmission) && 
                Enum.TryParse<TransmissionType>(filter.Transmission, out var parsedTransmission))
            {
                transmission = parsedTransmission;
            }

            // Вызываем метод репозитория
            var cars = await _carRepository.GetFilteredCarsAsync(
                brand: filter.Brand,
                model: filter.Model,
                maxPrice: filter.MaxPrice,
                carClass: carClass,
                fuelType: fuelType,
                transmission: transmission,
                minSeats: filter.MinSeats,
                maxSeats: filter.MaxSeats,
                sortBy: filter.SortBy
            );

            _logger.LogInformation("✅ Найдено {Count} автомобилей после фильтрации", cars.Count());

            return _mapper.Map<IEnumerable<CarDto>>(cars);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 ОШИБКА в CarService.GetCarsByFilterAsync");
            throw;
        }
    }

    public async Task<IEnumerable<string>> GetUniqueBrandsAsync()
    {
        try
        {
            var brands = await _carRepository.GetUniqueBrandsAsync();
            _logger.LogInformation("📊 GetUniqueBrandsAsync: {Count} brands found", brands?.Count() ?? 0);
            return brands ?? new List<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Ошибка при получении уникальных марок");
            return new List<string>();
        }
    }

    public async Task<IEnumerable<string>> GetUniqueModelsAsync()
    {
        try
        {
            var models = await _carRepository.GetUniqueModelsAsync();
            _logger.LogInformation("📊 GetUniqueModelsAsync: {Count} models found", models?.Count() ?? 0);
            return models ?? new List<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Ошибка при получении уникальных моделей");
            return new List<string>();
        }
    }

    public async Task<bool> IsCarAvailableAsync(Guid carId, DateTime startDate, DateTime endDate)
    {
        return await _carRepository.IsCarAvailableAsync(carId, startDate, endDate);
    }

    public async Task<IEnumerable<CarDto>> GetAvailableCarsAsync()
    {
        var cars = await _carRepository.GetAvailableCarsAsync();
        return _mapper.Map<IEnumerable<CarDto>>(cars);
    }

    public async Task<int> GetMinSeatsAsync()
    {
        return await _carRepository.GetMinSeatsAsync();
    }

    public async Task<int> GetMaxSeatsAsync()
    {
        return await _carRepository.GetMaxSeatsAsync();
    }

    public async Task<IEnumerable<CarClass>> GetUniqueClassesAsync()
    {
        var cars = await _carRepository.GetAllAsync();
        return cars.Select(c => c.Class).Distinct().ToList();
    }

    public async Task<IEnumerable<FuelType>> GetUniqueFuelTypesAsync()
    {
        var cars = await _carRepository.GetAllAsync();
        return cars.Select(c => c.FuelType).Distinct().ToList();
    }

    public async Task<IEnumerable<TransmissionType>> GetUniqueTransmissionsAsync()
    {
        var cars = await _carRepository.GetAllAsync();
        return cars.Select(c => c.Transmission).Distinct().ToList();
    }

    private async Task UploadCarImagesAsync(Guid carId, IEnumerable<IFormFile> files)
    {
        int order = 0;
        foreach (var file in files)
        {
            if (file == null || file.Length == 0) continue;
            order++;
            var url = await _fileStorageService.SaveFileAsync(file, "cars");
            var image = new CarImage
            {
                Id = Guid.NewGuid(),
                CarId = carId,
                ImageUrl = url,
                FileName = file.FileName,
                DisplayOrder = order,
                IsMain = order == 1 // первое фото становится главным
            };
            await _carImageRepository.AddAsync(image);
        }
        if (order > 0)
            await _carImageRepository.SaveChangesAsync();
    }

    public async Task<bool> DeleteCarImageAsync(Guid imageId)
    {
        var image = await _carImageRepository.GetByIdAsync(imageId);
        if (image == null) return false;
        _fileStorageService.DeleteFile(image.ImageUrl);
        _carImageRepository.Delete(image);
        await _carImageRepository.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SetMainImageAsync(Guid imageId)
    {
        return await _carImageRepository.SetMainAsync(imageId);
    }

    public async Task<IEnumerable<CarImageDto>> GetCarImagesAsync(Guid carId)
    {
        var images = await _carImageRepository.GetByCarIdAsync(carId);
        return _mapper.Map<IEnumerable<CarImageDto>>(images);
    }

    public async Task<string> UploadTempImageAsync(IFormFile file)
    {
        // Сохраняем во временную папку (cars/temp)
        return await _fileStorageService.SaveFileAsync(file, "cars/temp");
    }
    public async Task<bool> ReorderCarImagesAsync(List<Guid> orderedImageIds)
    {
        if (orderedImageIds == null || !orderedImageIds.Any()) return false;
        for (int i = 0; i < orderedImageIds.Count; i++)
        {
            var image = await _carImageRepository.GetByIdAsync(orderedImageIds[i]);
            if (image != null)
            {
                image.DisplayOrder = i;
                _carImageRepository.Update(image);
            }
        }
        await _carImageRepository.SaveChangesAsync();
        return true;
    }
    public async Task<bool> UpdateImageOrderAsync(Guid imageId, int newOrder)
    {
        await _carImageRepository.UpdateDisplayOrderAsync(imageId, newOrder);
        return true;
    }
    public async Task<bool> UpdateImagesOrderAsync(List<Guid> orderedImageIds)
    {
        for (int i = 0; i < orderedImageIds.Count; i++)
        {
            var image = await _carImageRepository.GetByIdAsync(orderedImageIds[i]);
            if (image != null)
            {
                image.DisplayOrder = i;
                _carImageRepository.Update(image);
            }
        }
        await _carImageRepository.SaveChangesAsync();
        return true;
    }
    public async Task<IEnumerable<CarDto>> SearchCarsAsync(string query)
    {
        var cars = await _carRepository.GetAllWithImagesAsync(); // или более эффективный запрос
        if (string.IsNullOrWhiteSpace(query)) return _mapper.Map<IEnumerable<CarDto>>(cars.Take(20));
        query = query.ToLower();
        cars = cars.Where(c => c.Brand.ToLower().Contains(query) 
                            || c.Model.ToLower().Contains(query)
                            || c.LicensePlate.ToLower().Contains(query))
                .Take(10)
                .ToList();
        return _mapper.Map<IEnumerable<CarDto>>(cars);
    }
    public async Task<IEnumerable<CarDto>> GetAvailableCarsForDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        var cars = await _carRepository.GetAvailableForDateRangeAsync(startDate, endDate);
        return _mapper.Map<IEnumerable<CarDto>>(cars);
    }
}