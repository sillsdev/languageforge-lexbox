using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using WebApi;
using WebApi.Auth;
using WebApi.Config;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ProxyAuthService>();
builder.Services.AddScoped<IAuthorizationHandler, BasicAuthHandler>();
builder.Services.AddOptions<LexBoxApiConfig>()
    .BindConfiguration("LexBoxApi")
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddAuthentication("Default")
    .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>("Default", null);
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("HgAuth",
        policyBuilder =>
        {
            policyBuilder.AddRequirements(new RequiresLexBoxBasicAuth());
        });

var app = builder.Build();

app.UseHttpLogging();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();

app.MapControllers();
app.MapReverseProxy();
app.Run();