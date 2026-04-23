using Blazored.LocalStorage;
using HospitalNoShow.BlazorClient.Auth;
using HospitalNoShow.BlazorClient.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using System.Globalization;

// ── Kültür Ayarı ─────────────────────────────────────────────────────────────
// MudBlazor grafik bileşenleri SVG üretirken thread culture'ı kullanır.
// Türkçe locale'de ondalık ayırıcı virgül (",") olduğundan SVG geçersiz olur.
// CultureInfo.InvariantCulture → nokta tabanlı sayı formatı kullanılır.
// NOT: CultureInfo("tr-TR") ile açık Türkçe format hâlâ çalışır.
CultureInfo.DefaultThreadCurrentCulture   = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<HospitalNoShow.BlazorClient.App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// API Base URL - appsettings.json veya ortam değişkeninden okunur
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7001/";
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });

// MudBlazor
builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = MudBlazor.Defaults.Classes.Position.BottomRight;
    config.SnackbarConfiguration.PreventDuplicates = false;
    config.SnackbarConfiguration.NewestOnTop = true;
    config.SnackbarConfiguration.ShowCloseIcon = true;
    config.SnackbarConfiguration.VisibleStateDuration = 4000;
    config.SnackbarConfiguration.MaxDisplayedSnackbars = 3;
});

// LocalStorage (JWT token saklama)
builder.Services.AddBlazoredLocalStorage();

// Authentication
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<JwtAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(
    sp => sp.GetRequiredService<JwtAuthStateProvider>());

// API Services
builder.Services.AddScoped<ApiService>();
builder.Services.AddScoped<AuthApiService>();
builder.Services.AddScoped<AppointmentApiService>();
builder.Services.AddScoped<PatientApiService>();
builder.Services.AddScoped<DoctorApiService>();
builder.Services.AddScoped<NoShowApiService>();

await builder.Build().RunAsync();
