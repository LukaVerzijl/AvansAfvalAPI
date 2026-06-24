using AvansAfvalAPI.Controllers;
using AvansAfvalAPI.Interfaces;
using AvansAfvalAPI.Prediction;
using AvansAfvalAPI.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AvansAfvalAPI.Tests;

[TestClass]
public sealed class UserUploadedControllerTests
{
    [TestMethod]
    public async Task UploadAsync_RejectsUnsupportedContentType()
    {
        using var database = TestDatabase.Create();
        var storage = new FakeObjectStorageService();
        var queue = new FakeImagePredictionQueue();
        var controller = CreateController(database, storage, queue);
        var file = CreateFormFile("trash.txt", "text/plain", [1, 2, 3]);

        var result = await controller.UploadAsync(file, CancellationToken.None);

        var badRequest = Assert.IsInstanceOfType<BadRequestObjectResult>(result.Result);
        Assert.AreEqual("Alleen JPEG, PNG, WebP en GIF afbeeldingen zijn toegestaan.", badRequest.Value);
        Assert.AreEqual(0, database.Context.UserUploaded.Count());
        Assert.HasCount(0, queue.EnqueuedUploadIds);
    }

    [TestMethod]
    public async Task UploadAsync_PersistsUploadAndEnqueuesPrediction()
    {
        using var database = TestDatabase.Create();
        var storage = new FakeObjectStorageService();
        var queue = new FakeImagePredictionQueue();
        var controller = CreateController(database, storage, queue);
        var file = CreateFormFile("trash.png", "image/png", [1, 2, 3, 4]);

        var result = await controller.UploadAsync(file, CancellationToken.None);

        var created = Assert.IsInstanceOfType<CreatedAtRouteResult>(result.Result);
        Assert.AreEqual("GetUserUploadedById", created.RouteName);

        var response = Assert.IsInstanceOfType<UserUploadedResponse>(created.Value);
        Assert.AreEqual("https://storage.example/trash.png", response.ImageUrl);
        Assert.AreEqual("trash.png", response.ImageName);
        Assert.AreEqual("user-123", response.UserId);
        Assert.AreEqual("https://api.example.test/useruploaded/" + response.UploadId + "/view-url", response.ViewUrlEndpoint);

        Assert.AreEqual(1, database.Context.UserUploaded.Count());
        Assert.HasCount(1, queue.EnqueuedUploadIds);
        Assert.AreEqual(response.UploadId, queue.EnqueuedUploadIds[0]);
        Assert.AreEqual("trash.png", storage.UploadedFileName);
        Assert.AreEqual("image/png", storage.UploadedContentType);
    }

    [TestMethod]
    public async Task DeleteAsync_RemovesUploadAndDeletesStoredObject()
    {
        using var database = TestDatabase.Create();
        var uploadId = Guid.NewGuid();
        database.Context.UserUploaded.Add(new()
        {
            UploadId = uploadId,
            UserId = "user-123",
            ImageUrl = "uploads/trash.png",
            ImageName = "trash.png",
            Confidence = 0
        });
        await database.Context.SaveChangesAsync();

        var storage = new FakeObjectStorageService();
        var controller = CreateController(database, storage, new FakeImagePredictionQueue());

        var result = await controller.DeleteAsync(uploadId, CancellationToken.None);

        Assert.IsInstanceOfType<NoContentResult>(result);
        Assert.AreEqual(0, database.Context.UserUploaded.Count());
        Assert.AreEqual("uploads/trash.png", storage.DeletedObjectKeyOrUrl);
    }

    private static UserUploadedController CreateController(
        TestDatabase database,
        FakeObjectStorageService storage,
        FakeImagePredictionQueue queue)
    {
        var controller = new UserUploadedController(
            database.Context,
            storage,
            new FakeAuthenticationService(),
            queue);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Request =
                {
                    Scheme = "https",
                    Host = new HostString("api.example.test")
                }
            }
        };

        return controller;
    }

    private static IFormFile CreateFormFile(string fileName, string contentType, byte[] bytes)
    {
        return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    private sealed class FakeObjectStorageService : IObjectStorageService
    {
        public string? UploadedFileName { get; private set; }
        public string? UploadedContentType { get; private set; }
        public string? DeletedObjectKeyOrUrl { get; private set; }

        public async Task<StoredObject> UploadAsync(
            Stream content,
            string fileName,
            string contentType,
            CancellationToken cancellationToken)
        {
            UploadedFileName = fileName;
            UploadedContentType = contentType;

            using var buffer = new MemoryStream();
            await content.CopyToAsync(buffer, cancellationToken);

            return new StoredObject($"uploads/{fileName}", $"https://storage.example/{fileName}");
        }

        public string CreateReadUrl(string objectKeyOrUrl, TimeSpan expiresIn)
        {
            return $"https://storage.example/read/{Uri.EscapeDataString(objectKeyOrUrl)}";
        }

        public Task DeleteAsync(string objectKeyOrUrl, CancellationToken cancellationToken)
        {
            DeletedObjectKeyOrUrl = objectKeyOrUrl;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeAuthenticationService : IAuthenticationService
    {
        public string? GetCurrentAuthenticatedUserId() => "user-123";
    }

    private sealed class FakeImagePredictionQueue : IImagePredictionQueue
    {
        public List<Guid> EnqueuedUploadIds { get; } = [];

        public void Enqueue(Guid uploadId)
        {
            EnqueuedUploadIds.Add(uploadId);
        }

        public async IAsyncEnumerable<Guid> ReadAllAsync(
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            yield break;
        }
    }
}
