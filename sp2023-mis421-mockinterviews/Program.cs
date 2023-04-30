using EllipticCurve;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using OpenAI_API;
using SendGrid;
using sp2023_mis421_mockinterviews.Data;
using sp2023_mis421_mockinterviews.Models.UserDb;
using System.Configuration;

namespace sp2023_mis421_mockinterviews
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var services = builder.Services;
            var configuration = builder.Configuration;

			configuration.AddUserSecrets<Program>();

            var connectionString = configuration["UserDataConnection"] ?? throw new InvalidOperationException("Connection string 'UserDataConnection' not found.");
            services.AddDbContext<UserDataDbContext>(options =>
                options.UseSqlServer(connectionString));

			//when updating the userdb, first...
			//add-migration <migrationname> -context userdatadbcontext -outputdir Data\Migrations\UserDb
			//second...
			//update-database -context userdatadbcontext

			var interviewDataConnectionString = configuration["MockInterviewDataConnection"] ?? throw new InvalidOperationException("Connection string 'UserDataConnection' not found.");
            services.AddDbContext<MockInterviewDataDbContext>(options =>
                options.UseSqlServer(interviewDataConnectionString));

            //when updating the mockinterviewdb, first...
            //add-migration <migrationname> -context mockinterviewdatadbcontext -outputdir Data\Migrations\MockInterviewDb
            //second...
            //update-database -context mockinterviewdatadbcontext

           

			var sendGridApiKey = configuration["SendGrid:ApiKey"];
			services.AddSingleton<ISendGridClient>(_ => new SendGridClient(sendGridApiKey));

			var openAIApiKey = configuration["OpenAI:ApiKey"];
			services.AddSingleton<OpenAIAPI>(_ => new OpenAIAPI(openAIApiKey));

            services.AddDatabaseDeveloperPageExceptionFilter();

            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<UserDataDbContext>()
                .AddDefaultUI()
                .AddDefaultTokenProviders();

            services.AddScoped<RoleManager<IdentityRole>>();
            services.AddScoped<UserManager<ApplicationUser>>();

            services.AddControllersWithViews();
            services.AddRazorPages();


			//services.AddAuthentication().AddMicrosoftAccount(microsoftOptions =>
			//{
			//    microsoftOptions.ClientId = configuration["Authentication:Microsoft:ClientId"];
			//    microsoftOptions.ClientSecret = configuration["Authentication:Microsoft:ClientSecret"];
			//});


			var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseDeveloperExceptionPage();
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
            app.MapRazorPages();

            ////seed three default roles if they don't exist, seed super user if doesn't exist
            //using (var scope = app.Services.CreateScope())
            //{
            //    var newservices = scope.ServiceProvider;
            //    var loggerFactory = newservices.GetRequiredService<ILoggerFactory>();

            //    try
            //    {
            //        var context = newservices.GetRequiredService<UserDataDbContext>();
            //        var userManager = newservices.GetRequiredService<UserManager<ApplicationUser>>();
            //        var roleManager = newservices.GetRequiredService<RoleManager<IdentityRole>>();
            //        var timeslotcontext = newservices.GetRequiredService<MockInterviewDataDbContext>();

            //        UserDbContextSeed.SeedRolesAsync(userManager, roleManager).Wait();
            //        UserDbContextSeed.SeedSuperAdminAsync(userManager, roleManager).Wait();
            //        MockInterviewDbContextSeed.SeedTimeslots(timeslotcontext).Wait();
            //    }
            //    catch (Exception ex)
            //    {
            //        var logger = loggerFactory.CreateLogger<Program>();
            //        logger.LogError(ex, "An error occurred seeding the DB.");
            //    }
            //}

            app.Run();
            //Task.WaitAll(app.RunAsync());
        }
    }
}