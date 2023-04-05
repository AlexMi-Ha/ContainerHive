using ContainerHive.Core;
using ContainerHive.Core.Datastore;
using ContainerHive.Filters;
using ContainerHive.Validation;
using ContainerHive.Workers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.Authorization;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllersWithViews();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
//builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();

builder.Services.AddValidationServices();
builder.Services.AddCoreServices(builder.Configuration);

// Background Workers
builder.Services.AddHostedService<LongRunningServiceWorker>();
builder.Services.AddSingleton<BackgroundWorkerQueue>();


// Key -> Obsolete when using CookieAuth
//builder.Services.AddScoped<ApiKeyAuthFilter>();

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(builder.Configuration["CookieAuth:DataProtectionPath"]!))
    .SetApplicationName(builder.Configuration["CookieAuth:ApplicationName"]!);


builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opt => {
        opt.Cookie.Name = builder.Configuration["CookieAuth:Name"]!;
        if(!builder.Environment.IsDevelopment())
            opt.Cookie.Domain = builder.Configuration["CookieAuth:Domain"]!;
        opt.Cookie.SameSite = SameSiteMode.Lax;
        opt.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        opt.Cookie.HttpOnly = true;
        opt.Cookie.IsEssential = true;
});

var app = builder.Build();

app.UseRouting();

using (var scope = app.Services.CreateScope()) {
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.EnsureCreatedAsync();
}

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment()) {
    //app.UseSwagger();
    //app.UseSwaggerUI();
//}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseStaticFiles();

if(app.Environment.IsDevelopment()) {
    app.MapGet("loginlel", async (HttpContext context) => {
        await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(new ClaimsIdentity(new List<Claim> {
                new Claim(ClaimTypes.Name, "Test"),
                new Claim(ClaimTypes.Role, "Administrator"),
            }, CookieAuthenticationDefaults.AuthenticationScheme)));
        return "lel";
    });
}

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();
