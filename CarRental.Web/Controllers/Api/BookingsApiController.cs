using Microsoft.AspNetCore.Mvc;
using CarRental.BLL.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.RateLimiting;

namespace CarRental.Web.Controllers.Api
{
    [Route("api/bookings")]
    [ApiController]
    public class BookingsApiController : ControllerBase
    {
        private readonly IBookingService _bookingService;
        private readonly ILogger<BookingsApiController> _logger;

        public BookingsApiController(
            IBookingService bookingService,
            ILogger<BookingsApiController> logger)
        {
            _bookingService = bookingService;
            _logger = logger;
        }

        [HttpGet("occupied-dates")]
        [EnableRateLimiting("api")]
        public async Task<IActionResult> GetOccupiedDates(Guid carId)
        {
            try
            {
                var bookings = await _bookingService.GetBookingsForCarAsync(carId);
                var disabledDates = new HashSet<string>();
                foreach (var b in bookings)
                {
                    for (var d = b.StartDate; d <= b.EndDate; d = d.AddDays(1))
                        disabledDates.Add(d.ToString("yyyy-MM-dd"));
                }
                return Ok(disabledDates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении занятых дат для автомобиля {CarId}", carId);
                return StatusCode(500, new { error = "Ошибка при получении данных" });
            }
        }
    }
}