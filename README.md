# О! Прокат — сервис аренды автомобилей

CarRental — веб-приложение для аренды автомобилей, построенное на **ASP.NET Core MVC**.  
Включает публичный сайт с каталогом машин, личный кабинет, бронирование, админ-панель и гибкую систему управления контентом.

## Основные возможности

### Для клиентов
- Просмотр каталога автомобилей с фильтрацией и сортировкой
- Детальная карточка автомобиля с фотографиями, характеристиками и условиями аренды
- Онлайн-бронирование с выбором дат и дополнительных услуг (доставка, безлимитный пробег)
- Расчёт стоимости в реальном времени с учётом тарифов «от 15 дней» и «от 30 дней»
- Информация о лимите пробега, депозите и стоимости перепробега
- Быстрое бронирование без регистрации (гостевое)
- Личный кабинет с историей бронирований и документами
- Онлайн-чат с поддержкой (через SignalR)
- Тёмная и светлая темы
- Адаптивный дизайн для мобильных устройств

### Для администратора
- Управление автомобилями, пользователями, бронированиями
- Календарь бронирований с возможностью создания/удаления броней
- Управление баннерами (изображения и видео) с настройкой масштабирования
- **Динамические страницы (slug)** — создание и редактирование информационных страниц через админ-панель (О компании, Контакты, Политика конфиденциальности и любые другие)
- Гибкие настройки: цены, депозиты, лимиты, соцсети, логотип
- **Email-уведомления** — автоматическая отправка писем менеджеру при новых бронированиях, быстрых заявках, подтверждении/отмене брони
- Экспорт и импорт автомобилей через Excel
- Просмотр логов ошибок прямо в админ-панели
- Разграничение ролей (Admin, User)

---

## Технологии

- **Backend:** ASP.NET Core MVC (.NET 8)
- **ORM:** Entity Framework Core + PostgreSQL
- **Identity:** ASP.NET Core Identity (аутентификация и авторизация)
- **Frontend:** Bootstrap 5, jQuery, Flatpickr, FullCalendar, Font Awesome
- **Дополнительно:** AutoMapper, ClosedXML, SignalR, Spire.Doc
- **База данных:** PostgreSQL
- **Хранение файлов:** локальная файловая система (uploads)

---

