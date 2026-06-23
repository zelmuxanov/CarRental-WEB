using AutoMapper;
using CarRental.BLL.DTOs.Document;
using CarRental.BLL.Interfaces.Services;
using CarRental.Domain.Entities;
using CarRental.Domain.Enums;
using CarRental.Domain.Interfaces.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace CarRental.BLL.Services;

public class DocumentService : IDocumentService
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IMapper _mapper;
    private readonly IFileStorageService _fileStorageService;
    private readonly UserManager<User> _userManager;
    private readonly IEmailService _emailService;
    private readonly ILogger<DocumentService> _logger;

    public DocumentService(
        IDocumentRepository documentRepository,
        IMapper mapper,
        IFileStorageService fileStorageService,
        UserManager<User> userManager,
        IEmailService emailService,
        ILogger<DocumentService> logger)
    {
        _documentRepository = documentRepository;
        _mapper = mapper;
        _fileStorageService = fileStorageService;
        _userManager = userManager;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<DocumentDto> UploadDocumentAsync(DocumentUploadDto uploadDto)
    {
        var user = await _userManager.FindByIdAsync(uploadDto.UserId.ToString());
        if (user == null) throw new InvalidOperationException("Пользователь не найден");

        ValidateFile(uploadDto.File);
        if (uploadDto.File2 != null)
            ValidateFile(uploadDto.File2);
        ValidateDocumentDetails(uploadDto);

        string fileUrl;
        string? fileUrl2 = null;
        try
        {
            fileUrl = await _fileStorageService.SaveSecureFileAsync(uploadDto.File, "documents", uploadDto.UserId);
            if (uploadDto.File2 != null)
            {
                fileUrl2 = await _fileStorageService.SaveSecureFileAsync(uploadDto.File2, "documents", uploadDto.UserId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка сохранения файлов для пользователя {UserId}", uploadDto.UserId);
            throw new InvalidOperationException("Не удалось сохранить файлы. Попробуйте позже.");
        }

        var document = new Document
        {
            Id = Guid.NewGuid(),
            UserId = uploadDto.UserId,
            FileName = uploadDto.File.FileName,
            FilePath = fileUrl,
            FileName2 = uploadDto.File2?.FileName,
            FilePath2 = fileUrl2,
            DocumentType = uploadDto.DocumentType,
            Status = "Pending",
            Description = uploadDto.Description,
            CreatedAt = DateTime.UtcNow,
            DocumentNumber = uploadDto.DocumentNumber,
            IssueDate = uploadDto.IssueDate.HasValue ? DateTime.SpecifyKind(uploadDto.IssueDate.Value, DateTimeKind.Utc) : null,
            ExpiryDate = uploadDto.ExpiryDate.HasValue ? DateTime.SpecifyKind(uploadDto.ExpiryDate.Value, DateTimeKind.Utc) : null,
            IssuedBy = uploadDto.IssuedBy,
            BirthDate = uploadDto.BirthDate,
            PlaceOfBirth = uploadDto.PlaceOfBirth,
            RegistrationAddress = uploadDto.RegistrationAddress
        };

        try
        {
            await _documentRepository.AddAsync(document);
            await _documentRepository.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _fileStorageService.DeleteFile(fileUrl);
            if (fileUrl2 != null) _fileStorageService.DeleteFile(fileUrl2);
            _logger.LogError(ex, "Ошибка сохранения в БД");
            throw new InvalidOperationException("Не удалось сохранить информацию о документе.");
        }

        try
        {
            var docType = uploadDto.DocumentType == DocumentType.Passport ? "Паспорт" :
                        uploadDto.DocumentType == DocumentType.DriverLicense ? "Водительское удостоверение" :
                        uploadDto.DocumentType.ToString();
            await _emailService.SendManagerNotificationAsync(
                "📄 Загружены документы на проверку",
                $"<p><strong>Пользователь:</strong> {user.FirstName} {user.LastName} ({user.Email})</p>" +
                $"<p><strong>Тип документа:</strong> {docType}</p>" +
                $"<p><strong>Номер документа:</strong> {uploadDto.DocumentNumber ?? "не указан"}</p>" +
                $"<p><strong>Дата загрузки:</strong> {DateTime.UtcNow:dd.MM.yyyy HH:mm}</p>");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ошибка отправки уведомления менеджеру о документах");
        }

        if (user.Status == UserStatus.Active)
        {
            user.Status = UserStatus.Pending;
            await _userManager.UpdateAsync(user);
        }

        return _mapper.Map<DocumentDto>(document);
    }

    public async Task<DocumentDto?> GetDocumentByIdAsync(Guid id)
    {
        var document = await _documentRepository.GetByIdWithUserAsync(id);
        if (document == null) return null;

        var dto = _mapper.Map<DocumentDto>(document);
        dto.FilePath = document.FilePath;
        dto.FileUrl = $"/file/document/{id}";
        if (!string.IsNullOrEmpty(document.FilePath2))
            dto.FileUrl2 = $"/file/document/{id}/second";
        return dto;
    }

    public async Task<IEnumerable<DocumentDto>> GetUserDocumentsAsync(Guid userId)
    {
        var documents = await _documentRepository.GetByUserIdWithUserAsync(userId); // новый метод
        var dtos = _mapper.Map<IEnumerable<DocumentDto>>(documents).ToList();
        foreach (var dto in dtos)
        {
            dto.FileUrl = $"/file/document/{dto.Id}";
            if (!string.IsNullOrEmpty(dto.FilePath2))
                dto.FileUrl2 = $"/file/document/{dto.Id}/second";
        }
        return dtos;
    }

    public async Task<bool> UpdateDocumentStatusAsync(Guid documentId, string status, string? adminComment = null)
    {
        var document = await _documentRepository.GetByIdAsync(documentId);
        if (document == null) return false;

        document.Status = status;
        document.VerifiedAt = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(adminComment))
            document.Description = adminComment;

        _documentRepository.Update(document);
        var result = await _documentRepository.SaveChangesAsync();

        if (result)
        {
            _logger.LogInformation("Документ {DocumentId} получил статус {Status}", documentId, status);
            if (status == "Verified")
                await CheckAndActivateUserAsync(document.UserId);
            else if (status == "Rejected")
                await NotifyUserAboutRejectionAsync(document.UserId, adminComment);
        }

        return result;
    }

    public async Task<bool> DeleteDocumentAsync(Guid documentId, Guid userId)
    {
        var document = await _documentRepository.GetByIdAsync(documentId);
        if (document == null) return false;

        // Проверяем что документ принадлежит этому пользователю
        if (document.UserId != userId)
        {
            _logger.LogWarning("Попытка удаления чужого документа {DocumentId} пользователем {UserId}", 
                documentId, userId);
            return false;
        }

        // Нельзя удалять уже проверенные документы
        if (document.Status == "Verified")
        {
            _logger.LogWarning("Попытка удаления верифицированного документа {DocumentId}", documentId);
            return false;
        }

        _fileStorageService.DeleteFile(document.FilePath);
        if (!string.IsNullOrEmpty(document.FilePath2))
            _fileStorageService.DeleteFile(document.FilePath2);

        _documentRepository.Delete(document);
        return await _documentRepository.SaveChangesAsync();
    }
    //Admin
    public async Task<IEnumerable<DocumentDto>> GetPendingDocumentsAsync()
    {
        var pending = await _documentRepository.GetPendingWithUserAsync(); // новый метод
        var dtos = _mapper.Map<IEnumerable<DocumentDto>>(pending).ToList();
        foreach (var dto in dtos)
        {
            dto.FileUrl = $"/file/document/{dto.Id}";
            if (!string.IsNullOrEmpty(dto.FilePath2))
                dto.FileUrl2 = $"/file/document/{dto.Id}/second";
        }
        return dtos;
    }

    public async Task<bool> UpdateDocumentDetailsAsync(Guid documentId, DocumentUpdateDto updateDto)
    {
        var document = await _documentRepository.GetByIdAsync(documentId);
        if (document == null) return false;

        document.DocumentNumber = updateDto.DocumentNumber ?? document.DocumentNumber;
        document.IssueDate = updateDto.IssueDate.HasValue 
            ? DateTime.SpecifyKind(updateDto.IssueDate.Value, DateTimeKind.Utc) 
            : document.IssueDate;
        document.ExpiryDate = updateDto.ExpiryDate.HasValue 
            ? DateTime.SpecifyKind(updateDto.ExpiryDate.Value, DateTimeKind.Utc) 
            : document.ExpiryDate;
        document.IssuedBy = updateDto.IssuedBy ?? document.IssuedBy;
        document.BirthDate = updateDto.BirthDate ?? document.BirthDate;
        document.PlaceOfBirth = updateDto.PlaceOfBirth ?? document.PlaceOfBirth;
        document.RegistrationAddress = updateDto.RegistrationAddress ?? document.RegistrationAddress;
        document.UpdatedAt = DateTime.UtcNow;

        _documentRepository.Update(document);
        return await _documentRepository.SaveChangesAsync();
    }

    private void ValidateFile(IFormFile file)
    {
        if (file.Length == 0)
            throw new InvalidOperationException("Файл пуст.");

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf" };
        var ext = Path.GetExtension(file.FileName).ToLower();
        if (!allowedExtensions.Contains(ext))
            throw new InvalidOperationException($"Недопустимое расширение. Допустимые: {string.Join(", ", allowedExtensions)}");

        const long maxSize = 10 * 1024 * 1024;
        if (file.Length > maxSize)
            throw new InvalidOperationException($"Размер файла превышает {maxSize / 1024 / 1024} МБ.");
    }

    private void ValidateDocumentDetails(DocumentUploadDto dto)
    {
        if (dto.DocumentType == DocumentType.DriverLicense)
        {
            if (dto.ExpiryDate <= DateTime.UtcNow.Date)
                throw new InvalidOperationException("Срок действия ВУ истёк.");
            if (dto.IssueDate > DateTime.UtcNow)
                throw new InvalidOperationException("Дата выдачи не может быть в будущем.");
        }
        if (dto.DocumentType == DocumentType.Passport)
        {
            if (string.IsNullOrWhiteSpace(dto.BirthDate))
                throw new InvalidOperationException("Не указана дата рождения.");
        }
    }

    private async Task CheckAndActivateUserAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return;

        var requiredTypes = new[] { DocumentType.Passport, DocumentType.DriverLicense };
        var userDocs = await _documentRepository.GetByUserIdAsync(userId);
        var requiredDocs = userDocs.Where(d => requiredTypes.Contains(d.DocumentType)).ToList();

        if (requiredDocs.Any() && requiredDocs.All(d => d.Status == "Verified"))
        {
            user.Status = UserStatus.Active;
            await _userManager.UpdateAsync(user);
            _logger.LogInformation("Пользователь {UserId} активирован", userId);
            await _emailService.SendEmailAsync(user.Email!, "Профиль активирован",
                "Поздравляем! Ваши документы проверены, профиль активирован.");
        }
    }

    private async Task NotifyUserAboutRejectionAsync(Guid userId, string? comment)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user != null && !string.IsNullOrEmpty(user.Email))
        {
            await _emailService.SendEmailAsync(user.Email,
                "Документы отклонены",
                $"Ваши документы не прошли проверку. Причина: {comment ?? "не указана"}.");
        }
    }
    public async Task<int> GetDrivingExperienceAsync(Guid userId)
    {
        var documents = await GetUserDocumentsAsync(userId);
        var driverLicense = documents.FirstOrDefault(d => 
            d.DocumentType == DocumentType.DriverLicense && 
            d.Status == "Verified" && 
            d.IssueDate.HasValue);

        if (driverLicense?.IssueDate == null)
            return 0;

        var issueDate = driverLicense.IssueDate.Value;
        var today = DateTime.UtcNow;
        var experience = today.Year - issueDate.Year;
        if (issueDate.Date > today.AddYears(-experience)) experience--;
        return experience;
    }
}