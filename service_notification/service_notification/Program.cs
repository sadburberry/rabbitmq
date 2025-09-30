using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 🔐 Keycloak Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Keycloak:Authority"];
        options.Audience = "account";
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Keycloak:Authority"],
            ValidateLifetime = true
        };
    });

// 🔐 RBAC Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("admin"));
    options.AddPolicy("TeacherOnly", policy => policy.RequireRole("teacher"));
    options.AddPolicy("AdminOrTeacher", policy => policy.RequireRole("admin", "teacher"));
});

// RabbitMQ Configuration
var rabbitMQConfig = builder.Configuration.GetSection("RabbitMQ");
var factory = new ConnectionFactory()
{
    HostName = rabbitMQConfig["HostName"] ?? "rabbitmq",
    Port = int.Parse(rabbitMQConfig["Port"] ?? "5672"),
    UserName = rabbitMQConfig["UserName"] ?? "guest",
    Password = rabbitMQConfig["Password"] ?? "guest",
    VirtualHost = rabbitMQConfig["VirtualHost"] ?? "/"
};

builder.Services.AddSingleton<RabbitMQ.Client.IConnectionFactory>(factory);

var app = builder.Build();
builder.WebHost.UseUrls("http://*:80");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication(); // 🔐 ADD THIS
app.UseAuthorization();

app.MapControllers();

// Test RabbitMQ Connection
try
{
    using var connection = factory.CreateConnection();
    using var channel = connection.CreateModel();
    Console.WriteLine("✅ Notification Service connected to RabbitMQ successfully!");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Failed to connect to RabbitMQ: {ex.Message}");
}

app.Run();