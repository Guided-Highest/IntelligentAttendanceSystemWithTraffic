using IntelligentAttendanceSystem;
using IntelligentAttendanceSystem.Data;
using IntelligentAttendanceSystem.Hub;
using IntelligentAttendanceSystem.Interface;
using IntelligentAttendanceSystem.Middlewares;
using IntelligentAttendanceSystem.Models;
using IntelligentAttendanceSystem.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<IShiftService, ShiftService>();
builder.Services.AddScoped<IDahuaService_One, DahuaService_One>();
builder.Services.AddScoped<IFaceManagementService, FaceManagementService>();
builder.Services.AddScoped<IReportingService, ReportingService>();
builder.Services.AddSingleton<IDahuaDeviceService, DahuaDeviceService>();
builder.Services.AddScoped<IVehicleCountingService, VehicleCountingService>();
// Add to your service registration in Program.cs
builder.Services.AddSingleton<IFaceRecognitionService, FaceRecognitionService>();
// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSession();
builder.Services.AddSignalR(hubOptions =>
{
    hubOptions.EnableDetailedErrors = true;
    hubOptions.KeepAliveInterval = TimeSpan.FromSeconds(10);
    hubOptions.ClientTimeoutInterval = TimeSpan.FromMinutes(2);
    hubOptions.HandshakeTimeout = TimeSpan.FromSeconds(15);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseSession();
var initializationResult = DeviceInitializer.InitializeDeviceAsync(app).GetAwaiter().GetResult();
app.UseMiddleware<DeviceInitializationMiddleware>();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Seed roles and admin user
using (var scope = app.Services.CreateScope())
{
    var serviceProvider = scope.ServiceProvider;
    await SeedData.Initialize(serviceProvider);
}
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllerRoute(
        name: "default",
        pattern: "{controller=Device}/{action=Initialize}/{id?}");
    endpoints.MapHub<FaceRecognitionHub>("/faceRecognitionHub");
});
app.Run();
