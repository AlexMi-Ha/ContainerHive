var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) {
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.Use(async (context, next) => {
    var token = context.Request.Headers["x-api-public-token"].ToString();
    var config = context.RequestServices.GetRequiredService<IConfiguration>();
    if(!config["ApiPublicToken"]!.Equals(token)) {
        context.Response.StatusCode = 401;
        await context.Response.WriteAsync("You are not authorized to access this resource.");
        return;
    }
    await next(context);
});

app.MapControllers();

app.Run();
