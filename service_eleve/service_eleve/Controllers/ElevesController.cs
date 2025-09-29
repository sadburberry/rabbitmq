using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using service_eleve.Data;
using service_eleve.Models;

namespace service_eleve.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ElevesController : ControllerBase
{
    private readonly EleveContext _context;

    public ElevesController(EleveContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Eleve>>> GetEleves()
    {
        return await _context.Eleves.ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Eleve>> GetEleve(int id)
    {
        var eleve = await _context.Eleves.FindAsync(id);
        if (eleve == null)
        {
            return NotFound();
        }
        return eleve;
    }

    [HttpGet("classe/{classeId}")]
    public async Task<ActionResult<IEnumerable<Eleve>>> GetElevesByClasse(int classeId)
    {
        return await _context.Eleves.Where(e => e.ClasseId == classeId).ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<Eleve>> CreateEleve(Eleve eleve)
    {
        _context.Eleves.Add(eleve);
        await _context.SaveChangesAsync();

        Console.WriteLine($"✅ Élève créé: {eleve.Prenom} {eleve.Nom} (Classe: {eleve.ClasseId})");

        return CreatedAtAction(nameof(GetEleves), new { id = eleve.Id }, eleve);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateEleve(int id, Eleve eleve)
    {
        if (id != eleve.Id)
        {
            return BadRequest();
        }

        _context.Entry(eleve).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!EleveExists(id))
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

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEleve(int id)
    {
        var eleve = await _context.Eleves.FindAsync(id);
        if (eleve == null)
        {
            return NotFound();
        }

        _context.Eleves.Remove(eleve);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("test")]
    public ActionResult<string> Test()
    {
        return "✅ Service Élève avec RabbitMQ fonctionne !";
    }

    private bool EleveExists(int id)
    {
        return _context.Eleves.Any(e => e.Id == id);
    }
}