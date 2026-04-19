var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Conexión compartida — singleton para toda la app
builder.Services.AddSingleton<RabbitMqConnection>();
// Publisher — scoped para que cada request tenga su canal
builder.Services.AddScoped<IRabbitMqPublisher, RabbitMqPublisher>();
// Consumer — hosted service que vive mientras vive la app
builder.Services.AddHostedService<RabbitMqConsumer>();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();
app.Run();
