using EllipticCurve;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using SendGrid;
using sp2023_mis421_mockinterviews.Data;
using sp2023_mis421_mockinterviews.Data.Access;
using sp2023_mis421_mockinterviews.Models.UserDb;
using System.Configuration;
using Microsoft.AspNetCore.HttpOverrides;
using System.Net;

namespace sp2023_mis421_mockinterviews
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var services = builder.Services;
            var configuration = builder.Configuration;
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                //options.KnownProxies.Add(IPAddress.Parse("127.0.0.1"));
		        //options.KnownProxies.Add(IPAddress.Parse("45.55.99.114"));
		        //options.KnownProxies.Add(IPAddress.Parse("10.108.0.5"));
		        //options.KnownProxies.Add(IPAddress.Parse("10.108.0.6"));

                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
		        options.KnownNetworks.Clear();
		        options.KnownProxies.Clear();            
            });

            configuration.AddUserSecrets<Program>();

            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            string userDataConnectionString;
            string mockInterviewDataConnectionString;
            string adminPwd = "";
            //adminPwd = configuration["SeededAdminPwd"] ?? throw new InvalidOperationException("User secret 'SeededAdminPwd' not stored yet.");

            if (environment == Environments.Development)
            {
                userDataConnectionString = configuration["UsersLocalConnection"] ?? throw new InvalidOperationException("Connection string 'UsersLocalConnection' not found.");
                mockInterviewDataConnectionString = configuration["SignupsLocalConnection"] ?? throw new InvalidOperationException("Connection string 'SignupsLocalConnection' not found.");
            }
            else
            {
                userDataConnectionString = configuration["UserDataConnection"] ?? throw new InvalidOperationException("Connection string 'UserDataConnection' not found.");
                mockInterviewDataConnectionString = configuration["MockInterviewDataConnection"] ?? throw new InvalidOperationException("Connection string 'MockInterviewDataConnection' not found.");
            }

            services.AddDbContext<UserDataDbContext>(options =>
                options.UseSqlServer(userDataConnectionString));

            //when updating the userdb, run the following commands...
            //1. create the entity framework migration
            //add-migration <migrationname> -context userdatadbcontext -outputdir Data\Migrations\UserDb
            //2. specify database environment which you want to update
            //$env:ASPNETCORE_ENVIRONMENT = "Development" OR $env:ASPNETCORE_ENVIRONMENT = "Production"
            //3. update the database 
            //update-database -context userdatadbcontext -startupproject sp2023-mis421-mockinterviews
            //this is to account for using two different database sets for the two different environments

            services.AddDbContext<MockInterviewDataDbContext>(options =>
                options.UseSqlServer(mockInterviewDataConnectionString));

            //when updating the userdb, run the following commands...
            //1. create the entity framework migration
            //add-migration <migrationname> -context mockinterviewdatadbcontext -outputdir Data\Migrations\MockInterviewDb
            //2. specify database environment which you want to update
            //$env:ASPNETCORE_ENVIRONMENT = "Development" OR $env:ASPNETCORE_ENVIRONMENT = "Production"
            //3. update the database 
            //update-database -context mockinterviewdatadbcontext -startupproject sp2023-mis421-mockinterviews
            //this is to account for using two different database sets for the two different environments

            var sendGridApiKey = configuration["SendGrid:ApiKey"];
            services.AddSingleton<ISendGridClient>(_ => new SendGridClient(sendGridApiKey));

            services.AddSignalR();

            services.AddHttpClient();

            services.AddDatabaseDeveloperPageExceptionFilter();

            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<UserDataDbContext>()
                .AddDefaultUI()
                .AddDefaultTokenProviders();

            services.AddScoped<RoleManager<IdentityRole>>();
            services.AddScoped<UserManager<ApplicationUser>>();

            services.AddControllersWithViews();
            services.AddRazorPages();


            services.AddAuthentication().AddMicrosoftAccount(microsoftOptions =>
            {
                microsoftOptions.ClientId = configuration["Authentication:Microsoft:ClientId"] ?? throw new InvalidOperationException("Azure AD Client ID not found.");
                microsoftOptions.ClientSecret = configuration["Authentication:Microsoft:ClientSecret"] ?? throw new InvalidOperationException("Azure AD Client Secret not found.");
            });
	   
            var app = builder.Build();

            //app.UseForwardedHeaders(new ForwardedHeadersOptions
            //{
            //    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            //});

            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseForwardedHeaders();
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseForwardedHeaders();
                app.UseHsts();
            }

           // app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UsePathBase("/wwwroot/");

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapHub<AssignInterviewsHub>("/interviewhub");

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
            app.MapRazorPages();

            //look at this later
            using (var scope = app.Services.CreateScope())
            {
                var newservices = scope.ServiceProvider;
                var loggerFactory = newservices.GetRequiredService<ILoggerFactory>();

                try
                {
                    var timeslotcontext = newservices.GetRequiredService<MockInterviewDataDbContext>();
                    var userManager = newservices.GetRequiredService<UserManager<ApplicationUser>>();
                    var roleManager = newservices.GetRequiredService<RoleManager<IdentityRole>>();

                    UserDbContextSeed.SeedRolesAsync(roleManager).Wait();
                    UserDbContextSeed.SeedSuperAdminAsync(userManager, adminPwd).Wait();
                    MockInterviewDbContextSeed.SeedTimeslots(timeslotcontext).Wait();
                    MockInterviewDbContextSeed.SeedGlobalConfigVars(timeslotcontext).Wait();
                }
                catch (Exception ex)
                {
                    var logger = loggerFactory.CreateLogger<Program>();
                    logger.LogError(ex, "An error occurred seeding the DB.");
                }
            }
            app.MapGet("/Home", () => "");
            app.Run();
        }
    }
}
