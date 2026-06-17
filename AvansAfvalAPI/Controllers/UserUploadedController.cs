using Amazon.Runtime;
using AvansAfvalAPI.Database;
using AvansAfvalAPI.Interfaces;
using AvansAfvalAPI.Models;
using AvansAfvalAPI.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AvansAfvalAPI.Controllers;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class UserUploadedController(
    DatabaseContext context,
    IObjectStorageService objectStorageService,
    IAuthenticationService authenticationService) : ControllerBase
{
    private const long MaxFileSize = 10 * 1024 * 1024;
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
        "image/gif"
    };

    [AllowAnonymous]
    [HttpGet(Name = "GetUserUploaded")]
    public async Task<ActionResult<IEnumerable<UserUploadedResponse>>> GetAsync(CancellationToken cancellationToken)
    {
        var uploads = await context.UserUploaded.AsNoTracking()
            .OrderByDescending(userUpload => userUpload.UploadId)
            .ToListAsync(cancellationToken);

        return Ok(uploads.Select(ToResponse));
    }

    [AllowAnonymous]
    [HttpPost(Name = "UploadUserImage")]
    [RequestSizeLimit(MaxFileSize)]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<UserUploadedResponse>> UploadAsync(IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0)
            return BadRequest("Upload een afbeelding die niet leeg is.");

        if (file.Length > MaxFileSize)
            return BadRequest("De afbeelding mag maximaal 10 MB zijn.");

        if (!AllowedContentTypes.Contains(file.ContentType))
            return BadRequest("Alleen JPEG, PNG, WebP en GIF afbeeldingen zijn toegestaan.");

        StoredObject storedObject;
        try
        {
            await using var fileStream = file.OpenReadStream();
            storedObject = await objectStorageService.UploadAsync(
                fileStream,
                file.FileName,
                file.ContentType,
                cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return Problem(ex.Message, statusCode: StatusCodes.Status502BadGateway);
        }
        catch (AmazonServiceException ex)
        {
            return Problem($"S3 upload failed: {ex.Message}", statusCode: StatusCodes.Status502BadGateway);
        }

        var upload = new UserUploaded
        {
            UploadId = Guid.NewGuid(),
            UserId = authenticationService.GetCurrentAuthenticatedUserId(),
            ImageUrl = storedObject.Url,
            ImageName = file.FileName,
            GarbageType = null,
            ExternalParameters = null,
            Confidence = 0
        };

        context.UserUploaded.Add(upload);
        await context.SaveChangesAsync(cancellationToken);

        var response = ToResponse(upload);

        return CreatedAtRoute("GetUserUploadedById", new { id = upload.UploadId }, response);
    }

    [AllowAnonymous]
    [HttpGet("{id:guid}/view-url", Name = "GetUserUploadedViewUrl")]
    public async Task<ActionResult<UserUploadedViewUrlResponse>> CreateViewUrlAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var upload = await context.UserUploaded.AsNoTracking()
            .FirstOrDefaultAsync(userUpload => userUpload.UploadId == id, cancellationToken);

        if (upload is null)
            return NotFound();

        if (string.IsNullOrWhiteSpace(upload.ImageUrl))
            return NotFound();

        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(10);

        try
        {
            var url = objectStorageService.CreateReadUrl(upload.ImageUrl, expiresAt - DateTimeOffset.UtcNow);
            return Ok(new UserUploadedViewUrlResponse(url, expiresAt));
        }
        catch (InvalidOperationException ex)
        {
            return Problem(ex.Message, statusCode: StatusCodes.Status502BadGateway);
        }
        catch (AmazonServiceException ex)
        {
            return Problem($"S3 signed URL failed: {ex.Message}", statusCode: StatusCodes.Status502BadGateway);
        }
    }

    [Authorize]
    [HttpGet("{id:guid}", Name = "GetUserUploadedById")]
    public async Task<ActionResult<UserUploaded>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var upload = await context.UserUploaded.AsNoTracking()
            .FirstOrDefaultAsync(userUpload => userUpload.UploadId == id, cancellationToken);

        if (upload is null)
            return NotFound();

        return Ok(upload);
    }

    private string BuildViewUrlEndpoint(Guid uploadId)
    {
        return $"{Request.Scheme}://{Request.Host}{Request.PathBase}/useruploaded/{uploadId}/view-url";
    }

    private UserUploadedResponse ToResponse(UserUploaded upload)
    {
        return new UserUploadedResponse(
            upload.UploadId,
            upload.ImageUrl,
            upload.ImageName,
            upload.UserId,
            upload.GarbageType,
            upload.Confidence,
            BuildViewUrlEndpoint(upload.UploadId));
    }
}

public sealed record UserUploadedResponse(
    Guid UploadId,
    string? ImageUrl,
    string? ImageName,
    string? UserId,
    string? GarbageType,
    double Confidence,
    string ViewUrlEndpoint);

public sealed record UserUploadedViewUrlResponse(string Url, DateTimeOffset ExpiresAt);
