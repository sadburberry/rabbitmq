using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using service_eleve.Data;
using service_eleve.Services;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://*:80");

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
builder.Services.AddDbContext<EleveContext>(options =>
    options.UseInMemoryDatabase("EleveDB"));

// RabbitMQ - Connection Factory
builder.Services.AddSingleton<IConnectionFactory>(sp =>
    new ConnectionFactory
    {
        HostName = "rabbitmq",
        UserName = "guest",
        Password = "guest",
        Port = 5672
    });

// RabbitMQ - Connection
builder.Services.AddSingleton<IConnection>(sp =>
    sp.GetRequiredService<IConnectionFactory>().CreateConnection());

// RabbitMQ Consumer Service
builder.Services.AddHostedService<RabbitMQConsumerService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<EleveContext>();
    context.Database.EnsureCreated();
}

app.Run();