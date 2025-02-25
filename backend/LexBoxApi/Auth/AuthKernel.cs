using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using LexBoxApi.Auth.Attributes;
using LexBoxApi.Auth.Requirements;
using LexBoxApi.Controllers;
using LexCore.Auth;
using LexData;
using LexSyncReverseProxy;
using LexSyncReverseProxy.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Logging;
using Microsoft.OpenApi.Models;
using OpenIddict.Core;
using OpenIddict.Server;
using OpenIddict.Server.AspNetCore;
using OpenIddict.Validation;
using OpenIddict.Validation.AspNetCore;

namespace LexBoxApi.Auth;

public static class AuthKernel
{
    public const string DefaultScheme = "JwtOrCookie";
    public const string JwtOverBasicAuthUsername = "bearer";
    public const string AuthCookieName = LexAuthConstants.AuthCookieName;

    public static void AddLexBoxAuth(IServiceCollection services,
        IConfigurationRoot configuration,
        IWebHostEnvironment environment)
    {
        if (environment.IsDevelopment())
        {
            IdentityModelEventSource.ShowPII = true;
            IdentityModelEventSource.LogCompleteSecurityArtifact = true;
        }

        services.AddScoped<LexAuthService>();
        services.AddSingleton<IAuthorizationHandler, ValidateUserUpdatedHandler>();
        services.AddSingleton<IAuthorizationHandler, FeatureFlagRequirementHandler>();
        services.AddSingleton<IAuthorizationHandler, ScopeRequirementHandler>();
        services.AddAuthorization(options =>
        {
            //fallback policy is used when there's no auth attribute.
            //default policy is when there's no parameters specified on the auth attribute
            //this will make sure that all endpoints require auth unless they have the AllowAnonymous attribute
            options.FallbackPolicy = options.DefaultPolicy = new AuthorizationPolicyBuilder()
                .RequireDefaultLexboxAuth()
                .Build();

            //don't use RequireDefaultLexboxAuth here because that only allows the default audience
            options.AddPolicy(AllowAnyAudienceAttribute.PolicyName, builder => builder.RequireAuthenticatedUser());
            //we still need this policy, without it the default policy is used which requires the default audience
            options.AddPolicy(RequireScopeAttribute.PolicyName, builder => builder.RequireAuthenticatedUser());
            options.AddPolicy(ProxyKernel.UserHasAccessToProjectPolicy,
                policyBuilder =>
                {
                    policyBuilder.RequireAuthenticatedUser()
                        .AddRequirements(
                            new UserHasAccessToProjectRequirement(),
                            new RequireScopeAttribute(LexboxAuthScope.LexboxApi, LexboxAuthScope.SendAndReceive)
                        );
                });

            options.AddPolicy(AdminRequiredAttribute.PolicyName,
                builder => builder.RequireDefaultLexboxAuth()
                    .RequireAssertion(context => context.User.IsInRole(UserRole.admin.ToString())));
            options.AddPolicy(VerifiedEmailRequiredAttribute.PolicyName,
                builder => builder.RequireDefaultLexboxAuth()
                    .RequireAssertion(context => !context.User.HasClaim(LexAuthConstants.EmailUnverifiedClaimType, "true")));

            //user can create a project if they have the claim or are an admin
            options.AddPolicy(CreateProjectRequiredAttribute.PolicyName,
                builder => builder.RequireDefaultLexboxAuth()
                    .RequireAssertion(context =>
                        context.User.HasClaim(LexAuthConstants.CanCreateProjectClaimType, "true") ||
                        context.User.IsInRole(UserRole.admin.ToString())
                    ));
        });
        services.AddOptions<JwtOptions>()
            .BindConfiguration("Authentication:Jwt")
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddOptions<OpenIdOptions>()
            .BindConfiguration("Authentication:OpenId");
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
                        if (context.Request.Headers.ContainsKey("Authorization") &&
                            context.Request.Headers.Authorization.ToString().StartsWith("Bearer") &&
                            context.RequestServices.GetService<IOptions<OpenIdOptions>>()?.Value.Enable == true)
                        {
                            //todo this breaks CanUseBearerAuth test
                            //fow now this will use oauth
                            return OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
                        }

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
                options.LoginPath = "/login";
                options.Cookie.Name = AuthCookieName;
                options.ForwardForbid = JwtBearerDefaults.AuthenticationScheme;
                options.Events = new()
                {
                    OnRedirectToLogin = context =>
                    {
                        if (context.Request.Path.StartsWithSegments("/api/oauth") &&
                            context.Response.StatusCode == StatusCodes.Status200OK)
                        {
                            context.Response.Redirect(context.RedirectUri);
                        }
                        else
                        {
                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        }
                        return Task.CompletedTask;
                    }
                };
            })
            .AddJwtBearer(options =>
            {
                var jwtOptions = configuration.GetSection("Authentication:Jwt").Get<JwtOptions>();
                ArgumentNullException.ThrowIfNull(jwtOptions);
                if (jwtOptions.Secret == "==== replace ====")
                {
                    throw new ArgumentException("default jwt secret value used, please specify non default value");
                }

                options.Audience = LexboxAudience.LexboxApi.ToString();
                options.ClaimsIssuer = LexboxAudience.LexboxApi.ToString();
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
            }).AddGoogle(googleOptions =>
            {
                var googleConfig = configuration.GetSection("Authentication:Google").Get<GoogleOptions>();
                // ArgumentNullException.ThrowIfNull(googleOptions); // Eventually we'll throw if google config not found
                if (googleConfig is not null)
                {
                    googleOptions.ClientId = googleConfig.ClientId;
                    googleOptions.ClientSecret = googleConfig.ClientSecret;
                }

                googleOptions.CallbackPath = "/api/login/signin-google";
                googleOptions.Events.OnTicketReceived = async context =>
                {
                    context.HandleResponse();
                    var loginController = context.HttpContext.RequestServices.GetRequiredService<LoginController>();
                    loginController.ControllerContext.HttpContext = context.HttpContext;
                    //using context.ReturnUri and not context.Properties.RedirectUri because the latter is null
                    var redirectTo = await loginController.CompleteGoogleLogin(context.Principal, context.ReturnUri);
                    context.HttpContext.Response.Redirect(redirectTo);
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

        var openIdOptions = configuration.GetSection("Authentication:OpenId").Get<OpenIdOptions>();
        if (openIdOptions?.Enable == true) AddOpenId(services, environment);
        services.AddOptions<AuthenticationOptions>().ValidateOnStart();
    }

    private static void AddOpenId(IServiceCollection services, IWebHostEnvironment environment)
    {
        services.Add(ScopeRequestFixer.Descriptor.ServiceDescriptor);
        //openid server
        services.AddOpenIddict()
            .AddCore(options =>
            {
                options.UseEntityFrameworkCore().UseDbContext<LexBoxDbContext>();
                options.UseQuartz();
            })
            .AddServer(options =>
            {
                options.RegisterScopes("openid", "profile", "email");
                options.RegisterScopes([..Enum.GetNames<LexboxAuthScope>().Select(s => s.ToLower())]);
                //todo add application claims
                options.RegisterClaims("aud", "email", "exp", "iss", "iat", "sub", "name");
                options.SetAuthorizationEndpointUris("api/oauth/open-id-auth");
                options.SetTokenEndpointUris("api/oauth/token");
                options.SetIntrospectionEndpointUris("api/oauth/introspect");
                options.SetUserinfoEndpointUris("api/oauth/userinfo");
                options.Configure(serverOptions => serverOptions.Handlers.Add(ScopeRequestFixer.Descriptor));

                options.SetAccessTokenLifetime(TimeSpan.FromHours(1));
                options.SetRefreshTokenLifetime(TimeSpan.FromDays(14));
                options.AllowAuthorizationCodeFlow()
                    .AllowRefreshTokenFlow();

                options.RequireProofKeyForCodeExchange();//best practice to use PKCE with auth code flow and no implicit flow

                if (environment.IsDevelopment())
                {
                    options.AddDevelopmentEncryptionCertificate();
                    options.AddDevelopmentSigningCertificate();
                }
                else
                {
                    //see docs: https://documentation.openiddict.com/configuration/encryption-and-signing-credentials.html
                    //todo, handle certificate rotation, right now these certs will be replaced 15 days before they expire
                    //however we need to start signing with the new cert before the old one expires
                    //this means we need 2 certs for each use case, an old one for tokens that have not expired yet, and a new one to sign all new tokens
                    var encryptionCert = X509Certificate2.CreateFromPemFile(
                        "/oauth-certs/encryption/tls.crt",
                        "/oauth-certs/encryption/tls.key");
                    options.AddEncryptionCertificate(encryptionCert);

                    var signingCert = X509Certificate2.CreateFromPemFile(
                        "/oauth-certs/signing/tls.crt",
                        "/oauth-certs/signing/tls.key");

                    options.AddSigningCertificate(signingCert);
                }

                var aspnetCoreBuilder = options.UseAspNetCore()
                    .EnableAuthorizationEndpointPassthrough()
                    .EnableTokenEndpointPassthrough();
                if (environment.IsDevelopment())
                {
                    aspnetCoreBuilder.DisableTransportSecurityRequirement();
                }
            })
            .AddValidation(options =>
            {
                options.UseLocalServer();
                options.UseAspNetCore();
                options.AddAudiences(Enum.GetValues<LexboxAudience>().Where(a => a != LexboxAudience.Unknown).Select(a => a.ToString()).ToArray());
                options.EnableAuthorizationEntryValidation();
            });
        //ensure that validation happens on startup, not on the first request which requires authentication
        services.AddOptions<OpenIddictCoreOptions>().ValidateOnStart();
        services.AddOptions<OpenIddictServerOptions>().ValidateOnStart();
        services.AddOptions<OpenIddictValidationOptions>().ValidateOnStart();
        services.AddOptions<OpenIddictServerAspNetCoreOptions>().ValidateOnStart();
    }

    public static AuthorizationPolicyBuilder RequireDefaultLexboxAuth(this AuthorizationPolicyBuilder builder)
    {
        return builder.RequireAuthenticatedUser()
            .AddRequirements(new RequireScopeAttribute(LexboxAuthScope.LexboxApi, true));
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
