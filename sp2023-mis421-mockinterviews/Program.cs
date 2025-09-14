using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.SignalR;
using Google.Apis.Drive.v3;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using SendGrid;
using VaultSharp;
using VaultSharp.V1.AuthMethods.Token;
using sp2023_mis421_mockinterviews.Models.UserDb;
using sp2023_mis421_mockinterviews.Interfaces.IDbContext;
using sp2023_mis421_mockinterviews.Interfaces.IServices;
using sp2023_mis421_mockinterviews.Services.GoogleDrive;
using sp2023_mis421_mockinterviews.Services.Controllers;
using sp2023_mis421_mockinterviews.Services.SignalR;
using sp2023_mis421_mockinterviews.Services.UserDb;
using sp2023_mis421_mockinterviews.Services.SignupDb;
using sp2023_mis421_mockinterviews.Data.Seeds;
using sp2023_mis421_mockinterviews.Data.Contexts;

namespace sp2023_mis421_mockinterviews
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var services = builder.Services;
            var configuration = builder.Configuration;
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
		        options.KnownNetworks.Clear();
		        options.KnownProxies.Clear();            
            });


            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            // secrets

            if (environment == Environments.Production)
            {
                try
                {
                    var tokenFile = Environment.GetEnvironmentVariable("VAULT_TOKEN_FILE")
                        ?? "/vault/tokens/app-token";

                    string vaultToken;
                    if (File.Exists(tokenFile))
                    {
                        vaultToken = File.ReadAllText(tokenFile).Trim();
                    }
                    else
                    {
                        vaultToken = Environment.GetEnvironmentVariable("VAULT_TOKEN");
                    }

                    var vaultUrl = Environment.GetEnvironmentVariable("VAULT_URL")
                        ?? "http://vault:8200";

                    var authMethod = new TokenAuthMethodInfo(vaultToken);
                    var vaultClientSettings = new VaultClientSettings(vaultUrl, authMethod);
                    var vaultClient = new VaultClient(vaultClientSettings);

                    var secrets = await vaultClient.V1.Secrets.KeyValue.V2
                        .ReadSecretAsync(path: "mockinterviews/production", mountPoint: "secret");

                    var vaultConfig = new Dictionary<string, string>();
                    foreach (var kvp in secrets.Data.Data)
                    {
                        vaultConfig[kvp.Key] = kvp.Value?.ToString();
                    }

                    configuration.AddInMemoryCollection(vaultConfig);
                }
                catch
                {
                    throw;
                }
            }
            else if (environment == Environments.Development)
            {
                configuration.AddUserSecrets<Program>();
            }

            // database

            string usersConnectionString;
            string signupsConnectionString;

            if (environment == Environments.Production)
            {
                usersConnectionString = configuration["ConnectionStrings:Postgres:Production:Users"]
                    ?? throw new InvalidOperationException("User secret 'ConnectionStrings:Postgres:Production:Users' has not been stored yet.");

                signupsConnectionString = configuration["ConnectionStrings:Postgres:Production:Signups"]
                    ?? throw new InvalidOperationException("User secret 'ConnectionStrings:Postgres:Production:Signups' has not been stored yet.");

                services.AddDbContext<IUserDbContext, UsersDbContext>(options =>
                    options.UseNpgsql("Host=127.0.0.1;Database=placeholder;Username=placeholder;Password=placeholder"));

                services.AddDbContext<ISignupDbContext, MockInterviewsDbContext>(options =>
                    options.UseNpgsql("Host=127.0.0.1;Database=placeholder;Username=placeholder;Password=placeholder"));
            }
            else
            {
                usersConnectionString = configuration["ConnectionStrings:SqlServer:Development:Users"]
                    ?? throw new InvalidOperationException("User secret 'ConnectionStrings:Postgres:Development:Users' has not been stored yet.");

                signupsConnectionString = configuration["ConnectionStrings:SqlServer:Development:Signups"]
                    ?? throw new InvalidOperationException("User secret 'ConnectionStrings:Postgres:Development:Signups' has not been stored yet.");

                services.AddDbContext<IUserDbContext, UserDataDbContext>(options =>
                    options.UseSqlServer(usersConnectionString, sqlServerOptions => sqlServerOptions.EnableRetryOnFailure()),
                    ServiceLifetime.Scoped);

                //when updating the userdb, run the following commands in the package manager console... (otherwise, you'll need to run the equivalent dotnet commands in the terminal)
                //1. create the entity framework migration
                //add-migration <migrationname> -context userdatadbcontext -outputdir Data\Migrations\UserDb
                //2. specify database environment which you want to update
                //$env:ASPNETCORE_ENVIRONMENT = "Development" OR $env:ASPNETCORE_ENVIRONMENT = "Production"
                //3. update the database 
                //update-database -context userdatadbcontext -startupproject sp2023-mis421-mockinterviews
                //this is to account for using two different database sets for the two different environments

                services.AddDbContext<ISignupDbContext, MockInterviewDataDbContext>(options =>
                    options.UseSqlServer(signupsConnectionString, sqlServerOptions => sqlServerOptions.EnableRetryOnFailure()),
                    ServiceLifetime.Scoped);

                //when updating the userdb, run the following commands in the package manager console... (otherwise, you'll need to run the equivalent dotnet commands in the terminal)
                //1. create the entity framework migration
                //add-migration <migrationname> -context mockinterviewdatadbcontext -outputdir Data\Migrations\MockInterviewDb
                //2. specify database environment which you want to update
                //$env:ASPNETCORE_ENVIRONMENT = "Development" OR $env:ASPNETCORE_ENVIRONMENT = "Production"
                //3. update the database 
                //update-database -context mockinterviewdatadbcontext -startupproject sp2023-mis421-mockinterviews
                //this is to account for using two different database sets for the two different environments
            }

            // google drive

            string siteContentFolderId;
            string resumesFolderId;
            string pfpsFolderId;

            if (environment == Environments.Production)
            {
                siteContentFolderId = configuration["GoogleDriveFolders:Production:SiteContent"] ?? throw new InvalidOperationException($"User secret 'GoogleDriveFolders:Production:SiteContent' not stored yet.");
                resumesFolderId = configuration["GoogleDriveFolders:Production:Resumes"] ?? throw new InvalidOperationException($"User secret 'GoogleDriveFolders:Production:Resumes' not stored yet.");
                pfpsFolderId = configuration["GoogleDriveFolders:Production:PFPs"] ?? throw new InvalidOperationException($"User secret 'GoogleDriveFolders:Production:PFPs' not stored yet.");
            }
            else
            {
                siteContentFolderId = configuration["GoogleDriveFolders:Development:SiteContent"] ?? throw new InvalidOperationException($"User secret 'GoogleDriveFolders:Development:SiteContent' not stored yet.");
                resumesFolderId = configuration["GoogleDriveFolders:Development:Resumes"] ?? throw new InvalidOperationException($"User secret 'GoogleDriveFolders:Development:Resumes' not stored yet.");
                pfpsFolderId = configuration["GoogleDriveFolders:Development:PFPs"] ?? throw new InvalidOperationException($"User secret 'GoogleDriveFolders:Development:PFPs' not stored yet.");
            }

            string adminPwd = configuration["SeededAdminPwd"] ?? throw new InvalidOperationException("User secret 'SeededAdminPwd' not stored yet.");

            // look into distributed memory cache soon
            services.AddMemoryCache();

            // sendgrid
            var sendGridApiKey = configuration["SendGrid:ApiKey"];
            services.AddSingleton<ISendGridClient>(_ => new SendGridClient(sendGridApiKey));

            // google drive upload
            var googleCredentialSection = configuration.GetSection("GoogleCredential");
            services.AddSingleton(_ => {
                string applicationName = "Mock Interviews App ASP.Net Core MVC";
                string json = GoogleDriveUtility.SerializeCredentials(googleCredentialSection);

                GoogleCredential credential;
                using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)))
                {
                    credential = GoogleCredential.FromStream(stream).CreateScoped(new[]
                    {
                        DriveService.Scope.DriveFile
                    });
                }

                return new DriveService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = applicationName
                });
            });

            services.AddScoped<GoogleDriveSiteContentService>(serviceProvider =>
            {
                var driveService = serviceProvider.GetRequiredService<DriveService>();
                var logger = serviceProvider.GetRequiredService<ILogger<IGoogleDrive>>();
                return new GoogleDriveSiteContentService(siteContentFolderId, driveService, logger);
            });

            services.AddScoped<GoogleDriveResumeService>(serviceProvider =>
            {
                var logger = serviceProvider.GetRequiredService<ILogger<IGoogleDrive>>();
                var driveService = serviceProvider.GetRequiredService<DriveService>();
                return new GoogleDriveResumeService(resumesFolderId, driveService, logger);
            });

            services.AddScoped<GoogleDrivePfpService>(serviceProvider =>
            {
                var logger = serviceProvider.GetRequiredService<ILogger<IGoogleDrive>>();
                var driveService = serviceProvider.GetRequiredService<DriveService>();
                var cacheService = serviceProvider.GetRequiredService<IMemoryCache>();
                return new GoogleDrivePfpService(pfpsFolderId, driveService, cacheService, logger);
            });

            services.AddScoped<InterviewService>(serviceProvider => {
                var dbContext = serviceProvider.GetRequiredService<ISignupDbContext>();
                var logger = serviceProvider.GetRequiredService<ILogger<InterviewService>>();
                return new InterviewService(dbContext, logger);
            });

            services.AddScoped<SettingsService>(serviceProvider => {
                var dbContext = serviceProvider.GetRequiredService<ISignupDbContext>();
                var logger = serviceProvider.GetRequiredService<ILogger<SettingsService>>();
                return new SettingsService(dbContext, logger);
            });

            services.AddScoped<TimeslotService>(serviceProvider => {
                var dbContext = serviceProvider.GetRequiredService<ISignupDbContext>();
                var logger = serviceProvider.GetRequiredService<ILogger<TimeslotService>>();
                return new TimeslotService(dbContext, logger);
            });

            services.AddScoped<EventService>(serviceProvider => {
                var dbContext = serviceProvider.GetRequiredService<ISignupDbContext>();
                var logger = serviceProvider.GetRequiredService<ILogger<EventService>>();
                return new EventService(dbContext, logger);
            });

            services.AddScoped<InterviewerSignupService>(serviceProvider => {
                var dbContext = serviceProvider.GetRequiredService<ISignupDbContext>();
                var logger = serviceProvider.GetRequiredService<ILogger<InterviewerSignupService>>();
                return new InterviewerSignupService(dbContext, logger);
            });

            services.AddScoped<InterviewerLocationService>(serviceProvider => {
                var dbContext = serviceProvider.GetRequiredService<ISignupDbContext>();
                var logger = serviceProvider.GetRequiredService<ILogger<InterviewerLocationService>>();
                return new InterviewerLocationService(dbContext, logger);
            });

            services.AddScoped<InterviewerTimeslotService>(serviceProvider => {
                var dbContext = serviceProvider.GetRequiredService<ISignupDbContext>();
                var logger = serviceProvider.GetRequiredService<ILogger<InterviewerTimeslotService>>();
                return new InterviewerTimeslotService(dbContext, logger);
            });

            services.AddScoped<UserService>(serviceProvider => {
                var dbContext = serviceProvider.GetRequiredService<IUserDbContext>();
                var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                return new UserService(dbContext, userManager);
            });

            services.AddScoped<ISignupDbServiceFactory, SignupDbServiceFactory>();

            services.AddSignalR();

            services.AddHttpClient();

            services.AddTransient<IManageInterviews, ManageInterviewsService>(serviceProvider => {
                var factory = serviceProvider.GetRequiredService<ISignupDbServiceFactory>();
                var users = serviceProvider.GetRequiredService<UserService>();
                var sendGrid = serviceProvider.GetRequiredService<ISendGridClient>();
                var interviews = serviceProvider.GetRequiredService<IHubContext<AssignInterviewsHub>>();
                var interviewers = serviceProvider.GetRequiredService<IHubContext<AvailableInterviewersHub>>();
                var logger = serviceProvider.GetRequiredService<ILogger<ManageInterviewsService>>();
                return new ManageInterviewsService(factory, users, sendGrid, interviews, interviewers, logger);
            });

            services.AddDatabaseDeveloperPageExceptionFilter();

            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<UserDataDbContext>()
                .AddDefaultUI()
                .AddDefaultTokenProviders();

            services.AddScoped<RoleManager<IdentityRole>>();
            services.AddScoped<UserManager<ApplicationUser>>();

            services.AddControllersWithViews();
            services.AddRazorPages();
            services.AddHealthChecks();

            //want to see if I can get this to work someday
            //services.AddAuthentication(options =>
            //{
            //    options.DefaultChallengeScheme = MicrosoftAccountDefaults.AuthenticationScheme;
            //}).AddMicrosoftAccount(microsoftOptions =>
            //{
            //    microsoftOptions.ClientId = configuration["Authentication:Microsoft:ClientId"] ?? throw new InvalidOperationException("Azure AD Client ID not found.");
            //    microsoftOptions.ClientSecret = configuration["Authentication:Microsoft:ClientSecret"] ?? throw new InvalidOperationException("Azure AD Client Secret not found.");
            //});

            services.AddAuthentication()
                .AddMicrosoftAccount(microsoftOptions =>
                {
                    microsoftOptions.ClientId = configuration["Authentication:Microsoft:ClientId"] ?? throw new InvalidOperationException("Azure AD Client ID not found.");
                    microsoftOptions.ClientSecret = configuration["Authentication:Microsoft:ClientSecret"] ?? throw new InvalidOperationException("Azure AD Client Secret not found.");
                });

            var app = builder.Build();

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
                app.UseWebSockets();
            }

            app.UseStaticFiles();
            app.UsePathBase("/wwwroot/");

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapHub<AssignInterviewsHub>("/interviewhub");
            app.MapHub<AvailableInterviewersHub>("/interviewershub");

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "areas",
                    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}"
                );

                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}"
                );
            });

            app.MapRazorPages();
            app.MapHealthChecks("/health");

            //look at this later
            using (var scope = app.Services.CreateScope())
            {
                var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger<Program>();

                logger.LogInformation("Proceeding with custom startup checks...");

                try
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<ISignupDbContext>();
                    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

                    var driveService = scope.ServiceProvider.GetRequiredService<GoogleDriveSiteContentService>();
                    var settingsService = scope.ServiceProvider.GetRequiredService<SettingsService>();
                    var timeslotService = scope.ServiceProvider.GetRequiredService<TimeslotService>();
                    var eventService = scope.ServiceProvider.GetRequiredService<EventService>();

                    logger.LogInformation("Checking that all required roles exist in table 'AspNetRoles'...");
                    await UserDbContextSeed.SeedRolesAsync(roleManager);

                    logger.LogInformation("Checking that backup admin user exists...");
                    await UserDbContextSeed.SeedSuperAdminAsync(userManager, adminPwd);

                    logger.LogInformation("Checking that all events have correct number of timeslots...");
                    await TimeslotSeed.SeedTimeslots(eventService, timeslotService);

                    logger.LogInformation("Checking that all settings exist and have values...");
                    await SettingsSeed.SeedSettings(settingsService);

                    var testGoogleDrive = new GoogleDriveServiceSeed(driveService, dbContext);
                    logger.LogInformation("Looking for parking pass and manual...");
                    await testGoogleDrive.Test();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "One or more startup checks failed.");
                }
            }

            app.MapGet("/Home", () => "");
            app.Run();
        }
    }
}
