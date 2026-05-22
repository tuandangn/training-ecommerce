namespace NamEcommerce.Domain.Shared.Exceptions.Inventory;

[Serializable]
public sealed class InvalidInventoryCostingOperationException(string errorCode, params object[] parameters)
    : NamEcommerceDomainException(errorCode, parameters);

[Serializable]
public sealed class InventoryCostingPolicyNotFoundException()
    : NamEcommerceDomainException("Error.InventoryCosting.PolicyNotFound");

[Serializable]
public sealed class UnsupportedInventoryCostingMethodException(params object[] parameters)
    : NamEcommerceDomainException("Error.InventoryCosting.UnsupportedCostingMethod", parameters);
