using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using LexCore.Entities;

namespace LexCore.Auth;

public class LexAuthUser
{
    public static LexAuthUser? FromClaimsPrincipal(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated is not true) return null;
        var jsonObject = new JsonObject();
        var typeInfo = JsonSerializerOptions.Default.GetTypeInfo(typeof(LexAuthUser));
        foreach (var property in typeInfo.Properties)
        {
            var isArray = property.PropertyType != typeof(string) &&
                          property.PropertyType.IsAssignableTo(typeof(IEnumerable));
            var array = isArray ? new JsonArray() : null;
            if (isArray)
            {
                jsonObject.Add(property.Name, array);
            }

            foreach (var claim in principal.FindAll(property.Name))
            {
                if (claim.Subject?.IsAuthenticated is not true) continue;
                if (array is null)
                {
                    jsonObject.Add(claim.Type, JsonValue.Create(claim.Value));
                    continue;
                }

                //claim json arrays may be a single object or an array of objects
                //we need to handle that properly here
                if (claim.ValueType != JsonClaimValueTypes.JsonArray)
                {
                    array.Add(JsonSerializer.Deserialize<JsonObject>(claim.Value));
                    continue;
                }

                var claimArray = JsonSerializer.Deserialize<JsonObject[]>(claim.Value);
                if (claimArray is null) continue;
                foreach (var item in claimArray)
                {
                    array.Add(item);
                }
            }
        }

        var user = jsonObject.Deserialize<LexAuthUser>();
        if (user is null) throw new Exception("Could not deserialize user");
        return user;
    }

    public LexAuthUser()
    {
    }

    [SetsRequiredMembers]
    public LexAuthUser(User user)
    {
        Id = user.Id;
        Email = user.Email;
        Role = user.IsAdmin ? UserRole.admin : UserRole.user;
        Name = user.Name;
        Projects = user.Projects.Select(p => new AuthUserProject(p.Project.Code, p.Role, p.ProjectId)).ToArray();
    }

    [JsonPropertyName(LexAuthConstants.IdClaimType)]
    public required Guid Id { get; set; }

    [JsonPropertyName(LexAuthConstants.EmailClaimType)]
    public required string Email { get; set; }

    [JsonPropertyName(LexAuthConstants.NameClaimType)]
    public required string Name { get; set; }

    [JsonPropertyName(LexAuthConstants.RoleClaimType)]
    public required UserRole Role { get; set; }

    [JsonPropertyName(LexAuthConstants.ProjectsClaimType)]
    public required AuthUserProject[] Projects { get; init; }

    public IEnumerable<Claim> GetClaims()
    {
        var jsonElement = JsonSerializer.SerializeToElement(this);
        foreach (var jsonProperty in jsonElement.EnumerateObject())
        {
            switch (jsonProperty.Value.ValueKind)
            {
                //we flatten arrays into multiple claims because that's how jwt handles it
                case JsonValueKind.Array:
                    foreach (var element in jsonProperty.Value.EnumerateArray())
                    {
                        yield return new Claim(jsonProperty.Name, element.ToString(), JsonClaimValueTypes.Json);
                    }

                    break;
                case JsonValueKind.Object:
                    yield return new Claim(jsonProperty.Name, jsonProperty.Value.ToString(), JsonClaimValueTypes.Json);
                    break;
                default:
                    yield return new Claim(jsonProperty.Name, jsonProperty.Value.ToString());
                    break;
            }
        }
    }

    public ClaimsPrincipal GetPrincipal(string authenticationType)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(GetClaims(),
            authenticationType,
            LexAuthConstants.EmailClaimType,
            LexAuthConstants.RoleClaimType));
    }

    public bool CanManageProject(Guid projectId)
    {
        return Role == UserRole.admin || Projects.Any(p => p.ProjectId == projectId && p.Role == ProjectRole.Manager);
    }

    public bool CanManageProject(string projectCode)
    {
        return Role == UserRole.admin || Projects.Any(p => p.Code == projectCode && p.Role == ProjectRole.Manager);
    }

    public void AssertCanManageProject(Guid projectId)
    {
        if (!CanManageProject(projectId)) throw new UnauthorizedAccessException();
    }

    public void AssertCanAccessProject(string projectCode)
    {
        if (Role != UserRole.admin && Projects.All(p => p.Code != projectCode)) throw new UnauthorizedAccessException();
    }
}

public record AuthUserProject(string Code, ProjectRole Role, Guid ProjectId);

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UserRole
{
    admin = 0,
    user = 1
}
