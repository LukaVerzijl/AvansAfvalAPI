using System.Reflection;
using Amazon.Runtime;
using Amazon.S3;
using AvansAfvalAPI.Database;
using AvansAfvalAPI.Interfaces;
using AvansAfvalAPI.Storage;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var sqlConnectionString = builder.Configuration.GetConnectionString("RailwayConnection");
var sqlConnectionStringFound = !string.IsNullOrWhiteSpace(sqlConnectionString);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "AvansAfvalAPI",
        Version = "v1",
    });
});
builder.Services.Configure<RouteOptions>(o => o.LowercaseUrls = true);


builder.Services.AddAuthorization();


builder.Services.AddIdentityApiEndpoints<IdentityUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 10;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireDigit = true;
        options.Password.RequireNonAlphanumeric = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<DatabaseContext>();
// Register IHttpContextAccessor for accessing HTTP context in services (e.g., to get current user info).
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<IAuthenticationService, AspNetIdentityAuthenticationService>();
builder.Services.AddSingleton(S3StorageOptions.FromConfiguration(builder.Configuration));
builder.Services.AddSingleton<IAmazonS3>(serviceProvider =>
{
    var s3Options = serviceProvider.GetRequiredService<S3StorageOptions>();
    s3Options.Validate();

    var credentials = new BasicAWSCredentials(s3Options.AccessKey, s3Options.SecretKey);
    var config = new AmazonS3Config
    {
        ServiceURL = s3Options.ServiceUrl,
        ForcePathStyle = s3Options.ForcePathStyle,
        AuthenticationRegion = s3Options.Region
    };

    return new AmazonS3Client(credentials, config);
});
builder.Services.AddTransient<IObjectStorageService, S3ObjectStorageService>();

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>()
    ?? [];

if (allowedOrigins.Length == 0 && builder.Environment.IsDevelopment())
{
    allowedOrigins =
    [
        "http://localhost:5056",
        "https://localhost:7177"
    ];
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("ImageWebApp", policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
    });
});


builder.Services.AddDbContextPool<DatabaseContext>(options =>
    options.UseNpgsql(sqlConnectionString, npgsqlOptions =>
        npgsqlOptions.ConfigureDataSource(dataSourceBuilder =>
            dataSourceBuilder.EnableDynamicJson()))
);


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "AvansAfvalAPI v1");
        options.RoutePrefix = "swagger"; // Access at /swagger
        options.CacheLifetime = TimeSpan.Zero; // Disable caching for development

        if (!sqlConnectionStringFound)
            options.HeadContent = "<h1 align=\"center\">❌ SqlConnectionString not found ❌</h1>";
    });
}
else
{
    var buildTimeStamp = File.GetCreationTime(Assembly.GetExecutingAssembly().Location);
    string currentHealthMessage = $"The API is up 🚀 | Connection string found: {(sqlConnectionStringFound ? "✅" : "❌")} | Build timestamp: {buildTimeStamp}";

    app.MapGet("/", () => currentHealthMessage);
}

app.UseHttpsRedirection();

app.UseCors("ImageWebApp");
app.UseAuthentication();
app.UseAuthorization();
app.MapGroup("/account").MapIdentityApi<IdentityUser>().WithTags("Account");
app.MapControllers();


app.Run();
