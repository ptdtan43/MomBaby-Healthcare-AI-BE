using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MomOi.API.BackgroundServices;
using MomOi.API.Data;
using MomOi.API.DTOs;
using MomOi.API.Models.Identity;
using MomOi.API.Repositories;
using MomOi.API.Services.AI;
using MomOi.API.Services.Admin;
using MomOi.API.Services.AIFeatures;
using MomOi.API.Services.Alert;
using MomOi.API.Services.Auth;
using MomOi.API.Services.Baby;
using MomOi.API.Services.BusinessRules;
using MomOi.API.Services.DailyMonitoring;
using MomOi.API.Services.Dashboard;
using MomOi.API.Services.Diet;
using MomOi.API.Services.Expert;
using MomOi.API.Services.Fertility;
using MomOi.API.Services.Integration;
using MomOi.API.Services.Lifestyle;
using MomOi.API.Services.Medication;
using MomOi.API.Services.Mom;
using MomOi.API.Services.Notifications;
using MomOi.API.Services.Nutrition;
using MomOi.API.Services.Postpartum;
using MomOi.API.Services.Pregnancy;
using MomOi.API.Services.Recipe;
using MomOi.API.Services.Report;
using MomOi.API.Services.Symptom;
using MomOi.API.Services.UserProfile;
using System;
using System.IO;
using System.Reflection;
using System.Threading.RateLimiting;

namespace MomOi.API.Extensions
{
    /// <summary>
    /// Extension methods để tách cấu hình khỏi Program.cs.
    /// Mỗi method group một nhóm cấu hình liên quan nhau.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        // ─── 1. DATABASE ──────────────────────────────────────────────────────────

        public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Chưa cấu hình DefaultConnection!");

            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString, npgsqlOptions =>
                    npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorCodesToAdd: null))
                       .UseSnakeCaseNamingConvention());

            return services;
        }

        // ─── 2. IDENTITY ──────────────────────────────────────────────────────────

        public static IServiceCollection AddIdentityConfig(this IServiceCollection services)
        {
            services.AddIdentity<AppUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequiredLength = 6;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

            return services;
        }

        // ─── 3. JWT AUTHENTICATION ────────────────────────────────────────────────

        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["Jwt:Issuer"] ?? "MomOiAPI",
                    ValidAudience = configuration["Jwt:Audience"] ?? "MomOiFrontend",
                    IssuerSigningKey = RsaKeyHelper.GetValidationKey(configuration),
                    ClockSkew = TimeSpan.Zero
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/alerts"))
                        {
                            context.Token = accessToken;
                        }
                        return System.Threading.Tasks.Task.CompletedTask;
                    }
                };
            });

            return services;
        }

        // ─── 4. APPLICATION SERVICES ──────────────────────────────────────────────

        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Repository layer
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            // Giữ lại IGenericRepository để tương thích ngược nếu có nơi nào dùng trực tiếp
            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

            // Domain services
            services.AddScoped<IPregnancyService, PregnancyService>();
            services.AddScoped<IBabyService, BabyService>();
            services.AddScoped<IFertilityService, FertilityService>();
            services.AddScoped<IPostpartumService, PostpartumService>();
            services.AddScoped<IDailyMonitoringService, DailyMonitoringService>();
            services.AddScoped<ILifestyleService, LifestyleService>();
            services.AddScoped<ISymptomService, SymptomService>();
            services.AddScoped<IMedicationService, MedicationService>();
            services.AddScoped<IDietService, DietService>();
            services.AddScoped<IDashboardService, DashboardService>();
            services.AddScoped<IRecipeService, RecipeService>();
            services.AddScoped<IAlertService, AlertService>();
            services.AddScoped<IUserProfileService, UserProfileService>();
            services.AddScoped<IAdminService, AdminService>();
            services.AddScoped<IExpertService, ExpertService>();
            services.AddScoped<IReportService, ReportService>();
            services.AddScoped<IAIFeatureService, AIFeatureService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IMomService, MomService>();
            services.AddScoped<IBusinessRuleEngine, BusinessRuleEngine>();

            // External API clients
            services.AddHttpClient<IUsdaClientService, UsdaClientService>();
            services.AddHttpClient<IGeminiService, GeminiService>();
            services.AddHttpClient<NutritionProxyService>();

            // Singleton services (stateless, shared across requests)
            services.AddSingleton<IEmailService, EmailService>();
            services.AddSingleton<ISmsService, SmsService>();
            services.AddSingleton<IPushNotificationService, PushNotificationService>();

            return services;
        }

        // ─── 5. BACKGROUND WORKERS ────────────────────────────────────────────────

        public static IServiceCollection AddBackgroundWorkers(this IServiceCollection services)
        {
            services.AddHostedService<MedicationReminderWorker>();
            services.AddHostedService<RoutineAlertWorker>();
            services.AddHostedService<DailyMonitoringWorker>();
            services.AddHostedService<LifestyleReminderWorker>();

            return services;
        }

        // ─── 6. RATE LIMITING ─────────────────────────────────────────────────────

        public static IServiceCollection AddRateLimitingPolicy(this IServiceCollection services)
        {
            services.AddRateLimiter(options =>
            {
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? httpContext.Request.Headers.Host.ToString(),
                        factory: partition => new FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = 100,
                            QueueLimit = 0,
                            Window = TimeSpan.FromMinutes(1)
                        }));

                options.OnRejected = async (context, token) =>
                {
                    context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    context.HttpContext.Response.ContentType = "application/json";
                    var rateLimitResponse = ApiResponse<object>.FailureResult("Tần suất yêu cầu quá nhanh. Vui lòng đợi 1 phút.");
                    await context.HttpContext.Response.WriteAsJsonAsync(rateLimitResponse, token);
                };
            });

            return services;
        }

        // ─── 7. CORS ──────────────────────────────────────────────────────────────

        public static IServiceCollection AddCorsPolicy(this IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy", policy =>
                {
                    policy.WithOrigins("http://localhost:3000", "http://localhost:5173", "https://momoi.example.com")
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
            });

            return services;
        }

        // ─── 8. SWAGGER ───────────────────────────────────────────────────────────

        public static IServiceCollection AddSwaggerConfig(this IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "MomOi API",
                    Version = "v1",
                    Description = "Hệ thống Backend API cho ứng dụng chăm sóc mẹ và bé MomOi."
                });

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "Nhập JWT Token: 'Bearer {your_token}'",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });

                try
                {
                    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                    c.IncludeXmlComments(xmlPath);
                }
                catch
                {
                    // XML documentation missing, suppress warning
                }
            });

            return services;
        }
    }
}
