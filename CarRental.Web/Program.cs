using Microsoft.EntityFrameworkCore;
using CarRental.DAL.Data;
using CarRental.DAL.Repositories;
using CarRental.BLL.Services;
using CarRental.BLL.Interfaces.Services;
using CarRental.Domain.Interfaces.Repositories;
using CarRental.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using AutoMapper;
using System.Globalization;
using CarRental.BLL.Options;
using CarRental.Web.Hubs;
using CarRental.Web.Services;
using System.IO;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

// ================== ЛОКАЛИЗАЦИЯ ==================
var culture = new CultureInfo("ru-RU");
culture.NumberFormat.NumberGroupSeparator = " ";
culture.NumberFormat.NumberDecimalSeparator = ",";
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[] { culture };
    options.DefaultRequestCulture = new RequestCulture(culture);
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
    options.RequestCultureProviders = new List<IRequestCultureProvider>
    {
        new QueryStringRequestCultureProvider(),
        new CookieRequestCultureProvider()
    };
});

// ================== БАЗОВЫЕ СЕРВИСЫ ==================
builder.Services.AddControllersWithViews()
    .AddRazorOptions(options =>
    {
        options.AreaViewLocationFormats.Add("/Areas/{2}/Views/{1}/{0}.cshtml");
        options.AreaViewLocationFormats.Add("/Areas/{2}/Views/Shared/{0}.cshtml");
        options.AreaViewLocationFormats.Add("/Views/Shared/{0}.cshtml");
    });
builder.Configuration.AddEnvironmentVariables();
builder.Services.AddWindowsService(options =>
    {
        options.ServiceName = "CarRental";
    });

if (!builder.Environment.IsDevelopment())
{
    builder.Services.AddHsts(options =>
    {
        options.Preload = true;
        options.IncludeSubDomains = true;
        options.MaxAge = TimeSpan.FromDays(365);
    });
}

// HTTPS редирект будет использоваться в продакшене (если не за прокси)
if (!builder.Environment.IsDevelopment())
{
    builder.Services.AddHttpsRedirection(options =>
    {
        options.HttpsPort = 443;
    });
}

// ================== БАЗА ДАННЫХ ==================
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("CarRental.DAL")));

// ================== IDENTITY ==================
builder.Services.AddIdentity<User, IdentityRole<Guid>>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;
    options.SignIn.RequireConfirmedEmail = true;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
    options.User.RequireUniqueEmail = true;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders()
.AddRoles<IdentityRole<Guid>>();

// ================== DATA PROTECTION ==================
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "keys")))
    .SetApplicationName("CarRental");

// ================== ОПЦИИ ==================
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection(EmailOptions.SectionName));
builder.Services.Configure<BookingEmailOptions>(builder.Configuration.GetSection(BookingEmailOptions.SectionName));
builder.Services.Configure<ManagerOptions>(builder.Configuration.GetSection(ManagerOptions.SectionName));
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10 * 1024 * 1024; // 10 MB
});

// ================== HTTP-КЛИЕНТЫ ==================
builder.Services.AddHttpClient("TelegramBot");

// ================== РЕПОЗИТОРИИ ==================
builder.Services.AddScoped<ICarRepository, CarRepository>();
builder.Services.AddScoped<ICarImageRepository, CarImageRepository>();
builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddScoped<IBannerRepository, BannerRepository>();
builder.Services.AddScoped<IChatRepository, ChatRepository>();
builder.Services.AddScoped<IChatMessageRepository, ChatMessageRepository>();
builder.Services.AddScoped<IFaqRepository, FaqRepository>();
builder.Services.AddScoped<IPageRepository, PageRepository>();

// ================== СЕРВИСЫ ==================
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ICarService, CarService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IBannerService, BannerService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IFaqService, FaqService>();
builder.Services.AddScoped<IPageService, PageService>();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddScoped<IFileEncryptionService, AesGcmFileEncryptionService>();
builder.Services.AddScoped<IContractService, ContractService>();
builder.Services.AddScoped<ISiteSettingsService, SiteSettingsService>();
builder.Services.AddSingleton<ILogViewerService, LogViewerService>();

