using Microsoft.EntityFrameworkCore;
using MomOi.API.Data;
using MomOi.API.Extensions;
using MomOi.API.Hubs;
using MomOi.API.Middleware;

var builder = WebApplication.CreateBuilder(args);

// ── SERVICE REGISTRATION ──────────────────────────────────────────────────────
// Mỗi dòng tương ứng một nhóm cấu hình. Xem chi tiết trong Extensions/ServiceCollectionExtensions.cs
builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddIdentityConfig();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddBackgroundWorkers();
builder.Services.AddRateLimitingPolicy();
builder.Services.AddCorsPolicy();
builder.Services.AddSwaggerConfig();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DictionaryKeyPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

builder.Services.AddSignalR();
builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>();

// ── HTTP PIPELINE ─────────────────────────────────────────────────────────────
var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment() || builder.Configuration["EnableSwagger"] == "true")
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "MomOi API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseCors("CorsPolicy");
app.UseRateLimiter();
app.UseRouting();
app.UseAuthentication();
app.UseMiddleware<SubscriptionTierMiddleware>();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapHub<AlertHub>("/hubs/alerts");
app.MapControllers();

// ── STARTUP TASKS ─────────────────────────────────────────────────────────────
if (builder.Configuration["RunMigrationsOnStartup"] == "true")
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        dbContext.Database.Migrate();
        Console.WriteLine("Database migrations applied successfully.");

        await MomOi.API.Data.DbInitializer.InitializeAsync(scope.ServiceProvider);
        Console.WriteLine("Default roles and users seeded successfully.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error applying migrations on startup: {ex.Message}");
    }
}

app.Run();