## Структура проекта
```text
CarRental/
├── CarRental.sln
├── .gitignore
├── appsettings.Production.json
├── migrate.sql
├── migration.sql
│
├── CarRental.BLL/
│   ├── CarRental.BLL.csproj
│   ├── DTOs/
│   │   ├── Banner/
│   │   │   ├── BannerCreateDto.cs
│   │   │   ├── BannerDto.cs
│   │   │   └── BannerUpdateDto.cs
│   │   ├── Booking/
│   │   │   ├── AdminBookingDto.cs
│   │   │   ├── BookingCalculationDto.cs
│   │   │   ├── BookingCalendarEventDto.cs
│   │   │   ├── BookingDto.cs
│   │   │   └── BookingRequestDto.cs
│   │   ├── Car/
│   │   │   ├── CarCreateDto.cs
│   │   │   ├── CarDto.cs
│   │   │   ├── CarFilterDto.cs
│   │   │   ├── CarImageDto.cs
│   │   │   └── CarUpdateDto.cs
│   │   ├── Chat/
│   │   │   ├── ChatCreateDto.cs
│   │   │   ├── ChatDto.cs
│   │   │   ├── ChatMessageCreateDto.cs
│   │   │   └── ChatMessageDto.cs
│   │   ├── Document/
│   │   │   ├── DocumentDto.cs
│   │   │   ├── DocumentUpdateDto.cs
│   │   │   └── DocumentUploadDto.cs
│   │   ├── Faq/
│   │   │   ├── FaqCreateDto.cs
│   │   │   ├── FaqDto.cs
│   │   │   └── FaqUpdateDto.cs
│   │   ├── Page/
│   │   │   ├── PageCreateDto.cs
│   │   │   ├── PageDto.cs
│   │   │   └── PageUpdateDto.cs
│   │   ├── Settings/
│   │   │   ├── RenterRequirementDto.cs
│   │   │   └── SiteSettingsDto.cs
│   │   └── User/
│   │       ├── LoginDto.cs
│   │       ├── RegisterDto.cs
│   │       ├── UserDocumentsDto.cs
│   │       ├── UserDto.cs
│   │       └── UserProfileDto.cs
│   ├── Interfaces/Services/
│   │   ├── IBannerService.cs
│   │   ├── IBookingService.cs
│   │   ├── ICarService.cs
│   │   ├── IChatService.cs
│   │   ├── IContractService.cs
│   │   ├── IDocumentService.cs
│   │   ├── IEmailService.cs
│   │   ├── IFaqService.cs
│   │   ├── IFileEncryptionService.cs
│   │   ├── IFileStorageService.cs
│   │   ├── ILogViewerService.cs
│   │   ├── IPageService.cs
│   │   ├── ISiteSettingsService.cs
│   │   └── IUserService.cs
│   ├── Mapping/
│   │   └── MappingProfile.cs
│   ├── Options/
│   │   ├── BookingEmailOptions.cs
│   │   ├── EmailOptions.cs
│   │   ├── FileStorageOptions.cs
│   │   ├── ISmtpOptions.cs
│   │   └── ManagerOptions.cs
│   ├── Services/
│   │   ├── AesGcmFileEncryptionService.cs
│   │   ├── BannerService.cs
│   │   ├── BookingService.cs
│   │   ├── CarService.cs
│   │   ├── ChatCleanupService.cs
│   │   ├── ChatService.cs
│   │   ├── DocumentService.cs
│   │   ├── EmailService.cs
│   │   ├── FaqService.cs
│   │   ├── InMemoryLogProvider.cs
│   │   ├── LoggingService.cs
│   │   ├── LogViewerService.cs
│   │   ├── PageService.cs
│   │   └── UserService.cs
│   ├── Utilities/
│   │   ├── PasswordEncryptor.cs
│   │   └── PriceCalculator.cs
│   └── Validators/
│       ├── BookingRequestValidator.cs
│       ├── MinimumAgeAttribute.cs
│       ├── RegisterDtoValidator.cs
│       └── RussianPhoneAttribute.cs
│
├── CarRental.DAL/
│   ├── CarRental.DAL.csproj
│   ├── DesignTimeDbContextFactory.cs
│   ├── Configurations/
│   │   ├── BannerConfiguration.cs
│   │   ├── BookingConfiguration.cs
│   │   ├── CarConfiguration.cs
│   │   ├── CarImageConfiguration.cs
│   │   ├── ChatConfiguration.cs
│   │   ├── ChatMessageConfiguration.cs
│   │   ├── DocumentConfiguration.cs
│   │   ├── PageConfiguration.cs
│   │   └── UserConfiguration.cs
│   ├── Data/
│   │   ├── ApplicationDbContext.cs
│   │   ├── DbInitializer.cs
│   │   └── IdentitySeeder.cs
│   ├── Migrations/
│   │   ├── * (все миграции) *.cs и *.Designer.cs
│   │   └── ApplicationDbContextModelSnapshot.cs
│   └── Repositories/
│       ├── BannerRepository.cs
│       ├── BookingRepository.cs
│       ├── CarImageRepository.cs
│       ├── CarRepository.cs
│       ├── ChatMessageRepository.cs
│       ├── ChatRepository.cs
│       ├── DocumentRepository.cs
│       ├── FaqRepository.cs
│       ├── PageRepository.cs
│       ├── Repository.cs
│       └── UserRepository.cs
│
├── CarRental.Domain/
│   ├── CarRental.Domain.csproj
│   ├── Entities/
│   │   ├── Banner.cs
│   │   ├── BaseEntity.cs
│   │   ├── Booking.cs
│   │   ├── CallbackRequest.cs
│   │   ├── Car.cs
│   │   ├── CarImage.cs
│   │   ├── Chat.cs
│   │   ├── ChatMessage.cs
│   │   ├── Document.cs
│   │   ├── FaqItem.cs
│   │   ├── Page.cs
│   │   └── User.cs
│   ├── Enums/
│   │   ├── BannerMediaType.cs
│   │   ├── BannerType.cs
│   │   ├── BookingStatus.cs
│   │   ├── CarClass.cs
│   │   ├── ChatStatus.cs
│   │   ├── DocumentType.cs
│   │   ├── FuelType.cs
│   │   ├── MessageType.cs
│   │   ├── TransmissionType.cs
│   │   └── UserStatus.cs
│   ├── Exceptions/
│   │   └── BusinessException.cs
│   └── Interfaces/Repositories/
│       ├── IBannerRepository.cs
│       ├── IBookingRepository.cs
│       ├── ICarImageRepository.cs
│       ├── ICarRepository.cs
│       ├── IChatMessageRepository.cs
│       ├── IChatRepository.cs
│       ├── IDocumentRepository.cs
│       ├── IFaqRepository.cs
│       ├── IPageRepository.cs
│       ├── IRepository.cs
│       └── IUserRepository.cs
│
├── CarRental.Tests/
│   ├── CarRental.Tests.csproj
│   └── UnitTest1.cs
│
├── CarRental.Tools/
│   └── PasswordManager.cs
│
└── CarRental.Web/
    ├── CarRental.Web.csproj
    ├── Program.cs
    ├── appsettings.Development.json
    ├── appsettings.json
    ├── Areas/Admin/
    │   ├── Controllers/
    │   │   ├── AdminController.cs
    │   │   ├── BaseAdminController.cs
    │   │   ├── BookingsController.cs
    │   │   ├── CalendarController.cs
    │   │   ├── CarsController.cs
    │   │   ├── ChatController.cs
    │   │   ├── ContentController.cs
    │   │   ├── DocumentsController.cs
    │   │   ├── FaqController.cs
    │   │   ├── ImportExportController.cs
    │   │   ├── LogsController.cs
    │   │   ├── PageController.cs
    │   │   ├── SettingsController.cs
    │   │   └── UsersController.cs
    │   └── Views/
    │       ├── _ViewImports.cshtml
    │       ├── _ViewStart.cshtml
    │       ├── Admin/Dashboard.cshtml
    │       ├── Bookings/Index.cshtml
    │       ├── Calendar/Index.cshtml
    │       ├── Cars/
    │       │   ├── Create.cshtml
    │       │   ├── Details.cshtml
    │       │   ├── Edit.cshtml
    │       │   ├── Index.cshtml
    │       │   └── TestCreate.cshtml
    │       ├── Chat/Index.cshtml
    │       ├── Content/
    │       │   ├── BannerForm.cshtml
    │       │   ├── Banners.cshtml
    │       │   └── Index.cshtml
    │       ├── Documents/
    │       │   ├── Details.cshtml
    │       │   └── Index.cshtml
    │       ├── Faq/
    │       │   ├── Form.cshtml
    │       │   └── Index.cshtml
    │       ├── ImportExport/
    │       │   ├── ImportCars.cshtml
    │       │   └── ImportCarsPreview.cshtml
    │       ├── Logs/Index.cshtml
    │       ├── Page/
    │       │   ├── Form.cshtml
    │       │   └── Index.cshtml
    │       ├── Settings/
    │       │   ├── Contracts.cshtml
    │       │   ├── General.cshtml
    │       │   ├── Index.cshtml
    │       │   └── RenterRequirements.cshtml
    │       ├── Shared/
    │       │   ├── _AdminLayout.cshtml
    │       │   └── _ValidationScriptsPartial.cshtml
    │       └── Users/
    │           ├── Details.cshtml
    │           └── Index.cshtml
    ├── Controllers/
    │   ├── AccountController.cs
    │   ├── Api.cs
    │   ├── BookingController.cs
    │   ├── CarController.cs
    │   ├── ChatController.cs
    │   ├── DocumentsController.cs
    │   ├── EmailConfirmationController.cs
    │   ├── FileController.cs
    │   ├── HomeController.cs
    │   ├── PageController.cs
    │   ├── PasswordRecoveryController.cs
    │   ├── ProfileController.cs
    │   ├── SitemapController.cs
    │   └── Api/
    │       ├── BookingsApiController.cs
    │       └── PublicApiController.cs
    ├── HtmlHelpers/
    │   └── PriceHtmlHelper.cs
    ├── Hubs/
    │   └── ChatHub.cs
    ├── Models/
    │   └── SiteSettings.cs
    ├── Properties/
    │   └── launchSettings.json
    ├── Services/
    │   ├── BookingExpirationService.cs
    │   ├── ContractService.cs
    │   ├── FileStorageService.cs
    │   └── SiteSettingsService.cs
    ├── TagHelpers/
    │   └── PriceTagHelper.cs
    ├── ViewModels/
    │   ├── BookCarViewModel.cs
    │   ├── DocumentsIndexViewModel.cs
    │   ├── DocumentUploadViewModel.cs
    │   ├── Account/
    │   │   ├── ConfirmByCodeViewModel.cs
    │   │   ├── ConfirmEmailViewModel.cs
    │   │   ├── ForgotPasswordViewModel.cs
    │   │   ├── LoginViewModel.cs
    │   │   ├── ProfileViewModel.cs
    │   │   ├── RegisterViewModel.cs
    │   │   ├── ResendConfirmationViewModel.cs
    │   │   └── VerifyResetCodeViewModel.cs
    │   ├── Admin/
    │   │   ├── AdminChatViewModel.cs
    │   │   ├── AdminUsersViewModel.cs
    │   │   ├── BannerCreateEditViewModel.cs
    │   │   ├── BannerIndexViewModel.cs
    │   │   ├── CarCreateViewModel.cs
    │   │   ├── CarEditViewModel.cs
    │   │   ├── ContractTemplateViewModel.cs
    │   │   ├── FaqCreateEditViewModel.cs
    │   │   ├── FaqIndexViewModel.cs
    │   │   ├── GeneralSettingsViewModel.cs
    │   │   ├── PageCreateEditViewModel.cs
    │   │   ├── PageIndexViewModel.cs
    │   │   └── RenterRequirementsViewModel.cs
    │   ├── Chat/
    │   │   ├── ChatMessageViewModel.cs
    │   │   └── ChatViewModel.cs
    │   └── Home/
    │       └── QuickBookingViewModel.cs
    ├── Views/
    │   ├── Account/
    │   │   ├── Login.cshtml
    │   │   └── Register.cshtml
    │   ├── Booking/
    │   │   └── Details.cshtml
    │   ├── Car/
    │   │   ├── Details.cshtml
    │   │   └── Index.cshtml
    │   ├── Chat/
    │   │   └── Index.cshtml
    │   ├── Documents/
    │   │   ├── Details.cshtml
    │   │   ├── Index.cshtml
    │   │   └── Upload.cshtml
    │   ├── EmailConfirmation/
    │   │   ├── ConfirmByCode.cshtml
    │   │   ├── ConfirmEmail.cshtml
    │   │   ├── RegistrationSuccess.cshtml
    │   │   └── ResendConfirmation.cshtml
    │   ├── Home/
    │   │   ├── FAQ.cshtml
    │   │   ├── Index.cshtml
    │   │   ├── Reviews.cshtml
    │   │   └── Terms.cshtml
    │   ├── Page/
    │   │   └── View.cshtml
    │   ├── PasswordRecovery/
    │   │   ├── ForgotPassword.cshtml
    │   │   ├── ForgotPasswordConfirmation.cshtml
    │   │   ├── ResetPassword.cshtml
    │   │   └── ResetPasswordConfirmation.cshtml
    │   ├── Profile/
    │   │   ├── Bookings.cshtml
    │   │   ├── ChangePassword.cshtml
    │   │   ├── Edit.cshtml
    │   │   └── Index.cshtml
    │   ├── Shared/
    │   │   ├── Error.cshtml
    │   │   ├── _BookingModal.cshtml
    │   │   ├── _CarCard.cshtml
    │   │   ├── _Layout.cshtml
    │   │   ├── _LoginPartial.cshtml
    │   │   ├── _ProfileNavigation.cshtml
    │   │   ├── _SimpleLayout.cshtml
    │   │   └── _ValidationScriptsPartial.cshtml
    │   └── Test/
    │       └── Encoding.cshtml
    └── wwwroot/
        ├── site-settings.json
        ├── test.html
        ├── css/
        │   ├── admin.css
        │   └── site.min.css
        ├── documents/
        │   └── oferta.pdf
        ├── js/
        │   ├── admin-chat.js
        │   ├── admin.js
        │   ├── chat-modal.js
        │   ├── chat.js
        │   └── site.js
        ├── lib/
        │   ├── datatables/i18n/ru.json
        │   ├── flatpickr/ ... (flatpickr.min.css, flatpickr.min.js, ru.js, themes/)
        │   ├── fullcalendar/ ... (daygrid.min.js, index.global.js, index.global.min.css, index.global.min.js)
        │   ├── jquery-validate/ ... (jquery.validate.min.js)
        │   ├── jquery-validation-unobtrusive/ ... (jquery.validate.unobtrusive.min.js)
        │   ├── sortable/ ... (Sortable.min.js)
        │   └── tinymce/ ... (все файлы редактора)
        ├── templates/
        │   ├── agreement.docx
        │   └── contract_template.docx
        └── uploads/
            ├── banners/ ... (изображения и видео)
            ├── cars/ ... (изображения автомобилей)
            ├── chat/ ... (аватарки)
            ├── contracts/ ... (договоры .docx, .pdf)
            ├── documents/ ... (документы пользователей)
            ├── logo/ ... (логотип)
            └── pages/ ... (изображения страниц)

```

