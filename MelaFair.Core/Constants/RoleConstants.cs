namespace MelaFair.Core.Constants;

/// <summary>
/// System-wide role names for ASP.NET Core Identity authorization
/// </summary>
public static class RoleConstants
{
    public const string Admin = "Admin";
    public const string Vendor = "Vendor";
    public const string Visitor = "Visitor";
    public const string Employee = "Employee";

    public static readonly string[] AllRoles = [Admin, Vendor, Visitor, Employee];
}
