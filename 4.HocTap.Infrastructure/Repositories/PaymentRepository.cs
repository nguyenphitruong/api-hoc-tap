using HocTap.Domain.Aggregates.Payments;
using HocTap.Infrastructure.Entities;
using HocTap.Infrastructure.MappingRepos;
using HocTap.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HocTap.Infrastructure.Repositories;

/// <summary>
/// 12/10/2026 - Repository đơn gia hạn (payment_order). Đọc AsNoTracking -> Model; ghi Model -> entity (Service gọi unitOfWork.Save()).
/// </summary>
public class PaymentRepository : IPaymentRepository
{
    private readonly HocTapDbContext dbContext;

    public PaymentRepository(HocTapDbContext i_context)
    {
        dbContext = i_context;
    }

    public void InsertOrder(PaymentOrderModel i_order)
    {
        dbContext.payment_order.Add(HocTapEntityMapper.ToEntity(i_order));
    }

    public void UpdateOrder(PaymentOrderModel i_order)
    {
        dbContext.payment_order.Update(HocTapEntityMapper.ToEntity(i_order));
    }

    public PaymentOrderModel? GetOrderById(Guid i_id)
    {
        payment_order? entity = dbContext.payment_order.AsNoTracking().FirstOrDefault(x => x.id == i_id);
        return entity == null ? null : HocTapEntityMapper.ToModel(entity);
    }

    public PaymentOrderModel? GetOrderByCode(string i_code)
    {
        payment_order? entity = dbContext.payment_order.AsNoTracking().FirstOrDefault(x => x.code == i_code);
        return entity == null ? null : HocTapEntityMapper.ToModel(entity);
    }

    public PaymentOrderModel? GetOrderByTxnRef(string i_gateway, string i_txnRef)
    {
        payment_order? entity = dbContext.payment_order.AsNoTracking().FirstOrDefault(x => x.gateway == i_gateway && x.txnref == i_txnRef);
        return entity == null ? null : HocTapEntityMapper.ToModel(entity);
    }

    public PaymentOrderModel? GetLastPendingOrder(Guid i_userId, string i_planCode)
    {
        payment_order? entity = dbContext.payment_order.AsNoTracking()
            .Where(x => x.userid == i_userId && x.plancode == i_planCode && x.status == Domain.Commons.HocTapConst.PAY_PENDING)
            .OrderByDescending(x => x.timecr)
            .FirstOrDefault();
        return entity == null ? null : HocTapEntityMapper.ToModel(entity);
    }

    public List<PaymentOrderRow> GetListOrder(string? i_status, int i_max)
    {
        IQueryable<payment_order> qOrder = dbContext.payment_order.AsNoTracking();
        if (!string.IsNullOrEmpty(i_status)) qOrder = qOrder.Where(x => x.status == i_status);
        var lstRow = (from o in qOrder
                      join u in dbContext.app_user.AsNoTracking() on o.userid equals u.id
                      orderby o.timecr descending
                      select new { order = o, u.fullname, u.email, u.phone })
                     .Take(i_max)
                     .ToList();
        List<PaymentOrderRow> lstResult = new List<PaymentOrderRow>();
        foreach (var row in lstRow)
        {
            PaymentOrderRow item = new PaymentOrderRow();
            item.Order = HocTapEntityMapper.ToModel(row.order);
            item.FullName = row.fullname;
            item.Email = row.email;
            item.Phone = row.phone;
            lstResult.Add(item);
        }
        return lstResult;
    }
}
