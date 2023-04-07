#region Namespaces
using GraphQL;
using GraphQL.DataLoader;
using GraphQL.Server.Ui.GraphiQL;
using GraphQL.Types;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.EntityFrameworkCore;
using NamEcommerce.Api.GraphQl.DataLoaders;
using NamEcommerce.Api.GraphQl.Schemes;
using NamEcommerce.Api.GraphQl.Schemes.Catalog;
using NamEcommerce.Application.Services.StubData;
using NamEcommerce.Data.Contracts;
using NamEcommerce.Data.SqlServer;
using NamEcommerce.Domain.Services.Catalog;
using NamEcommerce.Domain.Services.Common;
using NamEcommerce.Domain.Shared.Services;
using System.Reflection;
#endregion

var builder = WebApplication.CreateBuilder(args);

#region Services
{
    builder.Services.AddAuthorization();
    builder.Services.AddCors(opts =>
    {
        opts.AddPolicy("Default", builder => 
            builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()
        );
    });

    builder.Services.AddDbContext<NamEcommerceEfDbContext>(opts =>
        opts.UseSqlServer(builder.Configuration.GetConnectionString(nameof(NamEcommerceEfDbContext)),
            opt => opt.MigrationsAssembly(Assembly.GetExecutingAssembly().GetName().Name))
    );
    builder.Services.AddScoped<IDbContext, NamEcommerceEfDbContext>();
    builder.Services.AddScoped(typeof(IRepository<>), typeof(NamEcommerceEfRepository<>));

    builder.Services.AddScoped(typeof(IEntityDataReader<>), typeof(EntityDataReader<>));
    builder.Services.AddScoped<ICategoryManager, CategoryManager>();

    builder.Services.AddTransient<ICategoryDataLoader, CategoryDataLoader>();

    builder.Services.AddGraphQL(opts =>
    {
        opts.AddSchema<NamEcommerceSchema>()
            .AddSchema<CatalogSchema>()
            .AddSystemTextJson()
            .AddGraphTypes()
            .AddDataLoader();
    });
    builder.Services.AddMediatR(config =>
    {
        config.RegisterServicesFromAssemblyContaining<StubDataGetAllCategoriesHandler>();
    });
}
#endregion

#region Pipeline
var app = builder.Build();
{
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error");
        app.UseHsts();
    }
    app.UseHttpsRedirection();

    app.UseCors("Default");
    app.UseRouting();

    app.Map("/catalog", _ =>
    {
        _.UseGraphQL<CatalogSchema>();
        _.UseGraphQLGraphiQL(options: new GraphiQLOptions
        {
            GraphQLEndPoint = "/catalog/graphql"
        });
    });
    app.UseGraphQL<NamEcommerceSchema>();
    app.UseGraphQLGraphiQL();
}
#endregion

app.Run();