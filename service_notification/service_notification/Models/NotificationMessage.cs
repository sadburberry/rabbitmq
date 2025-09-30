namespace service_notification.Models;

public class NotificationMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Type { get; set; } = string.Empty; // "absence", "urgence", "info"
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty; // "classe:TerminaleA", "niveau:Secondaire", "all"
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string Sender { get; set; } = "System";
}

public class AbsenceNotification
{
    public string ProfessorName { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public DateTime AbsenceDate { get; set; } = DateTime.Now;
    public string Reason { get; set; } = string.Empty;
}

public class EmergencyNotification
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Priority { get; set; } = "high"; // high, medium, low
}