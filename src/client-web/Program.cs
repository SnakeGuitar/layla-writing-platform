using client_web.Components;
using client_web.Services;
using client_web.Services.Http;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
var apiAccesoUrl = builder.Configuration["ApiUrls:Acceso"]
?? throw new InvalidOperationException("Falta la configuración ApiUrls:Acceso");
builder.Services.AddHttpClient<ApiClient>((sp, client) =>
{
    client.BaseAddress = new Uri(apiAccesoUrl);
}).AddTypedClient((httpClient, sp) =>
    new ApiClient(httpClient, apiAccesoUrl)
);
builder.Services.AddSingleton<HttpContextAccessor>();
builder.Services.AddScoped<AuthService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
