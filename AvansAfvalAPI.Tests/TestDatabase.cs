using AvansAfvalAPI.Database;
using AvansAfvalAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace AvansAfvalAPI.Tests;

internal sealed class TestDatabase : IDisposable
{
    private TestDatabase(TestDatabaseContext context)
    {
        Context = context;
    }

    public TestDatabaseContext Context { get; }

    public static TestDatabase Create()
    {
        var options = new DbContextOptionsBuilder<DatabaseContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new TestDatabaseContext(options);

        return new TestDatabase(context);
    }

    public void Dispose()
    {
        Context.Dispose();
    }
}

internal sealed class TestDatabaseContext(DbContextOptions<DatabaseContext> options) : DatabaseContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Trash>()
            .Ignore(trash => trash.ExternalParameters);

        modelBuilder.Entity<UserUploaded>()
            .Ignore(upload => upload.ExternalParameters);
    }
}
