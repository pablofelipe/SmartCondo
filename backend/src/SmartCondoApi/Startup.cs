using Amazon.ApiGatewayManagementApi;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SmartCondoApi.Controllers;
using SmartCondoApi.GraphQL;
using SmartCondoApi.GraphQL.Mutations;
using SmartCondoApi.GraphQL.Queries;
using SmartCondoApi.Infra;
using SmartCondoApi.Models;
using SmartCondoApi.Services.Auth;
using SmartCondoApi.Services.Condominium;
using SmartCondoApi.Services.Crypto;
using SmartCondoApi.Services.Email;
using SmartCondoApi.Services.ForgotPassword;
using SmartCondoApi.Services.LinkGenerator;
using SmartCondoApi.Services.Message;
using SmartCondoApi.Services.Notification;
using SmartCondoApi.Services.User;
using SmartCondoApi.Services.Vehicle;
using System.Threading.RateLimiting;

namespace SmartCondoApi;

public class Startup
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _env;

    public Startup(IConfiguration configuration, IWebHostEnvironment env)
    {
        _configuration = configuration;
        _env = env;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        var dbHost = Environment.GetEnvironmentVariable("DB_HOST");
        var dbName = Environment.GetEnvironmentVariable("DB_NAME");
        var dbUser = Environment.GetEnvironmentVariable("DB_USER");
        var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");

        if (!string.IsNullOrEmpty(dbHost) && !string.IsNullOrEmpty(dbName) &&
            !string.IsNullOrEmpty(dbUser) && !string.IsNullOrEmpty(dbPassword))
        {
            var connectionString = $"Host={dbHost};Database={dbName};Username={dbUser};Password={dbPassword}";
            _configuration["ConnectionStrings:DefaultConnection"] = connectionString;
            Console.WriteLine($"Connection string overridden from environment (Host={dbHost}, Database={dbName})");
        }
        else
        {
            Console.WriteLine("Using default connection string from appsettings.json");
        }

        services.AddDbContext<SmartCondoContext>(options =>
            options.UseNpgsql(_configuration.GetConnectionString("DefaultConnection"))
            .LogTo(Console.WriteLine, LogLevel.Error));

        services.AddIdentity<User, IdentityRole<long>>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequireUppercase = true;
            options.Password.RequiredLength = 6;
        })
        .AddEntityFrameworkStores<SmartCondoContext>()
        .AddDefaultTokenProviders();

        var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY") ?? _configuration["Jwt:Key"];
        var key = Convert.FromBase64String(jwtKey ?? throw new InvalidOperationException("JWT_KEY is missing"));
        if (key.Length < 32)
            throw new InvalidOperationException("JWT_KEY must be at least 32 characters");

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
                ValidIssuer = _configuration["Jwt:Issuer"],
                ValidAudience = _configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(key)
            };
        });

        services.AddAuthorization();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Login is the highest-value target for brute-forcing credentials, so it gets the
            // tightest window. PublicKeyRateLimit already had an [EnableRateLimiting] attribute on
            // it with nothing behind it - this is what makes that attribute actually do something.
            options.AddPolicy("LoginRateLimit", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(1)
                    }));

            options.AddPolicy("PublicKeyRateLimit", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 20,
                        Window = TimeSpan.FromMinutes(1)
                    }));
        });

        services.AddControllers();
        services.AddApiVersioning();
        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "SmartCondo API", Version = "v1" });
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "Insira o token JWT no formato: Bearer {token}",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
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
        });

        services.AddScoped<IUserProfileControllerDependencies, UserProfileControllerDependencies>();

        services.AddScoped<IAuthDependencies, AuthDependencies>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserProfileServiceDependencies, UserProfileServiceDependencies>();
        services.AddScoped<IUserProfileService, UserProfileService>();
        services.AddScoped<ILinkGeneratorService, LinkGeneratorService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IEmailConfirmationService, EmailConfirmationService>();
        services.AddScoped<IForgotPasswordService, ForgotPasswordService>();
        services.AddScoped<ITowerService, TowerService>();
        services.AddScoped<ICondominiumService, CondominiumService>();
        services.AddScoped<ICryptoService, CryptoService>();
        services.AddScoped<IMessageService, MessageService>();
        services.AddScoped<IVehicleService, VehicleService>();

        services.AddScoped<INotificationService, NotificationService>();

        services.AddHttpContextAccessor();
        services.AddMemoryCache();
        services.AddRouting();

        services.AddAWSLambdaHosting(LambdaEventSource.RestApi);

        services.AddLogging(logging =>
        {
            logging.AddLambdaLogger(new LambdaLoggerOptions
            {
                IncludeCategory = true,
                IncludeLogLevel = true
            });

            logging.SetMinimumLevel(LogLevel.Debug);
        });

        services.AddGraphQLServer()
            .AddQueryType<Query>()
            .AddMutationType<Mutation>()
            .AddTypeExtension<VehicleQueries>()
            .AddTypeExtension<VehicleMutations>()
            .AddVehicleTypes()
            .AddProjections()
            .ModifyRequestOptions(options =>
            {
                options.IncludeExceptionDetails = _env.IsDevelopment();
            });

        services.AddSingleton<IAmazonApiGatewayManagementApi>(provider =>
        {
            var config = new AmazonApiGatewayManagementApiConfig
            {
                ServiceURL = _configuration["WebSocket:ApiUrl"]
            };
            return new AmazonApiGatewayManagementApiClient(config);
        });

        var allowedOrigins = Environment.GetEnvironmentVariable("ALLOWED_ORIGINS")?
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            ?? [];

        if (allowedOrigins.Length == 0)
        {
            allowedOrigins = ["http://localhost:3000"];
            Console.WriteLine("Nenhuma origem configurada em ALLOWED_ORIGINS. Usando fallback para localhost.");
        }

        services.AddCors(options =>
        {
            options.AddPolicy("DynamicCors", policy =>
            {
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .SetPreflightMaxAge(TimeSpan.FromSeconds(86400)); // Cache para OPTIONS
            });
        });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseMiddleware<ErrorHandlingMiddleware>();

        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseRouting();

        app.UseCors("DynamicCors");

        app.UseRateLimiter();

        app.UseAuthentication();
        app.UseAuthorization();

        var provider = new FileExtensionContentTypeProvider();
        provider.Mappings[".json"] = "application/json";

        app.UseStaticFiles(new StaticFileOptions
        {
            ContentTypeProvider = provider
        });

        app.UseStaticFiles(new StaticFileOptions
        {
            OnPrepareResponse = ctx =>
            {
                if (ctx.File.Name == "manifest.json")
                {
                    ctx.Context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                    ctx.Context.Response.Headers.Add("Access-Control-Expose-Headers", "Content-Type");
                }
            }
        });

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            GraphQL.GraphQLEndpoints.Map(endpoints);
        });

    }
}
