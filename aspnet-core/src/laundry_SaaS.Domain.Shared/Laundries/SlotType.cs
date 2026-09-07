namespace laundry_SaaS.Laundries;

/// <summary>
/// يحدد نوع الفترة الزمنية (Time Slot) المخصصة للمغسلة، سواء كانت مخصصة لجدولة استلام الملابس من العميل أو توصيلها إليه.
/// </summary>
public enum SlotType
{
    /// <summary>
    /// فترة زمنية مخصصة لعمليات استلام الملابس من موقع العميل (Pickup).
    /// </summary>
    Pickup = 1,

    /// <summary>
    /// فترة زمنية مخصصة لعمليات توصيل وتسليم الملابس النظيفة إلى موقع العميل (Delivery).
    /// </summary>
    Delivery = 2
}
