using FieldManagementSystem.Data;
using FieldManagementSystem.Interfaces;
using FieldManagementSystem.Middlewares;
using FieldManagementSystem.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=fields.db";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IFieldService, FieldService>();
builder.Services.AddScoped<IDeviceService, DeviceService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseWhen(
    ctx =>
        ctx.Request.Path.StartsWithSegments("/api") &&
        !(ctx.Request.Path.StartsWithSegments("/api/users")
          && HttpMethods.IsPost(ctx.Request.Method)),
    branch => branch.UseMiddleware<UserAuthMiddleware>());

app.MapControllers();
app.Run();

public partial class Program { }