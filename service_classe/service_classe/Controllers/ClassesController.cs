using Microsoft.AspNetCore.Mvc;
using RabbitMQ.Client;
using System.Text;

namespace service_classe.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ClasseController : ControllerBase
    {
        private readonly IConnectionFactory _connectionFactory;

        public ClasseController(IConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new { message = "Service Classe is working!" });
        }

        [HttpPost]
        public IActionResult CreateClasse([FromBody] string classeName)
        {
            // Publier un message RabbitMQ
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                using var channel = connection.CreateModel();

                channel.QueueDeclare(queue: "classe.created",
                                    durable: false,
                                    exclusive: false,
                                    autoDelete: false,
                                    arguments: null);

                var body = Encoding.UTF8.GetBytes($"Classe created: {classeName}");

                channel.BasicPublish(exchange: "",
                                    routingKey: "classe.created",
                                    basicProperties: null,
                                    body: body);

                return Ok(new { message = $"Classe {classeName} created and message sent to RabbitMQ" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("test-rabbitmq")]
        public IActionResult TestRabbitMQ()
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                using var channel = connection.CreateModel();
                return Ok(new { message = "✅ RabbitMQ connection successful!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"❌ RabbitMQ connection failed: {ex.Message}" });
            }
        }
    }
}