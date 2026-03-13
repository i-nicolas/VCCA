using Domain.Entities.Transaction.Common;

namespace Domain.Extensions;

public static class DomainAppDocNumsVOExtensions
{
    public static T TripSchedNo<T>(this T entity, string? tripSchedNo) where T : TransactionalDocumentDEM
    {

        return entity;
    }
}
