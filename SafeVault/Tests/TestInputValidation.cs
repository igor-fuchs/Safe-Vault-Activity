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

    private static AppDbContext CreateInMemoryDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    // === XSS — DTO Validation Layer ===
    // RegisterRequest.Username has [RegularExpression(@"^[a-zA-Z0-9_-]+$")]
    // which rejects any character used in HTML/JS injection at the DTO boundary.

    [TestCase("<script>alert(1)</script>")]
    [TestCase("<img onerror=alert(1)>")]
    [TestCase("\" onmouseover=\"alert(1)")]
    [TestCase("javascript:alert(1)")]
    public void XSS_UsernameWithHtmlOrJsPayload_IsRejectedByDTOValidation(string payload)
    {
        var request = new RegisterRequest { Username = payload, Email = "test@example.com", Password = "SecurePass1!" };
        Assert.That(Validate(request), Is.False);
    }

    [TestCase("<img src=x onerror=alert(1)>")]
    [TestCase("<iframe src='evil.com'>")]
    public void XSS_EmailWithHtmlPayload_IsRejectedByEmailValidation(string payload)
    {
        var request = new RegisterRequest { Username = "john_doe", Email = payload, Password = "SecurePass1!" };
        Assert.That(Validate(request), Is.False);
    }

    [Test]
    public void XSS_JsonSerializer_EscapesHtmlCharsInApiResponse()
    {
        // System.Text.Json (ASP.NET default) encodes <, > and & to \uXXXX by default,
        // preventing reflected XSS if a client renders the JSON body as HTML.
        var response = new UserResponse { Id = 1, Username = "<script>alert(1)</script>", Email = "x@x.com" };
        var json = JsonSerializer.Serialize(response);

        Assert.That(json, Does.Not.Contain("<script>"), "Raw angle brackets must not appear in JSON output");
        Assert.That(json, Does.Contain("\\u003c").Or.Contain("\\u003C"), "< must be unicode-escaped in JSON");
    }

    // === SQL Injection — EF Core Parameterization ===
    // EF Core translates LINQ lambdas to parameterized SQL (WHERE Username = @p0).
    // The payload is bound as a literal value and is never interpreted as SQL syntax.

    [TestCase("' OR '1'='1")]
    [TestCase("' OR 1=1 --")]
    [TestCase("' OR 'x'='x")]
    public async Task SQLInjection_TautologyPayloadInLogin_DoesNotReturnAnyUser(string payload)
    {
        using var db = CreateInMemoryDb();
        db.Users.Add(new User { Username = "admin", Email = "admin@example.com", PasswordHash = "hash" });
        await db.SaveChangesAsync();

        // This mirrors UserService.LoginAsync: the payload is a parameter value, not SQL
        var result = await db.Users.FirstOrDefaultAsync(u => u.Username == payload);

        Assert.That(result, Is.Null, $"SQL tautology '{payload}' must not bypass user lookup");
    }

    [Test]
    public async Task SQLInjection_DropTablePayload_DoesNotAffectDatabase()
    {
        using var db = CreateInMemoryDb();
        db.Users.Add(new User { Username = "admin", Email = "admin@example.com", PasswordHash = "hash" });
        await db.SaveChangesAsync();

        var payload = "'; DROP TABLE Users; --";
        await db.Users.FirstOrDefaultAsync(u => u.Username == payload);

        Assert.That(await db.Users.CountAsync(), Is.EqualTo(1), "DROP TABLE payload must not remove records");
    }

    [Test]
    public async Task SQLInjection_UnionPayload_DoesNotLeakOtherUsersData()
    {
        using var db = CreateInMemoryDb();
        db.Users.Add(new User { Username = "alice", Email = "alice@example.com", PasswordHash = "hash1" });
        db.Users.Add(new User { Username = "bob", Email = "bob@example.com", PasswordHash = "hash2" });
        await db.SaveChangesAsync();

        var payload = "' UNION SELECT Id, Username, Email, PasswordHash FROM Users --";
        var result = await db.Users.FirstOrDefaultAsync(u => u.Username == payload);

        Assert.That(result, Is.Null, "UNION injection payload must not return any user");
    }

    // SQL payloads in Username also fail RegisterRequest DTO validation due to [RegularExpression]
    [TestCase("' OR 1=1 --")]
    [TestCase("'; DROP TABLE Users; --")]
    [TestCase("' UNION SELECT * FROM Users --")]
    public void SQLInjection_UsernamePayload_IsAlsoRejectedByDTOValidation(string payload)
    {
        var request = new RegisterRequest { Username = payload, Email = "test@example.com", Password = "SecurePass1!" };
        Assert.That(Validate(request), Is.False);
    }
}