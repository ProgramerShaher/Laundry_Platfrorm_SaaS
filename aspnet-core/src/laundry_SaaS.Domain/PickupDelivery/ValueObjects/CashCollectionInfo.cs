using System;
using System.Collections.Generic;
using Volo.Abp.Domain.Values;

namespace laundry_SaaS.PickupDelivery;

/// <summary>
/// كائن قيمة (Value Object) يوثق تفاصيل التحصيل المالي النقدي عند الاستلام (Cash On Delivery - COD) لمهمة التوصيل.
/// <para>
/// لا يمثل هذا الكيان وحدة مدفوعات (Not a Payment Entity)، ولا يملك معرّفاً مستقلاً (No Id) أو مستودعاً خاصاً (No Repository)،
/// بل هو كائن قيمة مدمج (Owned Type) داخل مهمة التوصيل (<see cref="DeliveryTask"/>) لتوثيق استلام المندوب للمبلغ وسداد العميل.
/// </para>
/// </summary>
public class CashCollectionInfo : ValueObject
{
    /// <summary>
    /// المبلغ المالي المطلوب تحصيله نقداً من العميل عند باب المنزل، بدقة decimal(18,2).
    /// </summary>
    public decimal AmountToCollect { get; private set; }

    /// <summary>
    /// المبلغ المالي الذي قام السائق باستلامه وتحصيله فعلياً من العميل.
    /// </summary>
    public decimal CollectedAmount { get; private set; }

    /// <summary>
    /// يشير إلى ما إذا كان المبلغ المالي قد تم تحصيله بنجاح وبشكل مؤكد.
    /// </summary>
    public bool IsCollected { get; private set; }

    /// <summary>
    /// تاريخ ووقت تحصيل المبلغ نقداً بواسطة السائق.
    /// </summary>
    public DateTime? CollectedAt { get; private set; }

    /// <summary>
    /// معرّف السائق الذي قام بتحصيل وتسلم المبلغ المالي من العميل.
    /// </summary>
    public Guid? CollectedByDriverId { get; private set; }

    /// <summary>
    /// سبب تعثر أو فشل تحصيل المبلغ المالي في حال عدم السداد (مثل: عدم توفر صرف، رفض الدفع).
    /// </summary>
    public string? FailureReason { get; private set; }

    /// <summary>
    /// مُنشئ محمي خالي من المعاملات مخصص لـ Entity Framework Core.
    /// </summary>
    private CashCollectionInfo()
    {
    }

    /// <summary>
    /// يُنشئ سجل تحصيل نقدي جديد بقيمة المبلغ المستحق، ويتحقق من عدم كونه سالباً.
    /// </summary>
    /// <param name="amountToCollect">المبلغ المطلوب تحصيله (لا يقل عن صفر).</param>
    /// <exception cref="Volo.Abp.BusinessException">يتم رميها إذا كان المبلغ المطلوب سالباً.</exception>
    public CashCollectionInfo(decimal amountToCollect)
    {
        if (amountToCollect < 0)
        {
            throw new Volo.Abp.BusinessException("AmountToCollect must be greater than or equal to zero.");
        }

        AmountToCollect = amountToCollect;
        CollectedAmount = 0;
        IsCollected = false;
    }

    /// <summary>
    /// يسجل نجاح تحصيل المبلغ المالي وتوثيق توقيت التحصيل وهوية السائق المستلم.
    /// يشترط أن يكون المبلغ المحصل مساوياً تماماً للمبلغ المطلوب سداده نقداً.
    /// </summary>
    /// <param name="collectedAmount">المبلغ المحصل فعلياً.</param>
    /// <param name="driverId">معرّف السائق المستلم.</param>
    /// <param name="collectedAt">تاريخ ووقت التحصيل.</param>
    /// <exception cref="Volo.Abp.BusinessException">يتم رميها إذا كان المبلغ سالباً أو غير مطابق للمبلغ المطلوب أو كان السائق فارغاً.</exception>
    public void MarkAsCollected(decimal collectedAmount, Guid driverId, DateTime collectedAt)
    {
        if (collectedAmount < 0)
        {
            throw new Volo.Abp.BusinessException("CollectedAmount must not be negative.");
        }

        if (collectedAmount != AmountToCollect)
        {
            throw new Volo.Abp.BusinessException($"CollectedAmount ({collectedAmount}) must equal AmountToCollect ({AmountToCollect}) for Cash On Delivery.");
        }

        if (driverId == Guid.Empty)
        {
            throw new Volo.Abp.BusinessException("DriverId must not be empty when recording cash collection.");
        }

        CollectedAmount = collectedAmount;
        CollectedByDriverId = driverId;
        CollectedAt = collectedAt;
        IsCollected = true;
        FailureReason = null;
    }

    /// <summary>
    /// يسجل تعثر أو فشل تحصيل المبلغ المالي وتوثيق السبب.
    /// </summary>
    /// <param name="reason">سبب تعثر التحصيل.</param>
    public void MarkAsFailed(string reason)
    {
        IsCollected = false;
        FailureReason = Volo.Abp.Check.NotNullOrWhiteSpace(reason, nameof(reason), maxLength: 500);
    }

    /// <summary>
    /// يُرجع القيم الذرية المحددة لمساواة كائن القيمة.
    /// </summary>
    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return AmountToCollect;
        yield return CollectedAmount;
        yield return IsCollected;
        yield return CollectedAt ?? DateTime.MinValue;
        yield return CollectedByDriverId ?? Guid.Empty;
        yield return FailureReason ?? string.Empty;
    }
}
