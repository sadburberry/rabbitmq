using Microsoft.AspNetCore.Mvc;
using RabbitMQ.Client;
using System.Text;

namespace service_eleve.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EleveController : ControllerBase
    {
        private readonly IConnectionFactory _connectionFactory;

        public EleveController(IConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new { message = "Service Eleve is working!" });
        }

        [HttpPost]
        public IActionResult CreateEleve([FromBody] string eleveName)
        {
            // Publier un message RabbitMQ
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                using var channel = connection.CreateModel();

                channel.QueueDeclare(queue: "eleve.created",
                                    durable: false,
                                    exclusive: false,
                                    autoDelete: false,
                                    arguments: null);

                var body = Encoding.UTF8.GetBytes($"Eleve created: {eleveName}");

                channel.BasicPublish(exchange: "",
                                    routingKey: "eleve.created",
                                    basicProperties: null,
                                    body: body);

                return Ok(new { message = $"Eleve {eleveName} created and message sent to RabbitMQ" });
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