using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.HttpOverrides;
using Google.Apis.Drive.v3;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using SendGrid;
using sp2023_mis421_mockinterviews.Options;
using sp2023_mis421_mockinterviews.Models.UserDb;
using sp2023_mis421_mockinterviews.Interfaces.IDbContext;
using sp2023_mis421_mockinterviews.Interfaces.IServices;
using sp2023_mis421_mockinterviews.Services.GoogleDrive;
using sp2023_mis421_mockinterviews.Services.Controllers;
using sp2023_mis421_mockinterviews.Services.SignalR;
using sp2023_mis421_mockinterviews.Services.UserDb;
using sp2023_mis421_mockinterviews.Services.SignupDb;
using sp2023_mis421_mockinterviews.Data.Contexts;

namespace sp2023_mis421_mockinterviews.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddForwardedHeaders(this IServiceCollection services)
    {
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        });

        return services;
    }

    public static IServiceCollection AddSendGrid(this IServiceCollection services, IConfiguration config)
    {
        services.AddOptions<SendGridOptions>()
            .Bind(config.GetSection("SendGrid"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<ISendGridClient>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<SendGridOptions>>().Value;
            return new SendGridClient(options.ApiKey);
        });

        return services;
    }

    public static IServiceCollection AddGoogleDrive(this IServiceCollection services, IConfiguration config, IHostEnvironment env)
    {
        services.AddOptions<GoogleDriveOptions>()
            .Configure(options =>
            {
                var environment = env.EnvironmentName.ToLowerInvariant();
                options.SiteContentFolderId = config[$"GoogleDriveFolders:{environment}:SiteContent"] ?? "";
                options.ResumesFolderId = config[$"GoogleDriveFolders:{environment}:Resumes"] ?? "";
                options.PfpsFolderId = config[$"GoogleDriveFolders:{environment}:PFPs"] ?? "";
                
                if (!env.IsDevelopment())
                {
                    if (string.IsNullOrEmpty(options.SiteContentFolderId))
                        throw new InvalidOperationException($"GoogleDriveFolders:{environment}:SiteContent not found.");
                    if (string.IsNullOrEmpty(options.ResumesFolderId))
                        throw new InvalidOperationException($"GoogleDriveFolders:{environment}:Resumes not found.");
                    if (string.IsNullOrEmpty(options.PfpsFolderId))
                        throw new InvalidOperationException($"GoogleDriveFolders:{environment}:PFPs not found.");
                }
            })
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<GoogleCredentialOptions>()
            .Bind(config.GetSection("GoogleCredential"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<DriveService>(provider =>
        {
            var credentialOptions = provider.GetRequiredService<IOptions<GoogleCredentialOptions>>().Value;
            var driveOptions = provider.GetRequiredService<IOptions<GoogleDriveOptions>>().Value;
            
            string json = GoogleDriveUtility.SerializeCredentials(credentialOptions);

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
                ApplicationName = driveOptions.ApplicationName
            });
        });

        services.AddScoped<GoogleDriveSiteContentService>(serviceProvider =>
        {
            var driveService = serviceProvider.GetRequiredService<DriveService>();
            var logger = serviceProvider.GetRequiredService<ILogger<IGoogleDrive>>();
            var options = serviceProvider.GetRequiredService<IOptions<GoogleDriveOptions>>().Value;
            return new GoogleDriveSiteContentService(options.SiteContentFolderId, driveService, logger);
        });

        services.AddScoped<GoogleDriveResumeService>(serviceProvider =>
        {
            var logger = serviceProvider.GetRequiredService<ILogger<IGoogleDrive>>();
            var driveService = serviceProvider.GetRequiredService<DriveService>();
            var options = serviceProvider.GetRequiredService<IOptions<GoogleDriveOptions>>().Value;
            return new GoogleDriveResumeService(options.ResumesFolderId, driveService, logger);
        });

        services.AddScoped<GoogleDrivePfpService>(serviceProvider =>
        {
            var logger = serviceProvider.GetRequiredService<ILogger<IGoogleDrive>>();
            var driveService = serviceProvider.GetRequiredService<DriveService>();
            var cacheService = serviceProvider.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>();
            var options = serviceProvider.GetRequiredService<IOptions<GoogleDriveOptions>>().Value;
            return new GoogleDrivePfpService(options.PfpsFolderId, driveService, cacheService, logger);
        });

        return services;
    }

    public static IServiceCollection AddDatabases(this IServiceCollection services, IConfiguration config, IHostEnvironment env)
    {
        if (env.IsProduction() || env.IsStaging())
        {
            var usersConnectionString = config[$"ConnectionStrings:Postgres:{env.EnvironmentName.ToLowerInvariant()}:Users"]
                ?? throw new InvalidOperationException($"ConnectionStrings:Postgres:{env.EnvironmentName.ToLowerInvariant()}:Users not found.");
            var signupsConnectionString = config[$"ConnectionStrings:Postgres:{env.EnvironmentName.ToLowerInvariant()}:Signups"]
                ?? throw new InvalidOperationException($"ConnectionStrings:Postgres:{env.EnvironmentName.ToLowerInvariant()}:Signups not found.");

            services.AddDbContextPool<IUserDbContext, UsersDbContext>(options =>
                options.UseNpgsql(usersConnectionString));
            services.AddDbContextPool<ISignupDbContext, MockInterviewsDbContext>(options =>
                options.UseNpgsql(signupsConnectionString));
        }
        else
        {
            var usersConnectionString = config["ConnectionStrings:SqlServer:Development:Users"]
                ?? throw new InvalidOperationException("ConnectionStrings:SqlServer:Development:Users not found.");
            var signupsConnectionString = config["ConnectionStrings:SqlServer:Development:Signups"]
                ?? throw new InvalidOperationException("ConnectionStrings:SqlServer:Development:Signups not found.");

            services.AddDbContext<IUserDbContext, UserDataDbContext>(options =>
                options.UseSqlServer(usersConnectionString, sqlServerOptions => sqlServerOptions.EnableRetryOnFailure()),
                ServiceLifetime.Scoped);
            services.AddDbContext<ISignupDbContext, MockInterviewDataDbContext>(options =>
                options.UseSqlServer(signupsConnectionString, sqlServerOptions => sqlServerOptions.EnableRetryOnFailure()),
                ServiceLifetime.Scoped);
        }

        services.AddDatabaseDeveloperPageExceptionFilter();

        return services;
    }

    public static IServiceCollection AddIdentityAndAuth(this IServiceCollection services, IConfiguration config, IHostEnvironment env)
    {
        if (env.IsProduction() || env.IsStaging())
        {
            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<UsersDbContext>()
                .AddDefaultUI()
                .AddDefaultTokenProviders();
        }
        else
        {
            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<UserDataDbContext>()
                .AddDefaultUI()
                .AddDefaultTokenProviders();
        }

        services.AddScoped<RoleManager<IdentityRole>>();
        services.AddScoped<UserManager<ApplicationUser>>();

        services.AddAuthentication()
            .AddMicrosoftAccount(microsoftOptions =>
            {
                microsoftOptions.ClientId = config["Authentication:Microsoft:ClientId"] ?? throw new InvalidOperationException("Azure AD Client ID not found.");
                microsoftOptions.ClientSecret = config["Authentication:Microsoft:ClientSecret"] ?? throw new InvalidOperationException("Azure AD Client Secret not found.");
            });

        return services;
    }

    public static IServiceCollection AddExternalIntegrations(this IServiceCollection services, IConfiguration config)
    {
        services.AddHttpClient();
        services.AddSignalR();
        services.AddResponseCompression(opts => { opts.EnableForHttps = true; });
        services.AddMemoryCache();
        services.AddHealthChecks();
        services.AddControllersWithViews();
        services.AddRazorPages();

        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<InterviewService>();
        services.AddScoped<SettingsService>();
        services.AddScoped<TimeslotService>();
        services.AddScoped<EventService>();
        services.AddScoped<InterviewerSignupService>();
        services.AddScoped<InterviewerLocationService>();
        services.AddScoped<InterviewerTimeslotService>();
        services.AddScoped<UserService>();

        services.AddScoped<ISignupDbServiceFactory, SignupDbServiceFactory>();

        services.AddTransient<IManageInterviews, ManageInterviewsService>(serviceProvider => {
            var factory = serviceProvider.GetRequiredService<ISignupDbServiceFactory>();
            var users = serviceProvider.GetRequiredService<UserService>();
            var sendGrid = serviceProvider.GetRequiredService<ISendGridClient>();
            var interviews = serviceProvider.GetRequiredService<IHubContext<AssignInterviewsHub>>();
            var interviewers = serviceProvider.GetRequiredService<IHubContext<AvailableInterviewersHub>>();
            var logger = serviceProvider.GetRequiredService<ILogger<ManageInterviewsService>>();
            return new ManageInterviewsService(factory, users, sendGrid, interviews, interviewers, logger);
        });

        return services;
    }

    public static IServiceCollection AddProblemDetails(this IServiceCollection services, IHostEnvironment env)
    {
        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                if (env.IsDevelopment() && context.Exception != null)
                {
                    context.ProblemDetails.Detail = context.Exception.ToString();
                }
            };
        });

        return services;
    }
}