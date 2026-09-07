using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace laundry_SaaS.Bags;

/// <summary>
/// يمثل عنصراً في قائمة حقائب الغسيل مع آخر أحداث سلسلة الحيازة.
/// </summary>
public class BagListDto : EntityDto<Guid>
{
    /// <summary>
    /// المعرف الفريد للطلب المرتبط بالحقيبة.
    /// </summary>
    public Guid OrderId { get; set; }

    /// <summary>
    /// رقم الطلب التتبعي للعرض.
    /// </summary>
    public string OrderNumber { get; set; } = null!;

    /// <summary>
    /// الرقم التسلسلي الفيزيائي للحقيبة (مثل: BAG-1004).
    /// </summary>
    public string BagNumber { get; set; } = null!;

    /// <summary>
    /// رمز الاستجابة السريعة (QR Code) الملصق على الحقيبة.
    /// </summary>
    public string QrCode { get; set; } = null!;

    /// <summary>
    /// يشير إلى ما إذا كانت الحقيبة قيد الاستخدام الفعلي والنشط حالياً.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// آخر حدث مسجل في سلسلة حيازة الحقيبة.
    /// </summary>
    public BagEventType? LastEventType { get; set; }

    /// <summary>
    /// تاريخ ووقت آخر حدث تشغيلي.
    /// </summary>
    public DateTime? LastEventAt { get; set; }

    /// <summary>
    /// الأحداث التشغيلية المسجلة للحقيبة.
    /// </summary>
    public List<BagEventDto> Events { get; set; } = new();
}
