namespace TaxiSystem.Api.Models;

public static class UserRoles
{
    public const string Passenger = "passenger";
    public const string Driver = "driver";

    /// <summary>Роль у родовому відмінку для повідомлень: «водія» / «пасажира».</summary>
    public static string Label(string role) => role == Driver ? "водія" : "пасажира";

    public static string Opposite(string role) => role == Driver ? Passenger : Driver;
}
