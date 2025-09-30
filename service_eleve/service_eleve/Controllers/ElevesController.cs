using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using RabbitMQ.Client;
using System.Text;

namespace service_eleve.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize] // 🔐 Toutes les routes protégées
    public class EleveController : ControllerBase
    {
        private readonly IConnectionFactory _connectionFactory;
        private readonly HttpClient _httpClient;

        public EleveController(IConnectionFactory connectionFactory, HttpClient httpClient)
        {
            _connectionFactory = connectionFactory;
            _httpClient = httpClient;
        }

        [HttpGet]
        [Authorize(Policy = "AllUsers")] // 🔐 Tous les users authentifiés
        public IActionResult Get()
        {
            var userName = User.Identity?.Name;
            return Ok(new
            {
                message = $"Service Eleve is working! User: {userName}",
                user = User.Claims.Select(c => new { c.Type, c.Value })
            });
        }

        [HttpPost]
        [Authorize(Policy = "AdminOrTeacher")] // 🔐 Seulement admin/teacher
        public async Task<IActionResult> CreateEleve([FromBody] string eleveData)
        {
            try
            {
                var parts = eleveData.Split(':');
                var eleveName = parts[0].Trim();
                var classeName = parts.Length > 1 ? parts[1].Trim() : null;

                // RabbitMQ
                using var connection = _connectionFactory.CreateConnection();
                using var channel = connection.CreateModel();

                channel.QueueDeclare(queue: "eleve.created", durable: false, exclusive: false, autoDelete: false, arguments: null);
                var message = string.IsNullOrEmpty(classeName) ? $"Eleve created: {eleveName}" : $"Eleve created: {eleveName} in class: {classeName}";
                var body = Encoding.UTF8.GetBytes(message);
                channel.BasicPublish(exchange: "", routingKey: "eleve.created", basicProperties: null, body: body);

                // Assigner à la classe si spécifiée
                if (!string.IsNullOrEmpty(classeName))
                {
                    try
                    {
                        var addEleveUrl = $"http://service-classe/classe/add-eleve";
                        var requestData = new { EleveName = eleveName, ClasseName = classeName };
                        var json = System.Text.Json.JsonSerializer.Serialize(requestData);
                        var content = new StringContent(json, Encoding.UTF8, "application/json");
                        await _httpClient.PostAsync(addEleveUrl, content);
                    }
                    catch (Exception httpEx)
                    {
                        Console.WriteLine($"⚠️ Erreur HTTP: {httpEx.Message}");
                    }
                }

                return Ok(new
                {
                    message = $"Eleve {eleveName} created" + (string.IsNullOrEmpty(classeName) ? "" : $" and assigned to {classeName}"),
                    eleve = eleveName,
                    classe = classeName
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("test-rabbitmq")]
        [Authorize(Policy = "AllUsers")]
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

        [HttpGet("public")]
        [AllowAnonymous] // 🔐 Route publique
        public IActionResult PublicInfo()
        {
            return Ok(new { message = "Information publique sur les élèves" });
        }
    }
}