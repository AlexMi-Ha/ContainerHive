using ContainerHive.Core;
using ContainerHive.Core.Datastore;
using ContainerHive.Filters;
using ContainerHive.Validation;
using ContainerHive.Workers;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
        opt.Cookie.Name =   builder.Configuration["CookieAuth:Name"]!;
        opt.Cookie.Domain = builder.Configuration["CookieAuth:Domain"]!;
        opt.Cookie.SameSite = SameSiteMode.Lax;
        opt.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        opt.Cookie.HttpOnly = true;
        opt.Cookie.IsEssential = true;            
    });

var app = builder.Build();

using (var scope = app.Services.CreateScope()) {
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.EnsureCreatedAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) {
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
