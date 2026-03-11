using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using SafeVault.Data;
using SafeVault.DTOs;
using SafeVault.Models;

[TestFixture]
public class TestSecurity
{
    private static bool Validate(object model)
    {
        var context = new ValidationContext(model);
        var results = new List<ValidationResult>();
        return Validator.TryValidateObject(model, context, results, true);
    }

    private static AppDbContext CreateInMemoryDb()
    {
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options);
        db.Database.OpenConnection();
        db.Database.EnsureCreated();
        return db;
    }

    // === XSS ===

    [Test]
    public void XSS_ScriptTagInUsername_IsRejectedByDTOValidation()
    {
        var request = new RegisterRequest
        {
            Username = "<script>alert(1)</script>",
            Email = "test@example.com",
            Password = "SecurePass1!"
        };
        Assert.That(Validate(request), Is.False);
    }

    // === SQL Injection ===

    [Test]
    public async Task SQLInjection_TautologyPayload_DoesNotReturnAnyUser()
    {
        using var db = CreateInMemoryDb();
        db.Users.Add(new User { Username = "admin", Email = "admin@example.com", PasswordHash = "hash" });
        await db.SaveChangesAsync();

        var payload = "' OR '1'='1";
        var result = await db.Users.FirstOrDefaultAsync(u => u.Username == payload);

        Assert.That(result, Is.Null);
    }

    // === RBAC ===

    [Test]
    public void RBAC_NewUserReceivesUserRole()
    {
        var user = new User
        {
            Username = "newuser",
            Email = "new@example.com",
            PasswordHash = "hash"
        };
        Assert.That(user.Role, Is.EqualTo("User"));
    }

    [Test]
    public async Task RBAC_AdminRoleCanBeAssigned()
    {
        using var db = CreateInMemoryDb();
        var admin = new User
        {
            Username = "admin",
            Email = "admin@example.com",
            PasswordHash = "hash",
            Role = "Admin"
        };
        db.Users.Add(admin);
        await db.SaveChangesAsync();

        var found = await db.Users.FirstOrDefaultAsync(u => u.Username == "admin");
        Assert.That(found!.Role, Is.EqualTo("Admin"));
    }

    [Test]
    public async Task RBAC_UserRoleDiffersFromAdmin()
    {
        using var db = CreateInMemoryDb();
        db.Users.Add(new User { Username = "admin", Email = "a@x.com", PasswordHash = "h", Role = "Admin" });
        db.Users.Add(new User { Username = "regular", Email = "r@x.com", PasswordHash = "h", Role = "User" });
        await db.SaveChangesAsync();

        var admin = await db.Users.FirstAsync(u => u.Username == "admin");
        var user = await db.Users.FirstAsync(u => u.Username == "regular");

        Assert.That(admin.Role, Is.Not.EqualTo(user.Role));
    }

    [Test]
    public async Task RBAC_OnlyAdminRoleExistsAmongAdmins()
    {
        using var db = CreateInMemoryDb();
        db.Users.Add(new User { Username = "admin", Email = "a@x.com", PasswordHash = "h", Role = "Admin" });
        db.Users.Add(new User { Username = "u1", Email = "u1@x.com", PasswordHash = "h" });
        db.Users.Add(new User { Username = "u2", Email = "u2@x.com", PasswordHash = "h" });
        await db.SaveChangesAsync();

        var admins = await db.Users.Where(u => u.Role == "Admin").ToListAsync();
        Assert.That(admins, Has.Count.EqualTo(1));
        Assert.That(admins[0].Username, Is.EqualTo("admin"));
    }
}