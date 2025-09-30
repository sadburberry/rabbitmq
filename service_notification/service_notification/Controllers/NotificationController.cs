using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using RabbitMQ.Client;
using service_notification.Models;
using System.Text;
using System.Text.Json;

namespace service_notification.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize] // 🔐 Toutes les routes protégées
    public class NotificationController : ControllerBase
    {
        private readonly RabbitMQ.Client.IConnectionFactory _connectionFactory;

        public NotificationController(RabbitMQ.Client.IConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        [HttpPost("absence")]
        [Authorize(Policy = "AdminOrTeacher")] // 🔐 Seulement admin/teacher
        public IActionResult SendAbsenceNotification([FromBody] AbsenceNotification request)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                using var channel = connection.CreateModel();

                channel.ExchangeDeclare(exchange: "notifications_absences", type: RabbitMQ.Client.ExchangeType.Direct);

                var notification = new NotificationMessage
                {
                    Type = "absence",
                    Title = $"Absence du professeur {request.ProfessorName}",
                    Message = $"Le professeur {request.ProfessorName} est absent {request.Reason}. Classe: {request.ClassName}",
                    Target = $"classe:{request.ClassName}",
                    Sender = "Service Absence"
                };

                var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(notification));
                channel.BasicPublish(exchange: "notifications_absences", routingKey: request.ClassName, basicProperties: null, body: body);

                return Ok(new
                {
                    message = $"Notification d'absence envoyée pour la classe {request.ClassName}",
                    notification = notification
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("urgence")]
        [Authorize(Policy = "AdminOnly")] // 🔐 Seulement admin
        public IActionResult SendEmergencyNotification([FromBody] EmergencyNotification request)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                using var channel = connection.CreateModel();

                channel.ExchangeDeclare(exchange: "notifications_urgences", type: RabbitMQ.Client.ExchangeType.Fanout);

                var notification = new NotificationMessage
                {
                    Type = "urgence",
                    Title = request.Title,
                    Message = request.Message,
                    Target = "all",
                    Sender = "Administration",
                    CreatedAt = DateTime.UtcNow
                };

                var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(notification));
                channel.BasicPublish(exchange: "notifications_urgences", routingKey: "", basicProperties: null, body: body);

                return Ok(new
                {
                    message = "Notification d'urgence envoyée à tous",
                    notification = notification
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("info")]
        [Authorize(Policy = "AdminOrTeacher")] // 🔐 Seulement admin/teacher
        public IActionResult SendInfoNotification([FromBody] NotificationMessage request)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                using var channel = connection.CreateModel();

                channel.ExchangeDeclare(exchange: "notifications_info", type: RabbitMQ.Client.ExchangeType.Topic);
                var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request));
                var routingKey = request.Target.Replace(":", ".");
                channel.BasicPublish(exchange: "notifications_info", routingKey: routingKey, basicProperties: null, body: body);

                return Ok(new
                {
                    message = $"Notification info envoyée: {request.Target}",
                    notification = request
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("test")]
        [Authorize(Policy = "AdminOrTeacher")]
        public IActionResult Test()
        {
            var userName = User.Identity?.Name;
            return Ok(new
            {
                message = $"Service Notification is working! User: {userName}",
                user = User.Claims.Select(c => new { c.Type, c.Value })
            });
        }

        [HttpGet("test-rabbitmq")]
        [Authorize(Policy = "AdminOrTeacher")]
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
            return Ok(new { message = "Information publique sur les notifications" });
        }
    }
}