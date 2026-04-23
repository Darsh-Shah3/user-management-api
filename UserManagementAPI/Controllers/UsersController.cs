using Microsoft.AspNetCore.Mvc;
using UserManagementAPI.Data;
using UserManagementAPI.Models;

namespace UserManagementAPI.Controllers;

[ApiController]
[Route("users")]
public sealed class UsersController(UserRepository repo) : ControllerBase
{
    [HttpGet]
    public ActionResult<IReadOnlyList<User>> GetUsers([FromQuery] int? skip, [FromQuery] int? take)
    {
        var s = Math.Max(skip ?? 0, 0);
        var t = Math.Clamp(take ?? 100, 1, 500);
        return Ok(repo.GetAll(s, t));
    }

    [HttpGet("{id:int}")]
    public ActionResult<User> GetUserById(int id)
    {
        var user = repo.GetById(id);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPost]
    public ActionResult<User> CreateUser([FromBody] UserCreateRequest request)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var email = request.Email.Trim();
        if (repo.EmailExists(email))
        {
            ModelState.AddModelError(nameof(UserCreateRequest.Email), "Email must be unique.");
            return ValidationProblem(ModelState);
        }

        var created = repo.Create(request);
        return Created($"/users/{created.Id}", created);
    }

    [HttpPut("{id:int}")]
    public ActionResult<User> UpdateUser(int id, [FromBody] UserUpdateRequest request)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var found = repo.TryUpdate(id, request, out var updated, out var emailConflict);
        if (!found) return NotFound();

        if (emailConflict is not null)
        {
            ModelState.AddModelError(nameof(UserUpdateRequest.Email), emailConflict);
            return ValidationProblem(ModelState);
        }

        return Ok(updated);
    }

    [HttpDelete("{id:int}")]
    public IActionResult DeleteUser(int id)
    {
        return repo.Delete(id) ? NoContent() : NotFound();
    }
}

