var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddSingleton<UserManagementAPI.Data.UserRepository>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseMiddleware<UserManagementAPI.Middleware.ErrorHandlingMiddleware>();

app.UseHttpsRedirection();

app.UseMiddleware<UserManagementAPI.Middleware.TokenAuthMiddleware>();
app.UseMiddleware<UserManagementAPI.Middleware.RequestResponseLoggingMiddleware>();

app.MapControllers();

app.Run();
