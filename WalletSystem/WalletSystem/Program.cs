using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;
using WalletSystem.Data;
using WalletSystem.Services;

namespace WalletSystem
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container
            builder.Services.AddControllers();
            builder.Services.AddSwaggerGen();
            builder.Services.AddDbContext<WalletContext>(options =>
                options.UseSqlite("Data Source=Wallet_System.db"));

            // Configure Redis connection
            var redisConfiguration = new ConfigurationOptions
            {
                EndPoints = { "localhost:6379" },
                AbortOnConnectFail = false,
                ConnectRetry = 5, // Number of retries on connect
                ConnectTimeout = 5000, // 5 seconds timeout
                SyncTimeout = 5000, // 5 seconds sync timeout
                AsyncTimeout = 5000 // 5 seconds async timeout
            };

            // Try to connect to Redis and log any errors
            try
            {
                var connection = ConnectionMultiplexer.Connect(redisConfiguration);
                builder.Services.AddSingleton<IConnectionMultiplexer>(connection);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Redis connection error: {ex.Message}");
            }

            builder.Services.AddScoped<IWalletService, WalletService>();

            var app = builder.Build();

            // Configure the HTTP request pipeline
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            app.Run();
        }
    }
}
