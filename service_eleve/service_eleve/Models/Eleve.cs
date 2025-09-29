namespace service_eleve.Models;

public class Eleve
{
    public int Id { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string Prenom { get; set; } = string.Empty;
    public DateTime DateNaissance { get; set; }
    public int ClasseId { get; set; }
    public DateTime DateInscription { get; set; } = DateTime.UtcNow;
}