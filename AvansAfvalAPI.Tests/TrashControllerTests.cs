using System.Text.Json;
using AvansAfvalAPI.Controllers;
using AvansAfvalAPI.Models;
using Microsoft.AspNetCore.Mvc;

namespace AvansAfvalAPI.Tests;

[TestClass]
public sealed class TrashControllerTests
{
    [TestMethod]
    public async Task GetAsync_FiltersTrashBetweenDates()
    {
        using var database = TestDatabase.Create();
        database.Context.Trash.AddRange(
            CreateTrash(new DateTime(2026, 6, 1)),
            CreateTrash(new DateTime(2026, 6, 10)),
            CreateTrash(new DateTime(2026, 6, 20)));
        await database.Context.SaveChangesAsync();

        var controller = new TrashController(database.Context);

        var result = await controller.GetAsync(
            new DateTime(2026, 6, 5),
            new DateTime(2026, 6, 15));

        var okResult = Assert.IsInstanceOfType<OkObjectResult>(result.Result);
        var trash = Assert.IsInstanceOfType<IEnumerable<Trash>>(okResult.Value).ToList();
        Assert.HasCount(1, trash);
        Assert.AreEqual(new DateTime(2026, 6, 10), trash[0].CaptureDate);
    }

    [TestMethod]
    public async Task GetByIdAsync_ReturnsNotFoundForMissingTrash()
    {
        using var database = TestDatabase.Create();
        var controller = new TrashController(database.Context);

        var result = await controller.GetByIdAsync(123);

        Assert.IsInstanceOfType<NotFoundResult>(result.Result);
    }

    [TestMethod]
    public async Task CreateAsync_PersistsTrashAndReturnsCreatedRoute()
    {
        using var database = TestDatabase.Create();
        var controller = new TrashController(database.Context);
        var request = new CreateTrashRequest
        {
            CaptureDate = new DateTime(2026, 6, 24, 12, 30, 0),
            GarbageType = "Plastic",
            Location = "Breda",
            Confidence = 0.93,
            ExternalParameters = JsonDocument.Parse("""{"source":"unit-test"}""")
        };

        var result = await controller.CreateAsync(request);

        var created = Assert.IsInstanceOfType<CreatedAtRouteResult>(result.Result);
        Assert.AreEqual("GetTrashById", created.RouteName);

        var saved = Assert.IsInstanceOfType<Trash>(created.Value);
        Assert.AreNotEqual(0, saved.Id);
        Assert.AreEqual("Plastic", saved.GarbageType);
        Assert.AreEqual(1, database.Context.Trash.Count());
    }

    private static Trash CreateTrash(DateTime captureDate)
    {
        return new Trash
        {
            CaptureDate = captureDate,
            GarbageType = "Plastic",
            Location = "Breda",
            Confidence = 0.9,
            ExternalParameters = JsonDocument.Parse("""{"weather":"dry"}""")
        };
    }
}
