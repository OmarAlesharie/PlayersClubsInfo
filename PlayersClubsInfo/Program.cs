using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PlayersClubsInfo.Data;
using PlayersClubsInfo.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace PlayersClubsInfo
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

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
                var jwtSettings = builder.Configuration.GetSection("Jwt");

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,

                    ValidIssuer = jwtSettings["Issuer"],
                    ValidAudience = jwtSettings["Audience"],

                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!))
                };
            });

            var app = builder.Build();

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
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
