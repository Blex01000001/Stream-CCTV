using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using StreamCCTV;
using StreamCCTV.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient());
builder.Services.AddScoped<StorageService>();
builder.Services.AddScoped<PlayerService>();
builder.Services.AddScoped<Loc>();
builder.Services.AddScoped<WallState>();
// 之後換成後端代理實作時，只要改這一行
builder.Services.AddScoped<IHolodexService, HolodexService>();

await builder.Build().RunAsync();
