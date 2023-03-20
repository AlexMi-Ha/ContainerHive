using ContainerHive.Core;
using ContainerHive.Core.Datastore;
using ContainerHive.Filters;
using ContainerHive.Validation;
using ContainerHive.Workers;

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

// Key
builder.Services.AddScoped<ApiKeyAuthFilter>();


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
