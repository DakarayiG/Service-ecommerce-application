using Microsoft.EntityFrameworkCore;
using phpMVC.Data; // If you have a Data folder with ApplicationDbContext
using phpMVC.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))
    ));


//SESSION SERVICES HERE
builder.Services.AddDistributedMemoryCache(); // Required for session
builder.Services.AddSignalR();   // <-- ADD THIS

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Session timeout
    options.Cookie.HttpOnly = true; // Security: prevent client-side access
    options.Cookie.IsEssential = true; // Required for GDPR
    options.Cookie.Name = ".WorkConnect.Session"; // Optional: custom cookie name
});

var app = builder.Build();


// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();
app.MapHub<ChatHub>("/chatHub");  // <-- ADD THIS

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();