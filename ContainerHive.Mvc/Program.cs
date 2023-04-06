using ContainerHive.Core;
using ContainerHive.Core.Datastore;
using ContainerHive.Mvc.Filters;
using ContainerHive.Validation;
using ContainerHive.Mvc.Workers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Pomelo.EntityFrameworkCore.MySql.Query.Internal;

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


builder.Services.AddAuthentication(opt => {
    opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    opt.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(o => {
    o.TokenValidationParameters = new TokenValidationParameters {
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true
    };
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

app.Use(async (context, next) => {
    var token = context.Request.Cookies[app.Configuration["CookieAuth:Name"]!];
    if (token != null) {
        context.Request.Headers.Add("Authorization", "Bearer " + token);
    }
    await next();
});

app.UseAuthentication();
app.UseAuthorization();

app.UseStaticFiles();

if(app.Environment.IsDevelopment()) {
    app.MapGet("loginlel", async (HttpContext context, IConfiguration _configuration) => {
            var claims = new List<Claim> {
            new Claim(ClaimTypes.Name, "Test"),
            new Claim(ClaimTypes.Role, "Administrator"),
        };
        var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            expires: DateTime.Now.AddHours(12),
            claims: claims,
            signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
            );

        context.Response.Cookies.Append(_configuration["CookieAuth:Name"]!, new JwtSecurityTokenHandler().WriteToken(token), new CookieOptions {
            SameSite = SameSiteMode.Lax,
            Secure = true,
            HttpOnly = true,
            IsEssential = true,
            Expires = DateTime.Now.AddDays(1)
        });
        return "lel";
    });
}

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();
