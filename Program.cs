using GameStopInventory.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add MVC
builder.Services.AddControllersWithViews();

// Add Entity Framework with Azure SQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SqlConnection")));

// Add Azure Storage
builder.Services.AddSingleton(x =>
    new Azure.Storage.Blobs.BlobServiceClient(
        builder.Configuration.GetConnectionString("StorageConnection")));

builder.Services.AddSingleton(x =>
    new Azure.Data.Tables.TableServiceClient(
        builder.Configuration.GetConnectionString("StorageConnection")));

builder.Services.AddSession();
builder.Services.AddHttpContextAccessor();
builder.Services.AddAntiforgery();

var app = builder.Build();

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

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    if (!db.Users.Any(u => u.Role == "Admin"))
    {
        db.Users.Add(new GameStopInventory.Models.User
        {
            Username = "admin",
            Password = "admin123",
            Email = "admin@gamestop.com",
            Role = "Admin"
        });
        db.SaveChanges();
    }
}

app.Run();