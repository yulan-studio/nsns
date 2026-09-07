using Microsoft.AspNetCore.Authentication.Cookies;
using nsns_waiver.Data;
using nsns_waiver.Options;
using nsns_waiver.Repositories;
using nsns_waiver.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure concise application logging for both deployed and local environments.
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Register Razor Pages and cookie authentication for the protected admin area.
builder.Services.AddRazorPages();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Admin/Login";
        options.AccessDeniedPath = "/Admin/Login";
        options.Cookie.Name = "NorthStar.Waiver.Admin";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });
builder.Services.AddAuthorization();

// Bind strongly typed settings from appsettings and environment variables.
builder.Services.Configure<WaiverOptions>(
    builder.Configuration.GetSection(WaiverOptions.SectionName));
builder.Services.Configure<EmailOptions>(
    builder.Configuration.GetSection(EmailOptions.SectionName));
builder.Services.Configure<AdminOptions>(
    builder.Configuration.GetSection(AdminOptions.SectionName));

// Register the clock, data access, business services, email transport, and worker.
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IDbConnectionFactory, MySqlConnectionFactory>();
builder.Services.AddScoped<IWaiverSubmissionRepository, WaiverSubmissionRepository>();
builder.Services.AddScoped<IAdminSubmissionRepository, AdminSubmissionRepository>();
builder.Services.AddScoped<IEmailOutboxRepository, EmailOutboxRepository>();
builder.Services.AddScoped<IWaiverSubmissionService, WaiverSubmissionService>();
builder.Services.AddScoped<IWaiverAgreementProvider, FileWaiverAgreementProvider>();
builder.Services.AddSingleton<IAdminCredentialValidator, AdminCredentialValidator>();
builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<EmailOutboxProcessor>();
builder.Services.AddHostedService<EmailOutboxWorker>();

var app = builder.Build();

// Production requests use a friendly error page, HSTS, and HTTPS redirection.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseRouting();

// Authentication must run before authorization evaluates protected pages.
app.UseAuthentication();
app.UseAuthorization();

// Serve static assets and map every Razor Page endpoint.
app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();






