using GraphQL;
using Microsoft.EntityFrameworkCore;
using NamEcommerce.Api.GraphQl.Schemes;
using NamEcommerce.Api.GraphQl.Schemes.Catalog;
using NamEcommerce.Api.GraphQl.Schemes.Catalog.Categories;
using NamEcommerce.Application.Services.Queries.Handlers.Catalog;
using NamEcommerce.Application.Shared.Queries.Models.Catalog;
using NamEcommerce.Data.Contracts;
using NamEcommerce.Data.SqlServer;
using NamEcommerce.Domain.Services.Catalog;
using NamEcommerce.Domain.Shared.Services;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);


//services
builder.Services.AddAuthorization();
builder.Services.AddDbContext<NamEcommerceEfDbContext>(opts =>
    opts.UseSqlServer(builder.Configuration.GetConnectionString(nameof(NamEcommerceEfDbContext)),
        opt => opt.MigrationsAssembly(Assembly.GetExecutingAssembly().GetName().Name))
);
builder.Services.AddScoped<IDbContext, NamEcommerceEfDbContext>();
builder.Services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
builder.Services.AddScoped<ICategoryManager, CategoryManager>();
builder.Services.AddMediatR(config =>
{
    config.RegisterServicesFromAssemblyContaining<GetAllCategories>();
    config.RegisterServicesFromAssemblyContaining<GetAllCategoriesHandler>();
});

builder.Services.AddGraphQL(opts =>
{
    opts.AddSystemTextJson();
});
builder.Services.AddSingleton<NamEcommerceSchema>();
builder.Services.AddSingleton<NamEcommerceQuery>();
builder.Services.AddSingleton<CatalogQuery>();
builder.Services.AddSingleton<CategoryQuery>();
builder.Services.AddSingleton<CategoryType>();


//pipeline
var app = builder.Build();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseRouting();

app.UseGraphQL<NamEcommerceSchema>();
app.UseGraphQLGraphiQL();

//start
app.Run();