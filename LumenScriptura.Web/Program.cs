using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using LumenScriptura.Services;
using LumenScriptura.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<LumenScriptura.Web.App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient 
{ 
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) 
});

builder.Services.AddScoped<IBibleService, WebBibleService>();
builder.Services.AddScoped<IUserDbService, WebUserDbService>();
builder.Services.AddScoped<AppStateService>();
builder.Services.AddScoped<NoteParserService>();

await builder.Build().RunAsync();
