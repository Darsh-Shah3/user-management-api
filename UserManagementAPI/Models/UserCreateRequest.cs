using System.ComponentModel.DataAnnotations;

namespace UserManagementAPI.Models;

public sealed record UserCreateRequest(
    [property: Required, MinLength(1)] string FirstName,
    [property: Required, MinLength(1)] string LastName,
    [property: Required, EmailAddress] string Email,
    [property: Required, MinLength(1)] string Department
);

