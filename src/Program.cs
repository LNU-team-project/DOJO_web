using DOJO2.Domain.Entities;
using DOJO2.Infrastructure.Data;
using DOJO2.Infrastructure.Middleware;
using DOJO2.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.DataProtection;

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

    // Підключення до PostgreSQL через EF Core
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

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
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true; // ��одовжуємо сесію під час активності
        options.Cookie.IsEssential = true;
    });

    builder.Services.AddTransient<IEmailSender, EmailSender>();
    builder.Services.Configure<AuthMessageSenderOptions>(builder.Configuration.GetSection("SendGrid"));
    
    // Реєстрація сервісів
    builder.Services.AddScoped<IAdminService, AdminService>();
    builder.Services.AddScoped<ITodoService, TodoService>();
    builder.Services.AddScoped<IPlanService, PlanService>();
    builder.Services.AddScoped<IUserService, UserService>();

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

    app.UseRouting();

    app.UseAuthentication();
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
