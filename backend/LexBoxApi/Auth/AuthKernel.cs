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
                builder => builder.RequireAssertion(context => context.User.IsInRole(UserRole.admin.ToString())));
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
                        if ((context.Request.Headers.ContainsKey("Authorization") &&
                            context.Request.Headers.Authorization.ToString().StartsWith("Bearer")) ||
                            context.Request.Query.ContainsKey("jwt"))
                        {
                            return JwtBearerDefaults.AuthenticationScheme;
                        }

                        return CookieAuthenticationDefaults.AuthenticationScheme;
                    };
                })
            .AddCookie(options =>
            {
                configuration.Bind("Authentication:Cookie", options);
                options.LoginPath = "/api/login";
                options.Cookie.Name = ".LexBoxAuth";
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
                        if (context.Request.Query.TryGetValue("jwt", out var jwt))
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
}