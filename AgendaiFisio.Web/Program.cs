var builder = WebApplication.CreateBuilder(args);

// Projeto reservado para a interface ASP.NET Core MVC com views Razor.
// Controllers, views e regras de navegação serão adicionados em uma etapa futura.
builder.Services.AddControllersWithViews();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
