using DOJO2.Domain.Entities;
using DOJO2.Application.Interfaces;
using DOJO2.Infrastructure.Data;
using DOJO2.Infrastructure.Middleware;
using DOJO2.Infrastructure.Services;
using DOJO2.Application.Common;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity.UI.Services;
using DOJO2.Application.Services;

// Bootstrap logger — щоб логи були навіть під час старту
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

Log.Information("Запуск застосунку...");

try
{
    var builder = WebApplication.CreateBuilder(new WebApplicationOptions
    {
        Args = args,
        // Статичні файли з Presentation/wwwroot
        WebRootPath = "Presentation/wwwroot"
    });

    // Завантажити User Secrets у development середовищі
    if (builder.Environment.IsDevelopment())
    {
        builder.Configuration.AddUserSecrets<Program>();
    }

    // Налаштування Serilog з appsettings.json
    builder.Host.UseSerilog((context, config) =>
        config.ReadFrom.Configuration(context.Configuration));

    // Add services to the container.
    builder.Services.AddControllersWithViews()
        .AddRazorOptions(options =>
        {
            // Шукаємо Views у папці Presentation/Views
            options.ViewLocationFormats.Clear();
            options.ViewLocationFormats.Add("/Presentation/Views/{1}/{0}.cshtml");
            options.ViewLocationFormats.Add("/Presentation/Views/Shared/{0}.cshtml");
        });

    // Прив'язка секцій конфігурації до типізованих options
    builder.Services.Configure<AdminUsersOptions>(builder.Configuration.GetSection(AdminUsersOptions.SectionName));
    builder.Services.Configure<AuthCookieOptions>(builder.Configuration.GetSection(AuthCookieOptions.SectionName));
    builder.Services.Configure<EmailSenderOptions>(builder.Configuration.GetSection(EmailSenderOptions.SectionName));
    // Кеш
    builder.Services.Configure<CacheOptions>(builder.Configuration.GetSection(CacheOptions.SectionName));

    builder.Services.AddMemoryCache();
    
    // Підключення до PostgreSQL через EF Core
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
    builder.Services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

    // Identity: користувачі, ролі, токени
    builder.Services.AddDataProtection(); // Використовуємо персистентні ключі, щоб куки лишались валідними після рестарту без виходу

    builder.Services.AddIdentity<AppUser, IdentityRole<int>>(options =>
        {
            options.User.RequireUniqueEmail = true;
            options.SignIn.RequireConfirmedEmail = false;
            options.Password.RequiredLength = 6;
            options.Password.RequireDigit = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireNonAlphanumeric = false;
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

    builder.Services.ConfigureApplicationCookie(options =>
    {
        const string BlockedNoticeCookieName = "dojo_blocked_notice";

        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true; // Продовжуємо сесію під час активності
        options.Cookie.IsEssential = true;
        options.Events.OnValidatePrincipal = async context =>
        {
            var principal = context.Principal;
            var userId = principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return;
            }

            var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<AppUser>>();
            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync();
                return;
            }

            var isBlocked = user.LockoutEnabled && user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow;
            if (!isBlocked)
            {
                return;
            }

            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync();
            context.HttpContext.Response.Cookies.Append(
                BlockedNoticeCookieName,
                "1",
                new CookieOptions
                {
                    HttpOnly = true,
                    IsEssential = true,
                    Secure = true,
                    SameSite = SameSiteMode.Lax,
                    Expires = DateTimeOffset.UtcNow.AddMinutes(5)
                });
        };
    });

    builder.Services.AddTransient<IEmailSender, EmailSender>();
    builder.Services.Configure<DOJO2.Infrastructure.Services.AuthMessageSenderOptions>(
        builder.Configuration.GetSection("SendGrid"));
    
    // Реєстрація сервісів
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IAdminService, AdminService>();
    builder.Services.AddScoped<ITodoService, TodoService>();
    builder.Services.AddScoped<IHeroService, HeroService>();
    builder.Services.AddScoped<IPlanService, PlanService>();
    builder.Services.AddScoped<IScheduleService, ScheduleService>();
    builder.Services.AddScoped<ICalendarService, CalendarService>();
    builder.Services.AddScoped<IPomodoroService, PomodoroService>();
    builder.Services.AddScoped<IUserService, UserService>();
    builder.Services.AddScoped<IStatisticsService, StatisticsService>();
    builder.Services.AddScoped<INotificationService, NotificationService>();
    builder.Services.AddScoped<ILeaderboardService, LeaderboardService>();

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
        app.UseHttpsRedirection();
    }

    // Логування HTTP запитів через Serilog
    app.UseSerilogRequestLogging();

    // Глобальний обробник винятків
    app.UseGlobalExceptionHandler();

    // Логування часу виконання запитів
    app.UseRequestExecutionTimeLogging();

    app.UseRouting();

    app.UseAuthentication();

    // Логування деталей запиту (включно з user id для авторизованих користувачів)
    app.UseRequestDetailsLogging();

    app.UseAuthorization();

    app.UseStaticFiles();

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Account}/{action=Register}/{id?}");


    await app.RunAsync();
}
catch (Exception ex)when (ex.GetType().Name is not "StopTheHostException" && ex.GetType().Name is not "HostAbortedException")
{
    Log.Fatal(ex, "Застосунок впав під час запуску");
}
finally
{
    await Log.CloseAndFlushAsync();
}
