using System;
using System.ComponentModel.DataAnnotations;

namespace laundry_SaaS.Complaints;

/// <summary>
/// مدخلات إضافة مرفق إثباتي جديد لملف الشكوى (Add Complaint Attachment Input).
/// <para>
/// ملاحظة أمنية ومعمارية حرجة (Security Notice):
/// لا يتم استقبال الملفات الثنائية (Binary / Base64 / IFormFile) داخل هذه العقود، بل يتم إرسال المرجع السحابي (BlobName) للملف
/// الذي تم رفعه مسبقاً عبر مسار تخزين سحابي معتمد. إن مرجع BlobName القادم من العميل لا يُعتبر موثوقاً بمجرد كونه نصاً؛
/// ويجب على طبقة التطبيق (Application Implementation) لاحقاً التحقق الصارم من وجود الـ Blob الفعلي، وأن المستخدم الحالي
/// يملك حق استخدامه، وأن الملف يتبع السياق الصحيح لنوع الشكوى قبل اعتماده وربطه بكيان الشكوى في الدومين.
/// </para>
/// </summary>
public class AddComplaintAttachmentInput
{
    /// <summary>
    /// اسم مرجع الملف في وحدة التخزين السحابية (Blob Name).
    /// </summary>
    [Required]
    [StringLength(500, MinimumLength = 1)]
    public string BlobName { get; set; } = string.Empty;

    /// <summary>
    /// اسم الملف الأصلي بصيغته وامتداده (مثل: damaged_shirt.jpg).
    /// </summary>
    [Required]
    [StringLength(255, MinimumLength = 1)]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// نوع وسائط الإنترنت للملف (MIME Content-Type مثل: image/jpeg, application/pdf).
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 3)]
    public string ContentType { get; set; } = string.Empty;
}
