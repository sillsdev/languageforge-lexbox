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
    // from testing done in November 2023, we started getting errors at 10,200 chars. See HeaderTests.CheckCloudflareHeaderSizeLimit.
    public const int MaxJwtLength = 9000;
    public const int MaxProjectCount = 170;
    public static readonly JsonTypeInfo LexAuthUserTypeInfo =
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
                        case ClaimValueTypes.Integer:
                        case ClaimValueTypes.Integer32:
                        case ClaimValueTypes.Integer64:
                            jsonObject.Add(claim.Type, JsonValue.Create(int.Parse(claim.Value)));
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
        UpdatedDate = user.UpdatedDate.ToUnixTimeSeconds();
        Projects = user.IsAdmin
            ? Array.Empty<AuthUserProject>() // admins have access to all projects, so we don't include them to prevent going over the jwt limit
            : user.Projects.Select(p => new AuthUserProject(p.Role, p.ProjectId)).ToArray();
        EmailVerificationRequired = user.EmailVerified ? null : true;
        CanCreateProjects = user.CanCreateProjects ? true : null;
        Locale = user.LocalizationCode;
        Locked = user.Locked ? null : true;
    }

    [JsonPropertyName(LexAuthConstants.IdClaimType)]
    public required Guid Id { get; set; }

    [JsonPropertyName(LexAuthConstants.UpdatedDateClaimType)]
    public long UpdatedDate { get; set; }
    [JsonPropertyName(LexAuthConstants.AudienceClaimType)]
    public LexboxAudience Audience { get; set; } = LexboxAudience.LexboxApi;

    [JsonPropertyName(LexAuthConstants.EmailClaimType)]
    public required string Email { get; set; }

    [JsonPropertyName(LexAuthConstants.NameClaimType)]
    public required string Name { get; set; }

    [JsonPropertyName(LexAuthConstants.RoleClaimType)]
    public required UserRole Role { get; set; }

    [JsonIgnore]
    public bool IsAdmin => Role == UserRole.admin;

    [JsonIgnore]
    public AuthUserProject[] Projects { get; set; } = Array.Empty<AuthUserProject>();

    [JsonPropertyName(LexAuthConstants.ProjectsClaimType)]
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
                    return $"{projectRole}:{projectString}";
                }));
        set
        {
            //will be empty for admins
            if (string.IsNullOrEmpty(value))
            {
                Projects = Array.Empty<AuthUserProject>();
                return;
            }
            Projects = value.Split(",").SelectMany(p =>
            {
                if (string.IsNullOrEmpty(p)) return Array.Empty<AuthUserProject>();
                var role = p[0] switch
                {
                    'm' => ProjectRole.Manager,
                    'e' => ProjectRole.Editor,
                    _ => ProjectRole.Unknown
                };
                return p[2..].Split("|").Select(Guid.Parse).Select(pId => new AuthUserProject(role, pId));
            }).ToArray();
        }
    }

    [JsonPropertyName(LexAuthConstants.IsLockedClaimType)]
    public bool? Locked { get; init; }
    [JsonPropertyName(LexAuthConstants.EmailUnverifiedClaimType)]
    public bool? EmailVerificationRequired { get; init; }

    [JsonPropertyName(LexAuthConstants.CanCreateProjectClaimType)]
    public bool? CanCreateProjects { get; init; }

    [JsonPropertyName(LexAuthConstants.LocaleClaimType)]
    public string Locale { get; init; }

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
                case JsonValueKind.Number:
                    yield return new Claim(jsonProperty.Name, jsonProperty.Value.ToString(), ClaimValueTypes.Integer);
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
}

public record AuthUserProject(ProjectRole Role, Guid ProjectId);

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UserRole
{
    admin = 0,
    user = 1
}
