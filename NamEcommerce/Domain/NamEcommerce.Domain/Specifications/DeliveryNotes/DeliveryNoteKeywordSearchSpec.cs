using NamEcommerce.Domain.Entities.DeliveryNotes;
using NamEcommerce.Domain.Shared.Specifications;
using NamEcommerce.Domain.Specifications.Filters;

namespace NamEcommerce.Domain.Specifications.DeliveryNotes;

[Serializable]
public sealed class DeliveryNoteKeywordSearchSpec(KeywordFilter filter) : BaseSpecification<DeliveryNote>(
    deliveryNote => deliveryNote.Code.Contains(filter.Keywords) ||
                (deliveryNote.OrderCode != null && deliveryNote.OrderCode.Contains(filter.Keywords)) ||
                deliveryNote.CustomerInfo.FullName.Value.ToUpper().Contains(filter.UppercaseKeywords) ||
                deliveryNote.CustomerInfo.FullName.Value.ToUpper().Contains(filter.NormalizedKeywords) ||
                deliveryNote.CustomerInfo.FullName.NormalizedValue.Contains(filter.NormalizedKeywords) ||
                (deliveryNote.CustomerInfo.PhoneNumber != null && deliveryNote.CustomerInfo.PhoneNumber.Contains(filter.Keywords)));
