namespace NamEcommerce.Web.Models.DesignSystem;

public sealed record DesignActionModel(
    string Text,
    string Url,
    string Icon = "",
    string Variant = "outline-secondary",
    string Title = "");

public sealed record PageHeaderModel(
    string Title,
    string Subtitle = "",
    IReadOnlyList<DesignBreadcrumbModel>? Breadcrumbs = null,
    IReadOnlyList<DesignActionModel>? Actions = null);

public sealed record DesignBreadcrumbModel(string Text, string Url = "");

public sealed record FilterToolbarModel(
    string Action,
    string SearchName,
    string SearchPlaceholder,
    string SearchValue = "",
    string Method = "get",
    IReadOnlyList<FilterSelectModel>? Filters = null,
    IReadOnlyList<DesignActionModel>? Actions = null);

public sealed record FilterSelectModel(
    string Name,
    string Label,
    string Value,
    IReadOnlyList<SelectOptionModel> Options);

public sealed record SelectOptionModel(string Text, string Value);

public sealed record DataTableModel(
    IReadOnlyList<DataTableColumnModel> Columns,
    IReadOnlyList<IReadOnlyList<string>> Rows,
    EmptyStateModel EmptyState,
    string CssClass = "");

public sealed record DataTableColumnModel(
    string Text,
    string Align = "",
    string Width = "");

public sealed record StatusBadgeModel(
    string Text,
    string Tone = "muted",
    string Icon = "");

public sealed record FormSectionModel(
    string Title,
    string Description,
    IReadOnlyList<FormRowModel> Rows);

public sealed record FormRowModel(
    string Label,
    string ControlHtml,
    string HelpText = "",
    bool Required = false);

public sealed record EmptyStateModel(
    string Title,
    string Message,
    string Icon = "bi-inbox",
    DesignActionModel? Action = null);

public sealed record ConfirmModalModel(
    string Id,
    string Title,
    string Message,
    string ConfirmText,
    string CancelText = "Huỷ",
    string Tone = "danger");

public sealed record NumberDisplayModel(
    decimal Value,
    string Suffix = "",
    string Label = "",
    string CssClass = "");
