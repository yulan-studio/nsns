using Core.Contexts;
using Core.Interfaces;
using Core.Models;
using Core.BackendService;
using Core.Repositories;
using Core.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
//using System.IdentityModel.Tokens.Jwt;
//using Microsoft.AspNetCore.Authentication.JwtBearer;
//using Microsoft.IdentityModel.Tokens;
//using System.Text;

//using Pomelo.EntityFrameworkCore.MySql;
//using Microsoft.AspNetCore.Mvc;


async Task SeedRolesAndAdmin(IServiceProvider serviceProvider)
{
    var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();
    var userManager = serviceProvider.GetRequiredService<UserManager<User>>();

    string[] roleNames = { "Admin", "Staff", "Coach", "Parent", "Child" };

    // Create roles if they don't exist
    foreach (var roleName in roleNames)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole<int> { Name = roleName });
        }
    }

    // Create an admin user if none exists
    var adminEmail = "admin@nsns.ca";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);

    if (adminUser == null)
    {
        var newAdmin = new User
        {
            UserName = "admin",
            Email = adminEmail,
            EmailConfirmed = true,
            Role = "Admin"
        };

        var result = await userManager.CreateAsync(newAdmin, "Admin@123"); // Secure default password

        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(newAdmin, "Admin");
        }
    }
}

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Full MVC with Views (HTML pages using Razor).
// Controllers that return both Views & JSON (e.g., hybrid APIs).
builder.Services.AddControllersWithViews();

// ? Add session services
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Session timeout (adjust as needed)
    options.Cookie.HttpOnly = true; // Security: Prevents client-side script from accessing cookies
    options.Cookie.IsEssential = true; // Required for session to work
});

// ? Add distributed memory cache (required for session to work)
builder.Services.AddDistributedMemoryCache();


// Add JWT configuration to the dependency injection container
//builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));


builder.Services.AddScoped<IUserRepository<User>, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();

// Add UserService
builder.Services.AddScoped<IStaffRepository, StaffRepository>();
builder.Services.AddScoped<IStaffService, StaffService>();



builder.Services.AddScoped<IAdminRepository, AdminRepository>();
builder.Services.AddScoped<IAdminService, AdminService>();


builder.Services.AddScoped<ICoachRepository, CoachRepository>();
builder.Services.AddScoped<ICoachService, CoachService>();

builder.Services.AddScoped<ICoachIncomeRepository, CoachIncomeRepository>();
builder.Services.AddScoped<ICoachIncomeService, CoachIncomeService>();

builder.Services.AddScoped<IChildBalanceRepository, ChildBalanceRepository>();
builder.Services.AddScoped<IChildBalanceService, ChildBalanceService>();


builder.Services.AddScoped<ICourseRepository, CourseRepository>();
builder.Services.AddScoped<ICourseService, CourseService>();

builder.Services.AddScoped<IActivityRepository, ActivityRepository>();
builder.Services.AddScoped<IActivityService, ActivityService>();

builder.Services.AddScoped<IChildService, ChildService>();
builder.Services.AddScoped<IChildRepository, ChildRepository>();

builder.Services.AddScoped<IParentService, ParentService>();
builder.Services.AddScoped<IParentRepository, ParentRepository>();

builder.Services.AddScoped<IParentChildRepository, ParentChildRepository>();
builder.Services.AddScoped<IParentChildService, ParentChildService>();

builder.Services.AddScoped<ICourseEnrollmentRepository, CourseEnrollmentRepository>();
builder.Services.AddScoped<ICourseEnrollmentService, CourseEnrollmentService>();

builder.Services.AddScoped<IActivityEnrollmentService, ActivityEnrollmentService>();
builder.Services.AddScoped<IActivityEnrollmentRepository, ActivityEnrollmentRepository>();

builder.Services.AddScoped<IPaymentPackageRepository, PaymentPackageRepository>();
builder.Services.AddScoped<IPaymentPackageService, PaymentPackageService>();

builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IPaymentService, PaymentService>();

// Add UserService


// Add password hasher
//builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
//builder.Services.AddScoped(typeof(IPasswordHasher<>), typeof(PasswordHasher<>));

builder.Services.AddScoped<ICityRepository, CityRepository>();
builder.Services.AddScoped<ICityService, CityService>();

builder.Services.AddScoped<ISpecialtyRepository, SpecialtyRepository>();
builder.Services.AddScoped<ISpecialtyService, SpecialtyService>();

builder.Services.AddScoped<ICoachSpecialtyRepository, CoachSpecialtyRepository>();
builder.Services.AddScoped<ICoachSpecialtyService, CoachSpecialtyService>();

builder.Services.AddScoped<IUserRegistrationService, UserRegistrationService>();

var connectionString1 = Environment.GetEnvironmentVariable("DefaultConnection");
try
{
    var connectionString = Environment.GetEnvironmentVariable("DefaultConnection")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");
    Console.WriteLine($"Connection string: {connectionString}");
    //builder.Services.AddDbContext<AppDbContext>(options => options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 39))));
    builder.Services.AddDbContext<AppDbContext>(options => options.UseMySql(connectionString,ServerVersion.AutoDetect(connectionString)));

}
catch (Exception ex)
{
    Console.WriteLine("DB setup failed: " + ex.Message);
    throw;
}



builder.Services.AddHostedService<ActivityStatusUpdater>();


// Add Identity
builder.Services.AddIdentity<User, IdentityRole<int>>(options =>
{
    options.Password.RequiredLength = 6;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});






var app = builder.Build();

// ? Enable session middleware
app.UseSession();

// Automatically create roles and an admin user
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await SeedRolesAndAdmin(services);
}



// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

//This will ensure any request to the root URL redirects to /User/AddAdmin.
//app.Use(async (context, next) =>
//{
//    if (context.Request.Path == "/")
//    {
//        context.Response.Redirect("/Account/Login");
//        //context.Response.Redirect("/Staff/List");
//        return;
//    }
//    await next();
//});

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

//app.UseAuthorization();

//app.MapControllerRoute(
//    name: "default",
//    pattern: "{controller=User}/{action=AddAdmin}/{id?}");

//app.MapControllerRoute(
//    name: "default",
//    pattern: "{controller=Account}/{action=Login}/{id?}");

//app.UseEndpoints(endpoints =>
//{
//    // Map controller routes
//    endpoints.MapControllerRoute(
//        name: "default",
//        pattern: "{controller=Account}/{action=Login}/{id?}" // Default route points to User/AddAdmin
//    );
//});

//app.UseEndpoints(endpoints =>
//{
//    // Map controller routes
//    endpoints.MapControllerRoute(
//        name: "admin_add",
//        pattern: "{controller=User}/{action=AddAdmin}/{id?}" // Default route points to User/AddAdmin
//    );
//});

app.UseEndpoints(endpoints =>
{
    // Map controller routes
    endpoints.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}" // Default route points to Home/Index
    );
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}");




app.Run();
