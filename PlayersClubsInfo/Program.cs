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

            // Add services to the container.
            builder.Services.AddControllers();

            // Add PlayerService to the DI container
            builder.Services.AddScoped<PlayerService>();

            // Add ClubService to the DI container
            builder.Services.AddScoped<ClubService>();


            // Add DbContext with PostgreSQL connection
            builder.Services.AddDbContext<PlayersClubsInfoContext>(options =>
            {
                options.UseNpgsql(
                    builder.Configuration.GetConnectionString("DefaultConnection"));
            });

            // Add Identity services
            builder.Services.AddIdentity<Models.ApplicationUser, Microsoft.AspNetCore.Identity.IdentityRole>()
                .AddEntityFrameworkStores<PlayersClubsInfoContext>()
                .AddDefaultTokenProviders();
            
            // Add JWT authentication
            builder.Services.AddAuthentication(options =>
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
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]
                            ?? throw new InvalidOperationException("JWT Key is not configured."))),
                    // tighten clock skew when using short-lived tokens
                    ClockSkew = TimeSpan.FromSeconds(30)
                };

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var jti = context.Principal?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
                        if (string.IsNullOrEmpty(jti))
                            return;

                        var db = context.HttpContext.RequestServices.GetRequiredService<PlayersClubsInfoContext>();

                        // ensure RevokedAccessTokens DbSet/model + migration exist
                        var revoked = await db.RevokedAccessTokens.AnyAsync(r => r.Jti == jti);
                        if (revoked)
                        {
                            context.Fail("Token has been revoked.");
                        }
                    }
                };
            });

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
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
                        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
                    });
            });

            var app = builder.Build();

            // Seed roles and default root user
            using (var scopeSeed = app.Services.CreateScope())
            {
                await IdentitySeeder.SeedAsync(scopeSeed.ServiceProvider);
            }

            #region Test database connection
            // Test database connection
            using var scope = app.Services.CreateScope();

            var db = scope.ServiceProvider.GetRequiredService<PlayersClubsInfoContext>();

            try
            {
                if (db.Database.CanConnect())
                {
                    Console.WriteLine("Database connection successful!");
                }
                else
                {
                    Console.WriteLine("Database connection failed!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database connection error: {ex.Message}");
            }
            #endregion

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapScalarApiReference(options =>
                {
                    options.WithOpenApiRoutePattern("/swagger/{documentName}/swagger.json");
                });
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthentication();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
