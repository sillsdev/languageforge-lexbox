using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using LexCore.Entities;

namespace LexCore.Auth;

public record LexAuthUser
{
    private static readonly JsonTypeInfo LexAuthUserTypeInfo =
        JsonSerializerOptions.Default.GetTypeInfo(typeof(LexAuthUser));

    public static LexAuthUser? FromClaimsPrincipal(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated is not true) return null;
        var jsonObject = new JsonObject();
        foreach (var property in LexAuthUserTypeInfo.Properties)
        {
            // if (property)
            var isArray = property.PropertyType != typeof(string) &&
                          property.PropertyType.IsAssignableTo(typeof(IEnumerable));
            var array = isArray ? new JsonArray() : null;


            foreach (var claim in principal.FindAll(property.Name))
            {
                if (claim.Subject?.IsAuthenticated is not true) continue;
                if (array is null)
                {
                    switch (claim.ValueType)
                    {
                        case ClaimValueTypes.Boolean:
                            jsonObject.Add(claim.Type, JsonValue.Create(bool.Parse(claim.Value)));
                            continue;
                        default:
                            jsonObject.Add(claim.Type, JsonValue.Create(claim.Value));
                            continue;
                    }
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

            if (array?.Count > 0)
            {
                jsonObject.Add(property.Name, array);
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
        Projects = user.IsAdmin
            ? Array.Empty<AuthUserProject>()
            : user.Projects.Select(p => new AuthUserProject(p.Project.Code, p.Role, p.ProjectId)).ToArray();
        EmailVerificationRequired = user.EmailVerified ? null : true;
        CanCreateProjects = user.CanCreateProjects ? true : null;
    }

    [JsonPropertyName(LexAuthConstants.IdClaimType)]
    public required Guid Id { get; set; }

    [JsonPropertyName(LexAuthConstants.AudienceClaimType)]
    public LexboxAudience Audience { get; set; } = LexboxAudience.LexboxApi;

    [JsonPropertyName(LexAuthConstants.EmailClaimType)]
    public required string Email { get; set; }

    [JsonPropertyName(LexAuthConstants.NameClaimType)]
    public required string Name { get; set; }

    [JsonPropertyName(LexAuthConstants.RoleClaimType)]
    public required UserRole Role { get; set; }

    [JsonPropertyName(LexAuthConstants.ProjectsClaimType)]
    public AuthUserProject[] Projects { get; set; } = Array.Empty<AuthUserProject>();

    [JsonIgnore]
    public string ProjectsJson
    {
        get =>
            string.Join(",",
                Projects.GroupBy(p => p.Role).Select(roleGroup =>
                {
                    var projectRole = roleGroup.Key switch
                    {
                        ProjectRole.Manager => "m",
                        ProjectRole.Editor => "e",
                        _ => "u"
                    };

                    var projectString = string.Join("|", roleGroup.Select(p => p.ProjectId.ToString("N")));
                    return $"{projectRole}{projectString}";
                }));
        set
        {
            Projects = value.Split(",").SelectMany(p =>
            {
                var role = p[0] switch
                {
                    'm' => ProjectRole.Manager,
                    'e' => ProjectRole.Editor,
                    _ => ProjectRole.Unknown
                };
                return p[1..].Split("|").Select(pId => Guid.Parse(pId))
                    .Select(pId => new AuthUserProject("na", role, pId));
            }).ToArray();
        }
    }

    [JsonPropertyName(LexAuthConstants.EmailUnverifiedClaimType)]
    public bool? EmailVerificationRequired { get; init; }
    [JsonPropertyName(LexAuthConstants.CanCreateProjectClaimType)]
    public bool? CanCreateProjects { get; init; }

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
                        var valueType = element.ValueKind switch
                        {
                            JsonValueKind.Array => JsonClaimValueTypes.JsonArray,
                            JsonValueKind.String => ClaimValueTypes.String,
                            _ => JsonClaimValueTypes.Json
                        };
                        yield return new Claim(jsonProperty.Name, element.ToString(), valueType);
                    }

                    break;
                case JsonValueKind.Object:
                    yield return new Claim(jsonProperty.Name, jsonProperty.Value.ToString(), JsonClaimValueTypes.Json);
                    break;
                case JsonValueKind.True:
                case JsonValueKind.False:
                    yield return new Claim(jsonProperty.Name, jsonProperty.Value.ToString(), ClaimValueTypes.Boolean);
                    break;
                case JsonValueKind.Null:
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

    public void AssertIsAdmin()
    {
        if (Role != UserRole.admin) throw new UnauthorizedAccessException();
    }

    public void AssertCanManageProject(Guid projectId)
    {
        if (!CanManageProject(projectId)) throw new UnauthorizedAccessException();
    }

    public void AssertCanAccessProject(string projectCode)
    {
        if (!CanAccessProject(projectCode)) throw new UnauthorizedAccessException();
    }

    public bool CanAccessProject(string projectCode)
    {
        return Role == UserRole.admin || Projects.Any(p => p.Code == projectCode);
    }

    public void AssertCanDeleteAccount(Guid userid)
    {
        if (Role != UserRole.admin && Id != userid) throw new UnauthorizedAccessException();
    }

    public void AssertCanManagerProjectMemberRole(Guid projectId, Guid userId)
    {
        AssertCanManageProject(projectId);
        if (Role != UserRole.admin && userId == Id)
            throw new UnauthorizedAccessException("Not allowed to change own project role.");
    }

    public bool HasProjectCreatePermission()
    {
        return CanCreateProjects ?? Role == UserRole.admin;
    }
}

public record AuthUserProject(string Code, ProjectRole Role, Guid ProjectId);

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UserRole
{
    admin = 0,
    user = 1
}
