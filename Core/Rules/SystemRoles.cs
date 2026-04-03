namespace Muzu.Api.Core.Rules;

public static class SystemRoles
{
    public const string Administrador = "Administrador";
    public const string Presidente = "Presidente";
    public const string Secretario = "Secretario";
    public const string Tesorero = "Tesorero";
    public const string Vocal = "Vocal";
    public const string Contador = "Contador";
    public const string Socio = "Socio";

    public static bool EsRolProtegidoParaAutodemocion(string roleName)
    {
        return string.Equals(roleName, Administrador, StringComparison.OrdinalIgnoreCase)
            || string.Equals(roleName, Presidente, StringComparison.OrdinalIgnoreCase);
    }

    public static bool EsRolAdministradorDeUsuarios(string roleName)
    {
        return string.Equals(roleName, Administrador, StringComparison.OrdinalIgnoreCase)
            || string.Equals(roleName, Presidente, StringComparison.OrdinalIgnoreCase);
    }
}
