using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace laundry_SaaS.Complaints;

/// <summary>
/// بيانات إدخال رفع وتسجيل شكوى جديدة من العميل بشأن طلب محدد.
/// </summary>
public class CreateComplaintInput
{
    /// <summary>
    /// المعرف الفريد للطلب محل الشكوى.
    /// </summary>
    [Required]
    public Guid OrderId { get; set; }

    /// <summary>
    /// نوع وتصنيف الشكوى (تلف، فقدان، تأخير، جودة، سلوك).
    /// </summary>
    [Required]
    public ComplaintType Type { get; set; }

    /// <summary>
    /// موضوع أو عنوان الشكوى الموجز.
    /// </summary>
    [Required]
    [StringLength(200)]
    public string Subject { get; set; } = null!;

    /// <summary>
    /// الشرح التفصيلي للشكوى والوقائع.
    /// </summary>
    [Required]
    [StringLength(2000)]
    public string Description { get; set; } = null!;

    /// <summary>
    /// قائمة أسماء ملفات المرفقات والصور الإثباتية في التخزين السحابي إن وجدت.
    /// </summary>
    public List<string> AttachmentBlobNames { get; set; } = new();
}
