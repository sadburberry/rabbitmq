using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using service_classe.Data;

var builder = WebApplication.CreateBuilder(args); 
builder.WebHost.UseUrls("http://*:80");

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configuration RabbitMQ
var rabbitMQConfig = builder.Configuration.GetSection("RabbitMQ");
var factory = new ConnectionFactory()
{
    HostName = rabbitMQConfig["HostName"],
    Port = int.Parse(rabbitMQConfig["Port"] ?? "5672"),
    UserName = rabbitMQConfig["UserName"],
    Password = rabbitMQConfig["Password"],
    VirtualHost = rabbitMQConfig["VirtualHost"] ?? "/"
};

builder.Services.AddSingleton<IConnectionFactory>(factory);

// Add DbContext (ajustez selon votre configuration)
// builder.Services.AddDbContext<ClasseContext>(options =>
//     options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Test RabbitMQ Connection
try
{
    using var connection = factory.CreateConnection();
    using var channel = connection.CreateModel();
    Console.WriteLine("✅ Connected to RabbitMQ successfully!");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Failed to connect to RabbitMQ: {ex.Message}");
}

app.Run();