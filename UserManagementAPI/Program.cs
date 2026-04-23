var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseMiddleware<UserManagementAPI.Middleware.ErrorHandlingMiddleware>();

app.UseHttpsRedirection();

app.UseMiddleware<UserManagementAPI.Middleware.TokenAuthMiddleware>();
app.UseMiddleware<UserManagementAPI.Middleware.RequestResponseLoggingMiddleware>();

var users = new System.Collections.Concurrent.ConcurrentDictionary<int, User>(
    new[]
    {
        new KeyValuePair<int, User>(1, new User(1, "Ava", "Patel", "ava.patel@techhive.com", "HR")),
        new KeyValuePair<int, User>(2, new User(2, "Noah", "Kim", "noah.kim@techhive.com", "IT")),
    }
);

var emailIndex = new System.Collections.Concurrent.ConcurrentDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
foreach (var u in users.Values)
{
    emailIndex.TryAdd(u.Email, u.Id);
}

var nextId = users.Keys.DefaultIfEmpty(0).Max();

int GetNextId() => Interlocked.Increment(ref nextId);

static Dictionary<string, string[]> Validate(object model)
{
    var ctx = new System.ComponentModel.DataAnnotations.ValidationContext(model);
    var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
    var ok = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(
        model,
        ctx,
        results,
        validateAllProperties: true
    );

    if (ok) return new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

    var errors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
    foreach (var r in results)
    {
        var members = r.MemberNames?.Any() == true ? r.MemberNames : new[] { "" };
        foreach (var m in members)
        {
            if (!errors.TryGetValue(m, out var list))
            {
                list = new List<string>();
                errors[m] = list;
            }
            list.Add(r.ErrorMessage ?? "Invalid value.");
        }
    }

    return errors.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToArray(), StringComparer.OrdinalIgnoreCase);
}

static bool IsNullOrWhiteSpace(string? s) => string.IsNullOrWhiteSpace(s);

User? GetUser(int id) => users.TryGetValue(id, out var u) ? u : null;

var usersApi = app.MapGroup("/users").WithTags("Users");

usersApi.MapGet("/", (int? skip, int? take) =>
{
    var s = Math.Max(skip ?? 0, 0);
    var t = Math.Clamp(take ?? 100, 1, 500);

    var page = users.Values
        .OrderBy(u => u.Id)
        .Skip(s)
        .Take(t)
        .ToArray();

    return Results.Ok(page);
});

usersApi.MapGet("/{id:int}", (int id) =>
{
    var user = GetUser(id);
    return user is null ? Results.NotFound() : Results.Ok(user);
});

usersApi.MapPost("/", (UserCreateRequest request) =>
{
    var errors = Validate(request);
    if (errors.Count > 0) return Results.ValidationProblem(errors);

    var email = request.Email.Trim();
    if (emailIndex.ContainsKey(email))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["Email"] = new[] { "Email must be unique." }
        });
    }

    var user = new User(
        GetNextId(),
        request.FirstName.Trim(),
        request.LastName.Trim(),
        email,
        request.Department.Trim()
    );

    users[user.Id] = user;
    emailIndex[email] = user.Id;
    return Results.Created($"/users/{user.Id}", user);
});

usersApi.MapPut("/{id:int}", (int id, UserUpdateRequest request) =>
{
    var existing = GetUser(id);
    if (existing is null) return Results.NotFound();

    // Validate only provided fields (avoid forcing required fields on update)
    if (IsNullOrWhiteSpace(request.FirstName) == false && request.FirstName!.Trim().Length == 0)
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["FirstName"] = new[] { "FirstName cannot be empty." } });
    if (IsNullOrWhiteSpace(request.LastName) == false && request.LastName!.Trim().Length == 0)
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["LastName"] = new[] { "LastName cannot be empty." } });
    if (IsNullOrWhiteSpace(request.Department) == false && request.Department!.Trim().Length == 0)
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["Department"] = new[] { "Department cannot be empty." } });

    if (request.Email is not null)
    {
        var emailErrors = Validate(new EmailOnly(request.Email));
        if (emailErrors.Count > 0) return Results.ValidationProblem(emailErrors);

        var newEmail = request.Email.Trim();
        if (!newEmail.Equals(existing.Email, StringComparison.OrdinalIgnoreCase) && emailIndex.ContainsKey(newEmail))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["Email"] = new[] { "Email must be unique." }
            });
        }
    }

    var updated = existing with
    {
        FirstName = request.FirstName?.Trim() ?? existing.FirstName,
        LastName = request.LastName?.Trim() ?? existing.LastName,
        Email = request.Email?.Trim() ?? existing.Email,
        Department = request.Department?.Trim() ?? existing.Department,
    };

    users[id] = updated;

    if (!updated.Email.Equals(existing.Email, StringComparison.OrdinalIgnoreCase))
    {
        emailIndex.TryRemove(existing.Email, out _);
        emailIndex[updated.Email] = updated.Id;
    }

    return Results.Ok(updated);
});

usersApi.MapDelete("/{id:int}", (int id) =>
{
    if (!users.TryRemove(id, out var removed)) return Results.NotFound();

    emailIndex.TryRemove(removed.Email, out _);
    return Results.NoContent();
});

app.Run();

record User(int Id, string FirstName, string LastName, string Email, string Department);

record UserCreateRequest(
    [property: System.ComponentModel.DataAnnotations.Required]
    [property: System.ComponentModel.DataAnnotations.MinLength(1)]
    string FirstName,

    [property: System.ComponentModel.DataAnnotations.Required]
    [property: System.ComponentModel.DataAnnotations.MinLength(1)]
    string LastName,

    [property: System.ComponentModel.DataAnnotations.Required]
    [property: System.ComponentModel.DataAnnotations.EmailAddress]
    string Email,

    [property: System.ComponentModel.DataAnnotations.Required]
    [property: System.ComponentModel.DataAnnotations.MinLength(1)]
    string Department
);

record UserUpdateRequest(string? FirstName, string? LastName, string? Email, string? Department);

record EmailOnly([property: System.ComponentModel.DataAnnotations.EmailAddress] string Email);