## Установка и запуск

### Требования
- .NET SDK 8.0
- PostgreSQL (развёрнутый локально или удалённо)

### Шаги

1. Клонируйте репозиторий:
   git clone https://github.com/your-username/CarRental.git
   cd CarRental
Настройте строку подключения в appsettings.Development.json (или appsettings.json):

json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=carrental;Username=postgres;Password=your_password"
}

#Примените миграции и запустите проект:

dotnet ef database update --project CarRental.DAL --startup-project CarRental.Web
dotnet run --project CarRental.Web
Откройте в браузере: https://localhost:5001

#Учётная запись администратора

При первом запуске автоматически создаётся администратор:
Логин: admin@carrental.ru
Пароль: Admin123!
(Пароль можно изменить в админ-панели или через БД)

#Конфигурация
Основные настройки хранятся в файле appsettings.json и через админ-панель (раздел «Настройки»):
Название компании, контакты, ссылки на соцсети (WhatsApp, Telegram, Messenger MAX)
Шаблон договора аренды
Требования к арендатору (возраст, стаж)
Параметры почтового сервера для уведомлений (SMTP)

#Использование
Публичный сайт
/ — главная страница с каруселью баннеров и быстрой заявкой
/Car — каталог автомобилей
/Car/Details/{id} — карточка автомобиля с бронью
/Profile — личный кабинет
/Home/FAQ, /Home/Terms и другие статические страницы
/page/{slug} — динамические страницы (О компании, Контакты, Политика и др.)

#Админ-панель

/Admin/Dashboard — сводка
/Admin/Cars — управление автопарком
/Admin/Bookings — все бронирования (включая быстрые заявки)
/Admin/Calendar — календарь бронирований
/Admin/Content/Banners — баннеры
/Admin/Content/Pages — динамические страницы (slug)
/Admin/Settings/General — основные настройки
/Admin/Settings/RenterRequirements — требования к арендатору
/Admin/Logs — консоль ошибок и предупреждений

#Публикация
Для деплоя используйте встроенную команду:

dotnet publish -c Release -o ./publish
Затем скопируйте содержимое папки publish на сервер и настройте веб-сервер (Nginx, IIS и т.п.).

#Лицензия
Проект разработан как личный коммерческий проект.
При использовании соблюдайте авторские права на сторонние библиотеки.

© 2025–2026 О! Прокат
