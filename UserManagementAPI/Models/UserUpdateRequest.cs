using System.ComponentModel.DataAnnotations;

namespace UserManagementAPI.Models;

public sealed record UserUpdateRequest(
    [property: MinLength(1)] string? FirstName,
    [property: MinLength(1)] string? LastName,
    [property: EmailAddress] string? Email,
    [property: MinLength(1)] string? Department
);