// ================== ЛОГИРОВАНИЕ ОШИБОК ==================
var logProvider = new InMemoryLogProvider();
builder.Services.AddSingleton<InMemoryLogProvider>(logProvider);
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();            // дублировать в консоль
    logging.AddProvider(logProvider);
});

// ================== ДОПОЛНИТЕЛЬНО ==================
builder.Services.AddHttpContextAccessor();
builder.Services.AddSignalR();
builder.Services.AddAutoMapper(typeof(CarRental.BLL.Mapping.MappingProfile).Assembly);
builder.Services.AddHostedService<ChatCleanupService>();
builder.Services.AddHostedService<BookingExpirationService>();

// ================== RATE LIMITING ==================
builder.Services.AddRateLimiter(options =>
{
    // Для входа и регистрации — не более 5 запросов в минуту с одного IP
    options.AddFixedWindowLimiter("auth", o =>
    {
        o.PermitLimit = 5;
        o.Window = TimeSpan.FromMinutes(1);
        o.QueueLimit = 0;
    });

    // Для QuickBook — не более 3 заявок в 10 минут с одного IP
    options.AddFixedWindowLimiter("quickbook", o =>
    {
        o.PermitLimit = 3;
        o.Window = TimeSpan.FromMinutes(10);
        o.QueueLimit = 0;
    });

    // Глобальный лимит для API
    options.AddFixedWindowLimiter("api", o =>
    {
        o.PermitLimit = 60;
        o.Window = TimeSpan.FromMinutes(1);
        o.QueueLimit = 0;
    });

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsync(
            "Слишком много запросов. Попробуйте позже.", token);
    };
});

var app = builder.Build();

// ================== ИНИЦИАЛИЗАЦИЯ БД ==================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        await context.Database.MigrateAsync();
        
        CarRental.DAL.Data.DbInitializer.Initialize(context);
        await CarRental.DAL.Data.IdentitySeeder.SeedAsync(services);
        
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("✅ Database migrated and seeded successfully!");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "❌ Database migration/seeding error");
        throw; // Перебрасываем, чтобы приложение не стартовало с битой БД
    }
}

// ================== PIPELINE ==================
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseRequestLocalization();
app.UseStaticFiles();
// ===== SECURITY HEADERS =====
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Append("Permissions-Policy", "camera=(), microphone=(), geolocation=()");
    // X-XSS-Protection устарел, но некоторые старые браузеры его поддерживают:
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    await next();
});
// ============================
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapHub<ChatHub>("/chatHub");

// ================== МАРШРУТЫ ==================
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "profile",
    pattern: "Profile/{action=Index}/{id?}",
    defaults: new { controller = "Profile" });
    
//app.MapControllerRoute(
//    name: "booking",
//    pattern: "Booking/{action}/{id?}",
//    defaults: new { controller = "Booking" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// ================== СОЗДАНИЕ ПАПОК (можно вынести в IStartupFilter) ==================
EnsureUploadDirectories(app.Environment.WebRootPath);

// ================== ИНФОРМАЦИЯ О ЗАПУСКЕ ==================
var loggerStartup = app.Services.GetRequiredService<ILogger<Program>>();
loggerStartup.LogInformation("🚀 Car Rental запущен!");
loggerStartup.LogInformation("🌐 URL: {Urls}", string.Join(", ", app.Urls));
loggerStartup.LogInformation("📧 SMTP настроен: {SmtpEnabled}", !string.IsNullOrEmpty(builder.Configuration["Email:Host"]));
loggerStartup.LogInformation("🤖 Telegram Bot: {TelegramEnabled}", builder.Configuration.GetValue<bool>("TelegramBot:Enabled", true));

//app.Run();
await app.RunAsync();

// ================== ВСПОМОГАТЕЛЬНЫЙ МЕТОД ==================
static void EnsureUploadDirectories(string webRootPath)
{
    var dirs = new[]
    {
        Path.Combine(webRootPath, "uploads", "banners"),
        Path.Combine(webRootPath, "uploads", "pages"),
        Path.Combine(webRootPath, "uploads", "cars"),
        Path.Combine(webRootPath, "uploads", "documents")
    };

    foreach (var dir in dirs)
    {
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
    }
}