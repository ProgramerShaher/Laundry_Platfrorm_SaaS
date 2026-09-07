using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace laundry_SaaS.Customers;

/// <summary>
/// كيان تابع (Child Entity) يمثل أحد العناوين الجغرافية المسجلة للعميل.
/// <para>
/// مالك الجذر التجميعي هو <see cref="Customer"/>. يرث الكيان من <see cref="FullAuditedEntity{TKey}"/> مما يتيح للعميل
/// حذف عناوينه القديمة ناعماً (Soft Delete) مع الاحتفاظ بالسجلات لأغراض التدقيق.
/// يُلاحظ أنه عند إنشاء الطلب يتم أخذ لقطة تاريخية (<see cref="Orders.AddressSnapshot"/>) من العنوان لمنع تأثر الطلبات السابقة بأي تعديل مستقبلي يجريه العميل على عنوانه.
/// </para>
/// </summary>
public class CustomerAddress : FullAuditedEntity<Guid>
{
    /// <summary>
    /// معرّف العميل المالك (Customer Id) لهذا العنوان.
    /// </summary>
    public Guid CustomerId { get; private set; }

    /// <summary>
    /// عنوان وصفي مختصر يختاره العميل (مثل: المنزل، العمل، شقة الوالدة).
    /// </summary>
    public string Title { get; private set; } = null!;

    /// <summary>
    /// النص التفصيلي الكامل للعنوان (اسم الحي، الشارع، المعالم القريبة).
    /// </summary>
    public string AddressText { get; private set; } = null!;

    /// <summary>
    /// خط العرض الجغرافي الدقيق لموقع التوصيل على الخريطة (GPS Latitude).
    /// </summary>
    public double Latitude { get; private set; }

    /// <summary>
    /// خط الطول الجغرافي الدقيق لموقع التوصيل على الخريطة (GPS Longitude).
    /// </summary>
    public double Longitude { get; private set; }

    /// <summary>
    /// اسم أو رقم المبنى أو العمارة السكنية.
    /// </summary>
    public string? Building { get; private set; }

    /// <summary>
    /// رقم أو وصف الطابق / الدور.
    /// </summary>
    public string? Floor { get; private set; }

    /// <summary>
    /// رقم الشقة أو المكتب أو الوحدة السكنية.
    /// </summary>
    public string? Apartment { get; private set; }

    /// <summary>
    /// إرشادات أو ملاحظات إضافية يكتبها العميل لمساعدة السائق في الوصول بسهولة (مثل: الباب الجانبي، الاتصال عند الوصول).
    /// </summary>
    public string? Notes { get; private set; }

    /// <summary>
    /// يشير إلى ما إذا كان هذا العنوان هو العنوان الافتراضي المحدد تلقائياً للعميل عند إنشاء طلب جديد.
    /// </summary>
    public bool IsDefault { get; internal set; }

    /// <summary>
    /// مُنشئ محمي خالي من المعاملات مخصص لـ Entity Framework Core.
    /// </summary>
    private CustomerAddress()
    {
    }

    /// <summary>
    /// يُنشئ عنواناً جديداً للعميل مع تحديد الإحداثيات والتفاصيل السكنية.
    /// </summary>
    /// <param name="id">المعرّف الفريد للعنوان.</param>
    /// <param name="customerId">معرّف العميل المالك.</param>
    /// <param name="title">عنوان وصفي مختصر.</param>
    /// <param name="addressText">النص الكامل للعنوان.</param>
    /// <param name="latitude">خط العرض.</param>
    /// <param name="longitude">خط الطول.</param>
    /// <param name="building">اسم أو رقم المبنى.</param>
    /// <param name="floor">رقم الطابق.</param>
    /// <param name="apartment">رقم الشقة.</param>
    /// <param name="notes">ملاحظات وصول إضافية.</param>
    /// <param name="isDefault">ما إذا كان العنوان افتراضياً.</param>
    public CustomerAddress(
        Guid id,
        Guid customerId,
        string title,
        string addressText,
        double latitude,
        double longitude,
        string? building = null,
        string? floor = null,
        string? apartment = null,
        string? notes = null,
        bool isDefault = false)
        : base(id)
    {
        CustomerId = customerId;
        SetDetails(title, addressText, latitude, longitude, building, floor, apartment, notes);
        IsDefault = isDefault;
    }

    /// <summary>
    /// يعدل تفاصيل العنوان مع التحقق من الحقول الإلزامية والحدود القصوى للنصوص.
    /// </summary>
    /// <param name="title">العنوان المختصر الجديد.</param>
    /// <param name="addressText">النص الكامل للعنوان.</param>
    /// <param name="latitude">خط العرض الجديد.</param>
    /// <param name="longitude">خط الطول الجديد.</param>
    /// <param name="building">المبنى الجديد.</param>
    /// <param name="floor">الطابق الجديد.</param>
    /// <param name="apartment">الشقة الجديدة.</param>
    /// <param name="notes">الملاحظات الجديدة.</param>
    public void SetDetails(
        string title,
        string addressText,
        double latitude,
        double longitude,
        string? building = null,
        string? floor = null,
        string? apartment = null,
        string? notes = null)
    {
        Title = Check.NotNullOrWhiteSpace(title, nameof(title), maxLength: 100);
        AddressText = Check.NotNullOrWhiteSpace(addressText, nameof(addressText), maxLength: 500);
        Common.GeoLocationValidator.ValidateCoordinates(latitude, longitude);
        Latitude = latitude;
        Longitude = longitude;
        Building = building;
        Floor = floor;
        Apartment = apartment;
        Notes = notes;
    }
}
