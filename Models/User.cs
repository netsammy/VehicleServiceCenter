namespace VehicleServiceCenter.Models;

public class User
{
    public string? Username { get; set; }
    public string? PasswordHash { get; set; }
    public bool IsAuthenticated { get; set; }
}
