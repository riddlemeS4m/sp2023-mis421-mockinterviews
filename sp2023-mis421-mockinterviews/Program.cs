using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using sp2023_mis421_mockinterviews.Data;

namespace sp2023_mis421_mockinterviews
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
                        var connectionString = builder.Configuration.GetConnectionString("UserDataDbContextConnection") ?? throw new InvalidOperationException("Connection string 'UserDataDbContextConnection' not found.");

                                    builder.Services.AddDbContext<UserDataDbContext>(options =>
                options.UseSqlServer(connectionString));

                                                builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
                .AddEntityFrameworkStores<UserDataDbContext>();
            var services = builder.Services;
            var configuration = builder.Configuration;

            // Add services to the container.
            var connectionString = configuration.GetConnectionString("UserDataConnection") ?? throw new InvalidOperationException("Connection string 'UserDataConnection' not found.");
            services.AddDbContext<UserDataDbContext>(options =>
                options.UseSqlServer(connectionString));

            //when updating the userdb, first...
            //add-migration <migrationname> -context userdatadbcontext -outputdir Data\Migrations\UserDb
            //second...
            //update-database -context userdatadbcontext

            var interviewDataConnectionString = configuration.GetConnectionString("MockInterviewDataConnection") ?? throw new InvalidOperationException("Connection string 'MockInterviewDataConnection' not found.");
            services.AddDbContext<MockInterviewDataDbContext>(options =>
                options.UseSqlServer(interviewDataConnectionString));

            //when updating the mockinterviewdb, first...
            //add-migration <migrationname> -context mockinterviewdatadbcontext -outputdir Data\Migrations\MockInterviewDb
            //second...
            //update-database -context mockinterviewdatadbcontext

            services.AddDatabaseDeveloperPageExceptionFilter();

            services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
                .AddEntityFrameworkStores<UserDataDbContext>();
            services.AddControllersWithViews();

            //services.AddAuthentication().AddMicrosoftAccount(microsoftOptions =>
            //{
            //    microsoftOptions.ClientId = configuration["Authentication:Microsoft:ClientId"];
            //    microsoftOptions.ClientSecret = configuration["Authentication:Microsoft:ClientSecret"];
            //});


            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseDeveloperExceptionPage();
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
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

            app.Run();
        }
    }
}