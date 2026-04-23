using System.Collections.Concurrent;
using UserManagementAPI.Models;

namespace UserManagementAPI.Data;

public sealed class UserRepository
{
    private readonly ConcurrentDictionary<int, User> _users;
    private readonly ConcurrentDictionary<string, int> _emailIndex;
    private int _nextId;

    public UserRepository()
    {
        _users = new ConcurrentDictionary<int, User>(
            new[]
            {
                new KeyValuePair<int, User>(1, new User(1, "Ava", "Patel", "ava.patel@techhive.com", "HR")),
                new KeyValuePair<int, User>(2, new User(2, "Noah", "Kim", "noah.kim@techhive.com", "IT")),
            }
        );

        _emailIndex = new ConcurrentDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var u in _users.Values)
        {
            _emailIndex.TryAdd(u.Email, u.Id);
        }

        _nextId = _users.Keys.DefaultIfEmpty(0).Max();
    }

    public IReadOnlyList<User> GetAll(int skip, int take) =>
        _users.Values.OrderBy(u => u.Id).Skip(skip).Take(take).ToArray();

    public User? GetById(int id) => _users.TryGetValue(id, out var u) ? u : null;

    public bool EmailExists(string email) => _emailIndex.ContainsKey(email);

    public User Create(UserCreateRequest request)
    {
        var id = Interlocked.Increment(ref _nextId);
        var email = request.Email.Trim();

        var user = new User(
            id,
            request.FirstName.Trim(),
            request.LastName.Trim(),
            email,
            request.Department.Trim()
        );

        _users[user.Id] = user;
        _emailIndex[email] = user.Id;
        return user;
    }

    public bool TryUpdate(int id, UserUpdateRequest request, out User updated, out string? emailConflict)
    {
        emailConflict = null;
        updated = default!;

        var existing = GetById(id);
        if (existing is null) return false;

        var newEmail = request.Email?.Trim();
        if (!string.IsNullOrWhiteSpace(newEmail) &&
            !newEmail.Equals(existing.Email, StringComparison.OrdinalIgnoreCase) &&
            EmailExists(newEmail))
        {
            emailConflict = "Email must be unique.";
            return true;
        }

        updated = existing with
        {
            FirstName = request.FirstName?.Trim() ?? existing.FirstName,
            LastName = request.LastName?.Trim() ?? existing.LastName,
            Email = newEmail ?? existing.Email,
            Department = request.Department?.Trim() ?? existing.Department,
        };

        _users[id] = updated;

        if (!updated.Email.Equals(existing.Email, StringComparison.OrdinalIgnoreCase))
        {
            _emailIndex.TryRemove(existing.Email, out _);
            _emailIndex[updated.Email] = updated.Id;
        }

        return true;
    }

    public bool Delete(int id)
    {
        if (!_users.TryRemove(id, out var removed)) return false;
        _emailIndex.TryRemove(removed.Email, out _);
        return true;
    }
}

