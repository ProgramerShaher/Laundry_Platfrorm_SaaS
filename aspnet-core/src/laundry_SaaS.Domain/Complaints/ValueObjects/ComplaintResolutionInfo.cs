using System;
using System.Collections.Generic;
using Volo.Abp.Domain.Values;

namespace laundry_SaaS.Complaints;

/// <summary>
/// كائن قيمة (Value Object) يوثق القرارات والإجراءات المتخذة لحل شكوى العميل، بما في ذلك التعويضات المالية.
/// <para>
/// لا يمتلك هذا الكائن معرّفاً مستقلاً (No Id) أو مستودعاً خاصاً (No Repository)،
/// ويتم حفظه كـ Owned Type مدمج داخل جدول الشكاوى (<see cref="Complaint"/>).
/// </para>
/// </summary>
public class ComplaintResolutionInfo : ValueObject
{
    /// <summary>
    /// الملاحظات والقرارات التفصيلية المعتمدة لتسوية الشكوى ورضا العميل.
    /// </summary>
    public string? ResolutionNotes { get; private set; }

    /// <summary>
    /// معرّف المستخدم أو المشرف الإداري الذي تولى حل الشكوى وإغلاقها.
    /// </summary>
    public Guid? ResolvedByUserId { get; private set; }

    /// <summary>
    /// تاريخ ووقت اعتماد الحل النهائي للشكوى.
    /// </summary>
    public DateTime? ResolutionTime { get; private set; }

    /// <summary>
    /// مبلغ التعويض المالي المقترح أو الممنوح للعميل (إن وجد)، بدقة decimal(18,2).
    /// </summary>
    public decimal CompensationAmount { get; private set; }

    /// <summary>
    /// مُنشئ افتراضي خالي من المعاملات.
    /// </summary>
    public ComplaintResolutionInfo()
    {
    }

    /// <summary>
    /// يُنشئ سجل حل وتسوية شكوى مع توثيق الملاحظات وهوية المعتمد ومبلغ التعويض.
    /// </summary>
    /// <param name="resolutionNotes">ملاحظات الحل.</param>
    /// <param name="resolvedByUserId">معرّف الموظف الذي حل الشكوى.</param>
    /// <param name="resolutionTime">تاريخ ووقت الحل.</param>
    /// <param name="compensationAmount">مبلغ التعويض المالي (افتراضياً صفر).</param>
    public ComplaintResolutionInfo(
        string? resolutionNotes,
        Guid? resolvedByUserId,
        DateTime? resolutionTime,
        decimal compensationAmount = 0)
    {
        if (compensationAmount < 0)
        {
            throw new Volo.Abp.BusinessException("CompensationAmount must be greater than or equal to zero.");
        }

        ResolutionNotes = resolutionNotes;
        ResolvedByUserId = resolvedByUserId;
        ResolutionTime = resolutionTime;
        CompensationAmount = compensationAmount;
    }

    /// <summary>
    /// يُرجع القيم الذرية المحددة لمساواة كائن القيمة.
    /// </summary>
    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return ResolutionNotes ?? string.Empty;
        yield return ResolvedByUserId ?? Guid.Empty;
        yield return ResolutionTime ?? DateTime.MinValue;
        yield return CompensationAmount;
    }
}
