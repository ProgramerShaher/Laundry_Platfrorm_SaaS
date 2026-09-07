using System;
using Volo.Abp.Application.Dtos;

namespace laundry_SaaS.Customers;

/// <summary>
/// مخرجات تفاصيل عنوان العميل المسجل في حسابه (Customer Address DTO).
/// </summary>
public class CustomerAddressDto : EntityDto<Guid>
{
    /// <summary>
    /// عنوان وصفي للعنوان (مثل المنزل، العمل، الشاليه).
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// النص الكامل للعنوان.
    /// </summary>
    public string AddressText { get; set; } = string.Empty;

    /// <summary>
    /// خط العرض لموقع العنوان.
    /// </summary>
    public double Latitude { get; set; }

    /// <summary>
    /// خط الطول لموقع العنوان.
    /// </summary>
    public double Longitude { get; set; }

    /// <summary>
    /// اسم أو رقم الشارع اختياري.
    /// </summary>
    public string? Street { get; set; }

    /// <summary>
    /// اسم أو رقم المبنى اختياري.
    /// </summary>
    public string? Building { get; set; }

    /// <summary>
    /// رقم الدور أو الطابق اختياري.
    /// </summary>
    public string? Floor { get; set; }

    /// <summary>
    /// رقم الشقة اختياري.
    /// </summary>
    public string? Apartment { get; set; }

    /// <summary>
    /// ملاحظات أو تعليمات خاصة للمندوب اختياري.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// هل هذا العنوان هو العنوان الافتراضي المحدد للعميل.
    /// </summary>
    public bool IsDefault { get; set; }
}
