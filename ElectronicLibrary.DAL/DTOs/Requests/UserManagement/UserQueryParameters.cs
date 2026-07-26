using System.ComponentModel.DataAnnotations;

namespace ElectronicLibrary.DAL.DTOs.Requests.UserManagement;

public class UserQueryParameters
{
    [MaxLength(200)]
    public string? Search { get; set; }

    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;

    public bool IncludeDeleted { get; set; }
}
