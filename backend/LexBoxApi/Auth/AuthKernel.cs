using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Text;
using LexCore.Auth;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Logging;
using Microsoft.OpenApi.Models;

namespace LexBoxApi.Auth;

public static class AuthKernel
{
    public const string DefaultScheme = "JwtOrCookie";
    public const string JwtOverBasicAuthUsername = "bearer";
    public const string AuthCookieName = ".LexBoxAuth";

    public static void AddLexBoxAuth(IServiceCollection services,
        IConfigurationRoot configuration,
        IWebHostEnvironment environment)
    {
        if (environment.IsDevelopment())
        {
            IdentityModelEventSource.ShowPII = true;
        }

        services.AddScoped<LexAuthService>();
        services.AddAuthorization(options =>
        {
            //fallback policy is used when there's no auth attribute.
            //default policy is when there's no parameters specified on the auth attribute
            //this will make sure that all endpoints require auth unless they have the AllowAnonymous attribute
            options.FallbackPolicy = options.DefaultPolicy;
            options.AddPolicy(AdminRequiredAttribute.PolicyName,
                builder => builder.RequireAuthenticatedUser()
                    .RequireAssertion(context => context.User.IsInRole(UserRole.admin.ToString())));
            options.AddPolicy(VerifiedEmailRequiredAttribute.PolicyName,
                builder => builder.RequireAuthenticatedUser()
                    .RequireAssertion(context => !context.User.HasClaim(LexAuthConstants.EmailUnverifiedClaimType, "true")));

            //user can create a project if they have the claim or are an admin
            options.AddPolicy(CreateProjectRequiredAttribute.PolicyName,
                builder => builder.RequireAuthenticatedUser()
                    .RequireAssertion(context =>
                        context.User.HasClaim(LexAuthConstants.CanCreateProjectClaimType, "true") ||
                        context.User.IsInRole(UserRole.admin.ToString())
                    ));
        });
        services.AddOptions<JwtOptions>()
            .BindConfiguration("Authentication:Jwt")
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddAuthentication(options =>
            {
                options.DefaultScheme = DefaultScheme;
                options.DefaultChallengeScheme = DefaultScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddPolicyScheme(DefaultScheme,
                "Jwt or cookie",
                options =>
                {
                    options.ForwardDefaultSelector = context =>
                    {

                        if (context.Request.IsJwtRequest())
                        {
                            return JwtBearerDefaults.AuthenticationScheme;
                        }

                        //jwtOverBasic auth can be performance intensive so we want to avoid it if possible
                        if (context.Request.Cookies.ContainsKey(AuthCookieName))
                        {
                            return CookieAuthenticationDefaults.AuthenticationScheme;
                        }
                        if (context.Request.IsJwtOverBasicAuth(out var jwt))
                        {
                            context.Features.Set(new JwtOverBasicAuthFeature(jwt));
                            return JwtBearerDefaults.AuthenticationScheme;
                        }

                        return CookieAuthenticationDefaults.AuthenticationScheme;
                    };
                })
            .AddCookie(options =>
            {
                configuration.Bind("Authentication:Cookie", options);
                options.LoginPath = "/api/login";
                options.Cookie.Name = AuthCookieName;
                options.ForwardChallenge = JwtBearerDefaults.AuthenticationScheme;
                options.ForwardForbid = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                var jwtOptions = configuration.GetSection("Authentication:Jwt").Get<JwtOptions>();
                ArgumentNullException.ThrowIfNull(jwtOptions);
                if (jwtOptions.Secret == "==== replace ====")
                {
                    throw new ArgumentException("default jwt secret value used, please specify non default value");
                }

                options.Audience = jwtOptions.Audience;
                options.ClaimsIssuer = jwtOptions.Issuer;
                options.IncludeErrorDetails = true;
                options.TokenValidationParameters = LexAuthService.TokenValidationParameters(jwtOptions);
                options.MapInboundClaims = false;
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var authFeature = context.HttpContext.Features.Get<JwtOverBasicAuthFeature>();
                        if (authFeature is not null)
                        {
                            context.Token = authFeature.Jwt;
                        }
                        else if (context.Request.Query.TryGetValue("jwt", out var jwt))
                        {
                            context.Token = jwt;
                        }

                        return Task.CompletedTask;
                    }
                };
            });
        services.AddSingleton<JwtTicketDataFormat>();
        //configure cooke auth to use jwt as the ticket format, aka the cookie will be a jwt
        new OptionsBuilder<CookieAuthenticationOptions>(services, CookieAuthenticationDefaults.AuthenticationScheme)
            .Configure<JwtTicketDataFormat>((options, format) => options.TicketDataFormat = format);

        services.ConfigureSwaggerGen(options =>
        {
            options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme,
                new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Description = "Enter the token from login, prefixed like this `Bearer {token}`",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Name = "Bearer",
                        In = ParameterLocation.Header,
                        Reference = new OpenApiReference { Id = "Bearer", Type = ReferenceType.SecurityScheme }
                    },
                    new List<string>()
                }
            });
        });
    }

    public static bool IsJwtRequest(this HttpRequest request)
    {
        return (request.Headers.ContainsKey("Authorization") &&
                request.Headers.Authorization.ToString().StartsWith("Bearer")) ||
               request.Query.ContainsKey("jwt");
    }

    public static bool IsJwtOverBasicAuth(this HttpRequest request, [MaybeNullWhen(false)] out string jwt)
    {
        jwt = null;
        if (!request.Headers.TryGetValue("Authorization", out var authHeader)) return false;
        if (!AuthenticationHeaderValue.TryParse(authHeader.ToString(), out var header))
            return false;
        if (!header.Scheme.Equals("basic", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(header.Parameter)) return false;
        var basicAuth = Encoding.UTF8.GetString(Convert.FromBase64String(header.Parameter));
        var (username, password) = basicAuth.Split(':') switch
        {
            ["", ""] => (null, null),
            [var u, var p] => (u, p),
            _ => (null, null)
        };
        if (username is null || password is null) return false;
        if (!username.Equals(JwtOverBasicAuthUsername, StringComparison.OrdinalIgnoreCase)) return false;
        jwt = password;
        return true;
    }

    public record JwtOverBasicAuthFeature(string Jwt);
}
