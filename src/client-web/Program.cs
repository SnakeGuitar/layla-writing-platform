using client_web.Config;
using client_web.UI;

WebApplicationBuilder? builder = WebApplication.CreateBuilder(args);


if (builder.Environment.IsProduction())
{
    Secrets.Configure(builder);
}
HttpClientConfig.Configure(builder.Services, builder);
Builder.Configure(builder.Services, builder);
Services.Configure(builder.Services);


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.MapHealthChecks("/health");
app.UseHttpsRedirection();
app.UseStaticFiles();
// Authentication / authorization middleware must run before Antiforgery and
// before MapRazorComponents so the AuthorizationMiddlewareResultHandler has
// IAuthenticationService available when challenging unauthenticated requests.
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
