namespace ElectronicLibrary.BLL.Constants;

public static class ApplicationRoles
{
    public const string Admin = "Admin";

    public const string Customer = "Customer";

    public const string Seller = "Seller";

    public static readonly string[] All =
    [
        Admin,
        Customer,
        Seller
    ];
}