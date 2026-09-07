using System.Collections.Generic;
using Volo.Abp;
using Volo.Abp.Domain.Values;

namespace laundry_SaaS.Orders;

/// <summary>
/// كائن قيمة (Value Object) يمثل لقطة تاريخية ثابتة (Historical Snapshot) لعنوان العميل لحظة تثبيت الطلب.
/// <para>
/// يُستخدم مرتين داخل كيان الطلب (<see cref="Order"/>) لتمثيل كلٍ من عنوان الاستلام (<see cref="Order.PickupAddress"/>) وعنوان التوصيل (<see cref="Order.DeliveryAddress"/>).
/// لا يمتلك هذا الكائن معرّفاً مستقلاً (No Id) ولا مستودعاً خاصاً (No Repository)، ولا يتأثر بأي تعديل أو حذف لاحق يجريه العميل على ملف عناوينه في <see cref="Customers.CustomerAddress"/>.
/// </para>
/// </summary>
public class AddressSnapshot : ValueObject
{
    /// <summary>
    /// النص الكامل للعنوان كما كان مسجلاً لحظة إنشاء الطلب (اسم الحي، الشارع، المعالم).
    /// </summary>
    public string AddressText { get; private set; } = null!;

    /// <summary>
    /// اسم الشارع أو الطريق إن وجد.
    /// </summary>
    public string? Street { get; private set; }

    /// <summary>
    /// رقم أو اسم المبنى/العمارة السكنية.
    /// </summary>
    public string? Building { get; private set; }

    /// <summary>
    /// رقم الطابق أو الدور.
    /// </summary>
    public string? Floor { get; private set; }

    /// <summary>
    /// رقم الشقة أو المكتب أو الوحدة السكنية.
    /// </summary>
    public string? Apartment { get; private set; }

    /// <summary>
    /// خط العرض الجغرافي الدقيق لموقع العنوان (GPS Latitude).
    /// </summary>
    public double Latitude { get; private set; }

    /// <summary>
    /// خط الطول الجغرافي الدقيق لموقع العنوان (GPS Longitude).
    /// </summary>
    public double Longitude { get; private set; }

    /// <summary>
    /// ملاحظات العميل وإرشادات الوصول الخاصة بهذا العنوان وقت إنشاء الطلب.
    /// </summary>
    public string? Notes { get; private set; }

    /// <summary>
    /// مُنشئ محمي خالي من المعاملات مخصص لـ Entity Framework Core.
    /// </summary>
    private AddressSnapshot()
    {
    }

    /// <summary>
    /// يُنشئ لقطة تاريخية جديدة لعنوان استلام أو توصيل للطلب.
    /// </summary>
    /// <param name="addressText">النص الكامل للعنوان.</param>
    /// <param name="latitude">خط العرض الجغرافي.</param>
    /// <param name="longitude">خط الطول الجغرافي.</param>
    /// <param name="street">اسم الشارع الاختياري.</param>
    /// <param name="building">اسم أو رقم المبنى الاختياري.</param>
    /// <param name="floor">رقم الطابق الاختياري.</param>
    /// <param name="apartment">رقم الشقة الاختياري.</param>
    /// <param name="notes">ملاحظات التوصيل الاختيارية.</param>
    public AddressSnapshot(
        string addressText,
        double latitude,
        double longitude,
        string? street = null,
        string? building = null,
        string? floor = null,
        string? apartment = null,
        string? notes = null)
    {
        AddressText = Check.NotNullOrWhiteSpace(addressText, nameof(addressText));
        Common.GeoLocationValidator.ValidateCoordinates(latitude, longitude);
        Latitude = latitude;
        Longitude = longitude;
        Street = street;
        Building = building;
        Floor = floor;
        Apartment = apartment;
        Notes = notes;
    }

    /// <summary>
    /// يُرجع القيم الذرية المحددة لمساواة كائن القيمة.
    /// </summary>
    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return AddressText;
        yield return Street ?? string.Empty;
        yield return Building ?? string.Empty;
        yield return Floor ?? string.Empty;
        yield return Apartment ?? string.Empty;
        yield return Latitude;
        yield return Longitude;
        yield return Notes ?? string.Empty;
    }
}
