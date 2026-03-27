using MediatR;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Catalog;
using NamEcommerce.Domain.Shared.Services.Catalog;

namespace NamEcommerce.Application.Services.Events.Catalog;

public sealed class CategoryDeletedEventHandler : INotificationHandler<EntityDeletedNotification<Category>>
{
    private readonly ICategoryManager _categoryManager;
    private readonly IEntityDataReader<Category> _categoryDataReader;

    public CategoryDeletedEventHandler(ICategoryManager categoryManager, IEntityDataReader<Category> categoryDataReader)
    {
        _categoryManager = categoryManager;
        _categoryDataReader = categoryDataReader;
    }

    public async Task Handle(EntityDeletedNotification<Category> notification, CancellationToken cancellationToken)
    {
        var categories = await _categoryDataReader.GetAllAsync().ConfigureAwait(false);
        var childCategories = categories.Where(category => category.ParentId == notification.Entity.Id);
        if (!childCategories.Any())
            return;

        foreach (var category in childCategories)
        {
            await _categoryManager.UpdateCategoryAsync(new UpdateCategoryDto(category.Id)
            {
                Name = category.Name,
                ParentId = null,
                DisplayOrder = category.DisplayOrder,
            }).ConfigureAwait(false);
        }
    }
}
