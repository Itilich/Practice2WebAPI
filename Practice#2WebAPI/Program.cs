using Microsoft.EntityFrameworkCore;
using Practice_2WebAPI.Data;
using System;
using System.Reflection;
using Practice_2WebAPI.Security;
using Microsoft.AspNetCore.Identity;

namespace Practice_2WebAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                      ?? "Data Source=shop.db";
            builder.Services.AddDbContext<WebAPIContext>(options =>
                options.UseSqlite(connectionString));

            builder.Services.AddAuthentication("Bearer").AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, TokenAuthenticationHandler>("Bearer", null);
            builder.Services.AddAuthorization();
            // Add services to the container.
            builder.Services.AddControllers().AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
            });
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddEndpointsApiExplorer();

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<WebAPIContext>();
                db.Database.Migrate();
                if (!db.Users.Any())
                {
                    var admin = new AppUser { Login = "admin", Role = "Admin" };
                    admin.PasswordHash = new PasswordHasher<AppUser>().HashPassword(admin, "admin");
                    db.Users.Add(admin);
                    db.SaveChanges();
                }
            }

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
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
