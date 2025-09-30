using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using RabbitMQ.Client;
using System.Text;
using service_classe.Models;

namespace service_classe.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize] // 🔐 Toutes les routes protégées
    public class ClasseController : ControllerBase
    {
        private readonly IConnectionFactory _connectionFactory;
        private static Dictionary<string, Classe> _classes = new Dictionary<string, Classe>();

        public ClasseController(IConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        [HttpGet]
        [Authorize(Policy = "AllUsers")] // 🔐 Tous les users authentifiés
        public IActionResult Get()
        {
            var userName = User.Identity?.Name;
            return Ok(new
            {
                message = $"Service Classe is working! User: {userName}",
                user = User.Claims.Select(c => new { c.Type, c.Value })
            });
        }

        [HttpGet("{classeName}")]
        [Authorize(Policy = "AllUsers")]
        public IActionResult GetClasse(string classeName)
        {
            if (_classes.ContainsKey(classeName))
            {
                return Ok(_classes[classeName]);
            }
            return NotFound(new { message = $"Classe '{classeName}' non trouvée" });
        }

        [HttpGet("all")]
        [Authorize(Policy = "AdminOrTeacher")] // 🔐 Seulement admin/teacher
        public IActionResult GetAllClasses()
        {
            return Ok(_classes.Values);
        }

        [HttpPost]
        [Authorize(Policy = "AdminOrTeacher")] // 🔐 Seulement admin/teacher
        public IActionResult CreateClasse([FromBody] string classeName)
        {
            if (!_classes.ContainsKey(classeName))
            {
                _classes[classeName] = new Classe
                {
                    Id = _classes.Count + 1,
                    Nom = classeName,
                    Niveau = "Secondaire",
                    Capacite = 30,
                    DateCreation = DateTime.UtcNow
                };
            }

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                using var channel = connection.CreateModel();

                channel.QueueDeclare(queue: "classe.created", durable: false, exclusive: false, autoDelete: false, arguments: null);
                var body = Encoding.UTF8.GetBytes($"Classe created: {classeName}");
                channel.BasicPublish(exchange: "", routingKey: "classe.created", basicProperties: null, body: body);

                return Ok(new
                {
                    message = $"Classe {classeName} created and message sent to RabbitMQ",
                    classe = _classes[classeName]
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("add-eleve")]
        [Authorize(Policy = "AdminOrTeacher")] // 🔐 Seulement admin/teacher
        public IActionResult AddEleveToClasse([FromBody] AddEleveRequest request)
        {
            if (string.IsNullOrEmpty(request.ClasseName) || string.IsNullOrEmpty(request.EleveName))
            {
                return BadRequest(new { message = "ClasseName et EleveName sont requis" });
            }

            if (!_classes.ContainsKey(request.ClasseName))
            {
                return NotFound(new { message = $"Classe '{request.ClasseName}' non trouvée" });
            }

            var classe = _classes[request.ClasseName];
            if (!classe.Eleves.Contains(request.EleveName))
            {
                classe.Eleves.Add(request.EleveName);
            }

            return Ok(new
            {
                message = $"Élève '{request.EleveName}' ajouté à la classe '{request.ClasseName}'",
                classe = classe
            });
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
            return Ok(new { message = "Information publique sur les classes" });
        }
    }
}