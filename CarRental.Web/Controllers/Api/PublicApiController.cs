using Microsoft.AspNetCore.Mvc;
using CarRental.BLL.Interfaces.Services;
using Microsoft.AspNetCore.RateLimiting;

namespace CarRental.Web.Controllers.Api;

[ApiController]
[Route("api/public")]
public class PublicApiController : ControllerBase
{
    private readonly ICarService _carService;

    public PublicApiController(ICarService carService)
    {
        _carService = carService;
    }

    [HttpGet("free-cars")]
    [EnableRateLimiting("api")]
    public async Task<IActionResult> GetFreeCars(DateTime startDate, DateTime endDate)
    {
        // Валидация входных дат
        if (endDate <= startDate)
            return BadRequest(new { error = "Дата окончания должна быть позже даты начала" });
        
        if (startDate.Date < DateTime.UtcNow.Date)
            return BadRequest(new { error = "Дата начала не может быть в прошлом" });

        var utcStart = DateTime.SpecifyKind(startDate.Date, DateTimeKind.Utc);
        var utcEnd = DateTime.SpecifyKind(endDate.Date, DateTimeKind.Utc);

        // Один запрос вместо N+1
        var freeCars = await _carService.GetAvailableCarsForDateRangeAsync(utcStart, utcEnd);
        
        var result = freeCars.Select(car => new
        {
            car.Id,
            car.Brand,
            car.Model,
            car.Year,
            car.PricePerDay,
            car.MainImageUrl,
            car.LicensePlate
        });

        return Ok(result);
    }
}