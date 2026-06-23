using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using CarRental.BLL.Interfaces.Services;
using CarRental.BLL.DTOs.Car;
using CarRental.Web.ViewModels.Car;
using CarRental.Domain.Entities;
using CarRental.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace CarRental.Web.Controllers;

public class CarController : Controller
{
    private readonly ICarService _carService;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<CarController> _logger;

    public CarController(ICarService carService, 
    UserManager<User> userManager,
    ILogger<CarController> logger)
    {
        _carService = carService;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        IEnumerable<CarDto> cars;
        if (User.IsInRole("Admin"))
        {
            cars = await _carService.GetAllCarsAsync();
        }
        else
        {
            cars = await _carService.GetAvailableCarsAsync();
        }
        
        await LoadFilterData();
        ViewBag.HasActiveFilters = false;
        ViewBag.CurrentBrand = "";
        ViewBag.CurrentModel = "";
        ViewBag.CurrentMaxPrice = "";
        ViewBag.CurrentSort = "";
        ViewBag.CurrentClass = "";
        ViewBag.CurrentFuelType = "";
        ViewBag.CurrentTransmission = "";
        ViewBag.CurrentMinSeats = "";
        return View(cars);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        if (id == Guid.Empty)
        {
            return BadRequest("Invalid car ID");
        }

        var car = await _carService.GetCarByIdAsync(id);
        if (car == null)
        {
            return NotFound();
        }

        BookCarViewModel? bookingModel = null;
        if (User.Identity?.IsAuthenticated == true)
        {
            bookingModel = new BookCarViewModel { 
                CarId = id,
                StartDate = DateTime.Today.AddDays(1),
                EndDate = DateTime.Today.AddDays(4)
            };
        }
        
        var model = (Car: car, BookingModel: bookingModel);
        
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Search(
        string brand, 
        string model, 
        decimal? maxPrice, 
        string sortBy,
        string? @class,
        string? fuelType,
        string? transmission,
        int? minSeats)
    {
        try
        {
            await LoadFilterData();
            
            ViewBag.CurrentBrand = brand ?? "";
            ViewBag.CurrentModel = model ?? "";
            ViewBag.CurrentMaxPrice = maxPrice?.ToString() ?? "";
            ViewBag.CurrentSort = sortBy ?? "";
            ViewBag.CurrentClass = @class ?? "";
            ViewBag.CurrentFuelType = fuelType ?? "";
            ViewBag.CurrentTransmission = transmission ?? "";
            ViewBag.CurrentMinSeats = minSeats?.ToString() ?? "";
            
            ViewBag.HasActiveFilters = !string.IsNullOrEmpty(brand) || 
                                    !string.IsNullOrEmpty(model) || 
                                    maxPrice.HasValue ||
                                    !string.IsNullOrEmpty(sortBy) ||
                                    !string.IsNullOrEmpty(@class) ||
                                    !string.IsNullOrEmpty(fuelType) ||
                                    !string.IsNullOrEmpty(transmission) ||
                                    minSeats.HasValue;
            
            var allCars = User.IsInRole("Admin") 
                ? await _carService.GetAllCarsAsync()
                : await _carService.GetAvailableCarsAsync();

            var filter = new CarFilterDto 
            { 
                Brand = brand ?? "", 
                Model = model ?? "", 
                MaxPrice = maxPrice,
                SortBy = sortBy ?? "",
                Class = @class,
                FuelType = fuelType,
                Transmission = transmission,
                MinSeats = minSeats
            };
            
            var cars = await _carService.GetCarsByFilterAsync(filter);
            
            return View("Index", cars);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при поиске автомобилей");
            TempData["ErrorMessage"] = "Ошибка при выполнении поиска";
            return RedirectToAction("Index");
        }
    }

    private async Task LoadFilterData()
    {
        try
        {          
            var brands = await _carService.GetUniqueBrandsAsync();
            var models = await _carService.GetUniqueModelsAsync();
            
            var brandsList = brands?.ToList() ?? new List<string>();
            var modelsList = models?.ToList() ?? new List<string>();
                
            var classesList = Enum.GetValues(typeof(CarClass))
                .Cast<CarClass>()
                .Select(c => c.ToString())
                .ToList();
                
            var fuelTypesList = Enum.GetValues(typeof(FuelType))
                .Cast<FuelType>()
                .Select(f => f.ToString())
                .ToList();
                
            var transmissionsList = Enum.GetValues(typeof(TransmissionType))
                .Cast<TransmissionType>()
                .Select(t => t.ToString())
                .ToList();
            
            ViewBag.Brands = brandsList;
            ViewBag.Models = modelsList;
            ViewBag.Classes = classesList;
            ViewBag.FuelTypes = fuelTypesList;
            ViewBag.Transmissions = transmissionsList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при загрузке данных фильтров");
            ViewBag.Brands = new List<string>();
            ViewBag.Models = new List<string>();
            ViewBag.Classes = Enum.GetValues(typeof(CarClass))
                .Cast<CarClass>()
                .Select(c => c.ToString())
                .ToList();
            ViewBag.FuelTypes = Enum.GetValues(typeof(FuelType))
                .Cast<FuelType>()
                .Select(f => f.ToString())
                .ToList();
            ViewBag.Transmissions = Enum.GetValues(typeof(TransmissionType))
                .Cast<TransmissionType>()
                .Select(t => t.ToString())
                .ToList();
        }
    }
}