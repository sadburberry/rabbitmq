using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using service_classe.Data;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://*:80");

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
builder.Services.AddDbContext<ClasseContext>(options =>
    options.UseInMemoryDatabase("ClasseDB"));

// RabbitMQ Connection Factory
builder.Services.AddSingleton<IConnectionFactory>(sp =>
    new ConnectionFactory
    {
        HostName = "rabbitmq",
        UserName = "guest",
        Password = "guest",
        Port = 5672
    });

// RabbitMQ Connection (singleton)
builder.Services.AddSingleton<IConnection>(sp =>
    sp.GetRequiredService<IConnectionFactory>().CreateConnection());

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
    var context = scope.ServiceProvider.GetRequiredService<ClasseContext>();
    context.Database.EnsureCreated();
}

app.Run();