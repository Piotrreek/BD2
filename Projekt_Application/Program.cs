using Projekt.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProjectServices(builder.Configuration);
#if DEBUG
builder.Services.AddRazorPages().AddRazorRuntimeCompilation();
#endif

var app = builder.Build();

app.UseRouting();
app.UseStaticFiles();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}");

app.Run();