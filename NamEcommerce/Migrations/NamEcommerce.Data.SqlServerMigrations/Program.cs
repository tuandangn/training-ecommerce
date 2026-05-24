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
//"NamEcommerceEfDbContext": "Data Source=.\\SQLEXPRESS;Database=NamEcommerceDb;Integrated Security=True;Persist Security Info=False;Pooling=False;MultipleActiveResultSets=False;TrustServerCertificate=True;"
//Server=34.142.218.73,1433;Database=NamEcommerce;User Id=sa;Password=xxx;TrustServerCertificate=True;MultipleActiveResultSets=True

//Add-Migration AddPasswordSalt -Project NamEcommerce.Data.SqlServerMigrations -StartupProject NamEcommerce.Data.SqlServerMigrations -Context NamEcommerce.Data.SqlServer.NamEcommerceEfDbContext
//Update-Database -Project NamEcommerce.Data.SqlServerMigrations -StartupProject NamEcommerce.Data.SqlServerMigrations -Context NamEcommerce.Data.SqlServer.NamEcommerceEfDbContext
