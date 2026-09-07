using System;
using laundry_SaaS.LaundryProcessing;
using Volo.Abp.Application.Dtos;

namespace laundry_SaaS.Orders;

/// <summary>
/// مخرجات سجل التغيير التاريخي لمراحل معالجة الملابس داخل المغسلة (Processing Stage History DTO).
/// </summary>
public class ProcessingStageHistoryDto : EntityDto<Guid>
{
    /// <summary>
    /// المرحلة التشغيلية التي انتقل إليها الطلب (مثل فرز، غسيل، كي، تغليف).
    /// </summary>
    public ProcessingStage Stage { get; set; }

    /// <summary>
    /// معرّف الموظف الذي قام بتنفيذ المرحلة اختياري.
    /// </summary>
    public Guid? OperatorStaffId { get; set; }

    /// <summary>
    /// ملاحظات تشغيلية حول المرحلة اختياري.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// تاريخ ووقت الانتقال للمرحلة.
    /// </summary>
    public DateTime TransitionedAt { get; set; }
}
