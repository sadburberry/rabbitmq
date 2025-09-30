using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://*:80");

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
    options.AddPolicy("StudentOnly", policy => policy.RequireRole("student"));
    options.AddPolicy("AdminOrTeacher", policy => policy.RequireRole("admin", "teacher"));
    options.AddPolicy("AllUsers", policy => policy.RequireRole("admin", "teacher", "student"));
});

// RabbitMQ Configuration
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
builder.Services.AddHttpClient();

var app = builder.Build();

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
    Console.WriteLine("✅ Connected to RabbitMQ successfully!");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Failed to connect to RabbitMQ: {ex.Message}");
}

app.Run();