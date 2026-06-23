using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using CarRental.BLL.Interfaces.Services;
using CarRental.Domain.Entities;
using CarRental.BLL.DTOs.Booking;
using CarRental.Web.ViewModels.Car;
using CarRental.Domain.Enums;
using CarRental.Web.ViewModels.Home;
using Microsoft.AspNetCore.RateLimiting;

namespace CarRental.Web.Controllers;

[Authorize]
[Route("Booking")]
public class BookingController : Controller
{
    private readonly IBookingService _bookingService;
    private readonly ICarService _carService;
    private readonly UserManager<User> _userManager;
    private readonly IEmailService _emailService;
    private readonly ILogger<BookingController> _logger;

    public BookingController(
        IBookingService bookingService,
        ICarService carService,
        UserManager<User> userManager,
        IEmailService emailService,
        ILogger<BookingController> logger)
    {
        _bookingService = bookingService;
        _carService = carService;
        _userManager = userManager;
        _emailService = emailService;
        _logger = logger;
    }

    private Guid UserId => Guid.Parse(_userManager.GetUserId(User) ?? throw new UnauthorizedAccessException());

    [HttpPost("BookCar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BookCar(BookCarViewModel model)
    {
        bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";
        
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                    
                
                return Json(new { 
                    success = false, 
                    message = "Ошибки валидации: " + string.Join(", ", errors) 
                });
            }

            var car = await _carService.GetCarByIdAsync(model.CarId);
            if (car == null || !car.IsAvailable)
            {
                
                return Json(new { 
                    success = false, 
                    message = "Автомобиль недоступен для бронирования" 
                });
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                
                return Json(new { 
                    success = false, 
                    message = "Пользователь не найден" 
                });
            }


            // Проверяем, активирован ли пользователь
            if (user.Status != UserStatus.Active)
            {
                
                return Json(new { 
                    success = false, 
                    message = "Ваш аккаунт не активирован. Обратитесь к администратору для активации аккаунта." 
                });
            }

            // ПРОВЕРКА: ОДНО БРОНИРОВАНИЕ НА ЧЕЛОВЕКА
            var userActiveBookings = await _bookingService.GetUserBookingsAsync(user.Id);
            var hasActiveBooking = userActiveBookings.Any(b => 
                b.Status == BookingStatus.Pending || 
                b.Status == BookingStatus.Confirmed ||
                b.Status == BookingStatus.Active);
            
            if (hasActiveBooking)
            {
                
                return Json(new { 
                    success = false, 
                    message = "У вас уже есть активное бронирование. Вы не можете создать новое, пока текущее не завершено." 
                });
            }

            // Расчет стоимости
            var days = (model.EndDate - model.StartDate).Days;
            if (days < 1) days = 1;
            
            var basePrice = car.PricePerDay * days;
            var deliveryPrice = model.NeedDelivery ? GetDeliveryPrice(model.DeliveryLocation) : 0;
            var unlimitedPrice = model.UnlimitedMileage ? 2000 * days : 0;
            var totalPrice = basePrice + deliveryPrice + unlimitedPrice;


            var bookingDto = new BookingRequestDto
            {
                CarId = model.CarId,
                UserId = user.Id,
                StartDate = DateTime.SpecifyKind(model.StartDate, DateTimeKind.Utc),
                EndDate = DateTime.SpecifyKind(model.EndDate, DateTimeKind.Utc),
                Notes = $"Доставка: {(model.NeedDelivery ? model.DeliveryLocation?.ToString() : "Нет")}, " +
                        $"Безлимит: {(model.UnlimitedMileage ? "Да" : "Нет")}",
                TotalPrice = totalPrice
            };

            var result = await _bookingService.CreateBookingAsync(bookingDto);
            
            if (result != null)
            {
                
                // ✅ УЛУЧШЕННОЕ УВЕДОМЛЕНИЕ В ТЕЛЕГРАМ
                //await SendEnhancedTelegramNotification(result, user, car);
                
                return Json(new 
                { 
                    success = true, 
                    message = $"✅ Автомобиль успешно забронирован!<br>" +
                            $"Стоимость: {totalPrice}₽<br>" +
                            $"Период: {model.StartDate:dd.MM.yyyy} - {model.EndDate:dd.MM.yyyy}<br>" +
                            $"Менеджер свяжется с вами для подтверждения",
                    redirectUrl = Url.Action("Bookings", "Profile")
                });
            }
            else
            {               
                return Json(new { 
                    success = false, 
                    message = "Ошибка при создании бронирования" 
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при создании бронирования для пользователя {UserId}", UserId);
            return Json(new { 
                success = false, 
                message = "Произошла ошибка при бронировании. Попробуйте позже или обратитесь к менеджеру." 
            });
        }
    }

    [HttpPost("Cancel/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(Guid id)
    {
        try
        {
            var booking = await _bookingService.GetBookingByIdAsync(id);
            if (booking == null)
                return Json(new { success = false, message = "Бронирование не найдено" });

            var userIdString = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userIdString))
                return Json(new { success = false, message = "Пользователь не найден" });

            var userId = Guid.Parse(userIdString);
            if (booking.UserId != userId)
                return Json(new { success = false, message = "Нет доступа к этому бронированию" });

            if (booking.Status != BookingStatus.Pending && 
                booking.Status != BookingStatus.Confirmed)
                return Json(new { success = false, message = "Невозможно отменить это бронирование" });

            var result = await _bookingService.CancelBookingAsync(id);
            
            if (result)
            {                
                return Json(new { success = true, message = "Бронирование отправлено на отмену" });
            }
            
            return Json(new { success = false, message = "Ошибка при отмене бронирования" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling booking {BookingId}", id);
            return Json(new { success = false, message = "Произошла ошибка при отмене бронирования" });
        }
    }

    [HttpPost("Confirm/{id:guid}")]
    [Authorize(Roles = "Admin,Manager")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(Guid id)
    {
        try
        {
            var booking = await _bookingService.GetBookingByIdAsync(id);
            if (booking == null)
                return Json(new { success = false, message = "Бронирование не найдено" });

            var result = await _bookingService.ConfirmBookingAsync(id);
            
            if (result)
            {              
                return Json(new { success = true, message = "Бронирование подтверждено" });
            }
            
            return Json(new { success = false, message = "Ошибка при подтверждении бронирования" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming booking {BookingId}", id);
            return Json(new { success = false, message = "Произошла ошибка при подтверждении бронирования" });
        }
    }

    private decimal GetDeliveryPrice(DeliveryLocation? location)
    {
        return location switch
        {
            DeliveryLocation.Sheremetyevo => 4500,
            DeliveryLocation.Vnukovo => 3500,
            DeliveryLocation.Domodedovo => 6000,
            DeliveryLocation.Moscow => 0,
            _ => 0
        };
    }

    [HttpGet("Details/{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Details(Guid id)
    {
        var booking = await _bookingService.GetBookingByIdAsync(id);
        if (booking == null) return NotFound();

        var currentUserId = UserId;
        if (booking.UserId != currentUserId && !User.IsInRole("Admin"))
            return Forbid();

        return View(booking);
    }
    [HttpPost("QuickBook")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("quickbook")]
    public async Task<IActionResult> QuickBook([FromBody] QuickBookingViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            return BadRequest(new { success = false, errors = errors });
        }

        var car = await _carService.GetCarByIdAsync(model.CarId);
        if (car == null)
            return BadRequest(new { success = false, message = "Автомобиль не найден" });

        var utcStartDate = DateTime.SpecifyKind(model.StartDate, DateTimeKind.Utc);
        var utcEndDate = DateTime.SpecifyKind(model.EndDate, DateTimeKind.Utc);

        var isAvailable = await _carService.IsCarAvailableAsync(model.CarId, utcStartDate, utcEndDate);
        if (!isAvailable)
            return BadRequest(new { success = false, message = "Автомобиль уже занят на выбранные даты" });

        // Расчёт стоимости
        int days = Math.Max(1, (model.EndDate - model.StartDate).Days);
        decimal rate = car.PricePerDay;
        if (days >= 30 && car.PricePerDay30 > 0) rate = car.PricePerDay30;
        else if (days >= 15 && car.PricePerDay15 > 0) rate = car.PricePerDay15;
        
        decimal basePrice = rate * days;
        decimal deliveryPrice = GetDeliveryPriceForLocation(model.DeliveryLocation);
        decimal unlimitedPrice = model.UnlimitedMileage ? (car.UnlimitedMileagePrice > 0 ? car.UnlimitedMileagePrice : 2000) * days : 0;
        decimal totalPrice = basePrice + deliveryPrice + unlimitedPrice + car.Deposit;

        string notes = $"Клиент: {model.Name}, Телефон: {model.Phone}, Email: {model.Email}";
        if (model.UnlimitedMileage) notes += ", Безлимитный пробег";
        if (!string.IsNullOrEmpty(model.DeliveryLocation)) notes += $", Доставка: {model.DeliveryLocation}";

        var adminDto = new AdminBookingDto
        {
            CarId = model.CarId,
            UserId = null,
            StartDate = utcStartDate,
            EndDate = utcEndDate,
            TotalPrice = totalPrice,
            Notes = notes,
            Status = BookingStatus.Pending
        };

        try
        {
            var booking = await _bookingService.CreateAdminBookingAsync(adminDto);
            
            // Отправка уведомления
            var carDetails = $"{car.Brand} {car.Model} ({car.Year})";
            var subject = $"⚡ Быстрое бронирование: {carDetails}";
            var message = $@"
                <p><strong>Имя клиента:</strong> {model.Name}</p>
                <p><strong>Телефон:</strong> {model.Phone}</p>
                <p><strong>Email:</strong> {model.Email}</p>
                <p><strong>Автомобиль:</strong> {carDetails}</p>
                <p><strong>Период:</strong> {utcStartDate:dd.MM.yyyy} – {utcEndDate:dd.MM.yyyy}</p>
                <p><strong>Стоимость:</strong> {totalPrice:N0} ₽</p>
                <p><small>Заявка создана {DateTime.UtcNow:dd.MM.yyyy HH:mm} UTC</small></p>";
            try { await _emailService.SendManagerNotificationAsync(subject, message); } catch { }

            return Ok(new { success = true, message = "Заявка отправлена! Менеджер свяжется с вами." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Quick booking error");
            return StatusCode(500, new { success = false, message = "Ошибка сервера. Попробуйте позже." });
        }
    }

    private decimal GetDeliveryPriceForLocation(string? location)
    {
        return location switch
        {
            "Sheremetyevo" => 4500,
            "Vnukovo" => 3500,
            "Domodedovo" => 6000,
            "Moscow" => 0,
            _ => 0
        };
    }
}