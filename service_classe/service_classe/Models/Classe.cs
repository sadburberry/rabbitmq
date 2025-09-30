namespace service_classe.Models;

public class Classe
{
    public int Id { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string Niveau { get; set; } = string.Empty;
    public int Capacite { get; set; }
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;
    public List<string> Eleves { get; set; } = new List<string>(); // ← AJOUTE CETTE LIGNE
}