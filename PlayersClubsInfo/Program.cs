using Microsoft.EntityFrameworkCore;
using PlayersClubsInfo.Data;

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

            var app = builder.Build();

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
