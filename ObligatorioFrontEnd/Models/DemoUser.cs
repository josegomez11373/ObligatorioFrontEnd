namespace ObligatorioFrontEnd.Models;

public sealed record DemoUser(string Email, string Name, UserRole Role, string Scope)
{
    public string Initials => string.Concat(Name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(part => char.ToUpperInvariant(part[0])));
}
