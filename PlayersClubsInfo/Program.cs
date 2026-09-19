using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using PlayersClubsInfo.Data;
using PlayersClubsInfo.Services;
using Scalar.AspNetCore;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace PlayersClubsInfo
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // =========================================================
            // Docker Secrets
            // =========================================================

            static string ReadDockerSecret(string secretName)
            {
                var path = Path.Combine("/run/secrets", secretName);

                if (!File.Exists(path))
                {
                    throw new InvalidOperationException(
                        $"Docker secret '{secretName}' was not found at '{path}'.");
                }

                var value = File.ReadAllText(path).Trim();

                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new InvalidOperationException(
                        $"Docker secret '{secretName}' is empty.");
                }

                return value;
            }

            var postgresPassword =
                ReadDockerSecret("postgres-password");

            var jwtKey =
                ReadDockerSecret("jwt-key");

            var adminPassword =
                ReadDockerSecret("admin-password");

            // Make Docker Secret values available through the normal
            // ASP.NET Core configuration system.
            builder.Configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Jwt:Key"] = jwtKey,
                    ["AdminUser:Password"] = adminPassword
                });

            // =========================================================
            // Services
            // =========================================================

            builder.Services.AddControllers();

            // Add PlayerService to the DI container
            builder.Services.AddScoped<PlayerService>();

            // Add ClubService to the DI container
            builder.Services.AddScoped<ClubService>();

            // =========================================================
            // PostgreSQL
            // =========================================================

            var connectionString =
                builder.Configuration.GetConnectionString(
                    "DefaultConnection")
                ?? throw new InvalidOperationException(
                    "DefaultConnection is not configured.");

            var connectionStringBuilder =
                new Npgsql.NpgsqlConnectionStringBuilder(connectionString)
                {
                    Password = postgresPassword
                };

            builder.Services.AddDbContext<PlayersClubsInfoContext>(
                options =>
                {
                    options.UseNpgsql(
                        connectionStringBuilder.ConnectionString);
                });

            // =========================================================
            // Token cleanup
            // =========================================================

            builder.Services.AddHostedService<TokenCleanupService>();

            // =========================================================
            // ASP.NET Core Identity
            // =========================================================

            builder.Services.AddIdentity<
                Models.ApplicationUser,
                IdentityRole>()
                .AddEntityFrameworkStores<PlayersClubsInfoContext>()
                .AddDefaultTokenProviders();

            // =========================================================
            // JWT Authentication
            // =========================================================

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme =
                    JwtBearerDefaults.AuthenticationScheme;

                options.DefaultChallengeScheme =
                    JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters =
                    new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,

                        ValidIssuer =
                            builder.Configuration["Jwt:Issuer"],

                        ValidAudience =
                            builder.Configuration["Jwt:Audience"],

                        IssuerSigningKey =
                            new SymmetricSecurityKey(
                                Encoding.UTF8.GetBytes(
                                    builder.Configuration["Jwt:Key"]
                                    ?? throw new InvalidOperationException(
                                        "JWT Key is not configured."))),

                        // Tight clock skew for short-lived tokens.
                        ClockSkew = TimeSpan.FromSeconds(30)
                    };

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var jti = context.Principal?
                            .FindFirst(JwtRegisteredClaimNames.Jti)
                            ?.Value;

                        if (string.IsNullOrEmpty(jti))
                            return;

                        var db = context.HttpContext
                            .RequestServices
                            .GetRequiredService<
                                PlayersClubsInfoContext>();

                        var revoked =
                            await db.RevokedAccessTokens
                                .AnyAsync(r => r.Jti == jti);

                        if (revoked)
                        {
                            context.Fail("Token has been revoked.");
                        }
                    }
                };
            });

            // =========================================================
            // Swagger
            // =========================================================

            builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddSwaggerGen(options =>
            {
                options.AddSecurityDefinition(
                    "Bearer",
                    new OpenApiSecurityScheme
                    {
                        Name = "Authorization",
                        Type = SecuritySchemeType.Http,
                        Scheme = "bearer",
                        BearerFormat = "JWT",
                        In = ParameterLocation.Header,
                        Description = "Enter your JWT token."
                    });

                options.AddSecurityRequirement(document =>
                    new OpenApiSecurityRequirement
                    {
                        [
                            new OpenApiSecuritySchemeReference(
                                "Bearer",
                                document)
                        ] = []
                    });
            });

            var app = builder.Build();

            // =========================================================
            // Database migration
            // =========================================================

            using (var scope = app.Services.CreateScope())
            {
                var db =
                    scope.ServiceProvider
                        .GetRequiredService<PlayersClubsInfoContext>();

                try
                {
                    await db.Database.MigrateAsync();

                    Console.WriteLine(
                        "Database migrations applied successfully.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        $"Database migration failed: {ex.Message}");

                    throw;
                }
            }

            // =========================================================
            // Seed roles and default root user
            // =========================================================

            using (var scopeSeed = app.Services.CreateScope())
            {
                await IdentitySeeder.SeedAsync(
                    scopeSeed.ServiceProvider);
            }

            // =========================================================
            // Test database connection
            // =========================================================

            using (var scope = app.Services.CreateScope())
            {
                var db =
                    scope.ServiceProvider
                        .GetRequiredService<PlayersClubsInfoContext>();

                try
                {
                    if (await db.Database.CanConnectAsync())
                    {
                        Console.WriteLine(
                            "Database connection successful!");
                    }
                    else
                    {
                        Console.WriteLine(
                            "Database connection failed!");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        $"Database connection error: {ex.Message}");
                }
            }

            // =========================================================
            // HTTP request pipeline
            // =========================================================

            if (app.Environment.IsDevelopment())
            {
                app.MapScalarApiReference(options =>
                {
                    options.WithOpenApiRoutePattern(
                        "/swagger/{documentName}/swagger.json");
                });

                app.UseSwagger();
                app.UseSwaggerUI();
            }

            // HTTPS is intentionally disabled for the initial
            // Docker development environment.
            //
            // app.UseHttpsRedirection();

            app.UseAuthentication();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}