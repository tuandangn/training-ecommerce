using Microsoft.EntityFrameworkCore;
using NamEcommerce.Data.SqlServer;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages();
builder.Services.AddDbContext<NamEcommerceEfDbContext>(opts =>
    opts.UseSqlServer(builder.Configuration.GetConnectionString(nameof(NamEcommerceEfDbContext)),
        opt => opt.MigrationsAssembly(Assembly.GetExecutingAssembly().GetName().Name))
);


var app = builder.Build();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.Run();
