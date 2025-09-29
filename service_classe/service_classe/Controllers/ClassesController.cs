using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using System.Text;
using service_classe.Data;
using service_classe.Models;

namespace service_classe.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClassesController : ControllerBase
{
    private readonly ClasseContext _context;
    private readonly IConnection _rabbitConnection;
    private readonly ILogger<ClassesController> _logger;

    public ClassesController(ClasseContext context, IConnection rabbitConnection, ILogger<ClassesController> logger)
    {
        _context = context;
        _rabbitConnection = rabbitConnection;
        _logger = logger;
    }

    // GET: api/classes
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Classe>>> GetClasses()
    {
        return await _context.Classes.ToListAsync();
    }

    // GET: api/classes/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Classe>> GetClasse(int id)
    {
        var classe = await _context.Classes.FindAsync(id);

        if (classe == null)
        {
            return NotFound();
        }

        return classe;
    }

    // POST: api/classes
    [HttpPost]
    public async Task<ActionResult<Classe>> CreateClasse(Classe classe)
    {
        _context.Classes.Add(classe);
        await _context.SaveChangesAsync();

        // Envoyer un message RabbitMQ
        try
        {
            using var channel = _rabbitConnection.CreateModel();

            // Déclarer une exchange (si elle n'existe pas)
            channel.ExchangeDeclare(exchange: "classes", type: ExchangeType.Fanout, durable: true);

            // Créer le message
            var message = System.Text.Json.JsonSerializer.Serialize(new
            {
                ClasseId = classe.Id,
                Nom = classe.Nom,
                Niveau = classe.Niveau,
                Capacite = classe.Capacite,
                DateCreation = classe.DateCreation,
                TypeEvenement = "ClasseCreee"
            });

            var body = Encoding.UTF8.GetBytes(message);

            // Publier le message
            channel.BasicPublish(exchange: "classes",
                               routingKey: "",
                               basicProperties: null,
                               body: body);

            _logger.LogInformation($"✅ Message RabbitMQ envoyé: Classe {classe.Nom} créée (ID: {classe.Id})");
        }
        catch (Exception ex)
        {
            _logger.LogError($"❌ Erreur envoi RabbitMQ: {ex.Message}");
        }

        return CreatedAtAction(nameof(GetClasse), new { id = classe.Id }, classe);
    }

    // PUT: api/classes/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateClasse(int id, Classe classe)
    {
        if (id != classe.Id)
        {
            return BadRequest();
        }

        _context.Entry(classe).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ClasseExists(id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return NoContent();
    }

    // DELETE: api/classes/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteClasse(int id)
    {
        var classe = await _context.Classes.FindAsync(id);
        if (classe == null)
        {
            return NotFound();
        }

        _context.Classes.Remove(classe);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // Endpoint de test
    [HttpGet("test")]
    public ActionResult<string> Test()
    {
        return "✅ Service Classe fonctionne avec RabbitMQ !";
    }

    private bool ClasseExists(int id)
    {
        return _context.Classes.Any(e => e.Id == id);
    }
}