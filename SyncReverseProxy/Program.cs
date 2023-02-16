using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.Net.Http.Headers;
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
builder.Services.AddHttpLogging(options =>
{
    options.LoggingFields = HttpLoggingFields.RequestPropertiesAndHeaders |
                            HttpLoggingFields.ResponsePropertiesAndHeaders | 
                            HttpLoggingFields.RequestBody |
                            HttpLoggingFields.ResponseBody;
    options.MediaTypeOptions.AddText("text/html");
    options.RequestHeaders.Add(HeaderNames.Authorization);
    options.ResponseHeaders.Add(HeaderNames.Authorization);
    options.RequestHeaders.Add(HeaderNames.WWWAuthenticate);
    options.ResponseHeaders.Add(HeaderNames.WWWAuthenticate);
});
builder.Services.AddScoped<ProxyAuthService>();
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
            policyBuilder.RequireAuthenticatedUser();
        });

var app = builder.Build();

app.UseHttpLogging();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();

app.MapControllers();
app.MapReverseProxy();
app.Run();