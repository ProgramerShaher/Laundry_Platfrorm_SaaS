using System;
using System.Collections.Generic;
using System.Linq;
using laundry_SaaS.Catalog;
using laundry_SaaS.Inspections;
using Volo.Abp;
using Volo.Abp.Domain.Services;

namespace laundry_SaaS.Orders;

/// <summary>
/// خدمة نطاق (Domain Service) مسؤولة عن احتساب الفروقات والتعديلات المالية للطلبات (Order Adjustments)
/// الناتجة حصرياً عن محضر الفحص الفني المعتمد (<see cref="Inspection"/>).
/// <para>
/// تطبق هذه الخدمة الحماية المالية الصارمة لمنع تلاعب واجهات المستخدم أو موظفي المغاسل بالإجمالي:
/// <list type="bullet">
/// <item><description>لا تستقبل ولا تثق في أي أسعار خام مجردة مرسلة من واجهة المستخدم أو الموظف (تم حذف قاموس الأسعار الرقمية overrideItemUnitPrices نهائياً).</description></item>
/// <item><description>تعتمد على كيانات أسعار موثوقة (<see cref="ServicePrice"/>) محملة ومحققة من قاعدة البيانات ومطابقة لمستأجر الطلب.</description></item>
/// <item><description><b>قاعدة الحفاظ على السعر التاريخي للأصناف الأصلية:</b> الصنف الأصلي المطابق أو المعدل كميته فقط يحتفظ بلقطته السعرية الأصلية (<see cref="OrderItem.UnitPriceSnapshot"/>) حتى لو تغير سعر الكتالوج لاحقاً.</description></item>
/// <item><description><b>قاعدة تسعير البنود الجديدة والمعدلة:</b> أي بند إضافي لم يكن بالطلب أو بند تم تغيير نوع صنفه أو خدمته، يتم تسعيره وفق سجل التسعير الموثوق النشط (<see cref="ServicePrice"/>) للتركيبة الفعلية الجديدة.</description></item>
/// <item><description><b>الحماية من غياب السعر:</b> في حال عدم العثور على تسعير فعال ونشط للتركيبة الفعلية، لا يتم وضع السعر صفراً ولا ينشأ تعديل ناقص، بل يُرمى استثناء أعمال <c>ServicePriceNotFoundForInspectedItem</c>.</description></item>
/// <item><description><b>الأمان وتعدد المستأجرين (Tenant Security):</b> يتم التحقق الصارم من تطابق TenantId بين الطلب ومحضر الفحص وسجلات التسعير.</description></item>
/// </list>
/// </para>
/// </summary>
public class OrderAdjustmentDomainService : DomainService
{
    /// <summary>
    /// يحسب الفوارق المالية الناتجة عن محضر الفحص الفني المعتمد وينشئ التعديل المالي في الطلب إذا وجد اختلاف في القيمة الإجمالية.
    /// </summary>
    /// <param name="order">كيان الطلب المراد تعديله واحتساب فوارقه المالية.</param>
    /// <param name="inspection">محضر الفحص الفني المعتمد لنفس الطلب.</param>
    /// <param name="reason">السبب التشغيلي الموثق للتعديل.</param>
    /// <param name="currentPrices">مجموعة سجلات التسعير الموثوقة (<see cref="ServicePrice"/>) المحملة من قاعدة البيانات لنفس المستأجر.</param>
    /// <returns>كيان التعديل المالي (<see cref="OrderAdjustment"/>) إذا ترتب فارق مالي، أو <c>null</c> إذا لم يوجد أي اختلاف مالي.</returns>
    /// <exception cref="BusinessException">يتم رميها عند وجود خرق في أمان المستأجر، أو نقص في سجلات الأسعار، أو عدم اكتمال الفحص.</exception>
    public OrderAdjustment? CalculateAndApplyAdjustment(
        Order order,
        Inspection inspection,
        string reason,
        IReadOnlyCollection<ServicePrice> currentPrices)
    {
        Check.NotNull(order, nameof(order));
        Check.NotNull(inspection, nameof(inspection));
        Check.NotNullOrWhiteSpace(reason, nameof(reason));
        Check.NotNull(currentPrices, nameof(currentPrices));

        // 1. Tenant Security & Order Consistency checks
        if (inspection.OrderId != order.Id)
        {
            throw new BusinessException($"Inspection with id '{inspection.Id}' belongs to order '{inspection.OrderId}', not order '{order.Id}'.");
        }

        if (inspection.TenantId != order.TenantId)
        {
            throw new BusinessException("Tenant mismatch between Order and Inspection.");
        }

        foreach (var price in currentPrices)
        {
            if (price.TenantId != order.TenantId)
            {
                throw new BusinessException("Tenant mismatch: ServicePrice does not belong to the order's tenant.");
            }
        }

        if (inspection.Status != InspectionStatus.Completed)
        {
            throw new BusinessException($"Cannot calculate adjustment for an inspection in status '{inspection.Status}'. Inspection must be '{InspectionStatus.Completed}'.");
        }

        // فهرسة بنود الفحص بحسب OrderItemId لتسهيل المطابقة
        var inspectionItemsByOrderItemId = inspection.Items
            .Where(i => i.OrderItemId.HasValue)
            .ToDictionary(i => i.OrderItemId!.Value);

        // قائمة البنود الإضافية (التي لا ترتبط بـ OrderItemId أصلي)
        var additionalInspectionItems = inspection.Items
            .Where(i => !i.OrderItemId.HasValue)
            .ToList();

        decimal calculatedSubtotal = 0;

        // 2. معالجة بنود الطلب الأصلية
        foreach (var orderItem in order.Items)
        {
            if (inspectionItemsByOrderItemId.TryGetValue(orderItem.Id, out var inspectionItem))
            {
                // تم فحص هذا البند:
                // إذا كانت كميته المستلمة 0 (مفقود بالكامل) فلا يضاف له أي قيمة مالية
                if (inspectionItem.ActualQuantity == 0)
                {
                    continue;
                }

                // التحقق هل تم تغيير نوع الصنف أو نوع الخدمة
                bool isItemTypeChanged = inspectionItem.ActualLaundryItemTypeId.HasValue &&
                                         inspectionItem.ActualLaundryItemTypeId.Value != orderItem.LaundryItemTypeId;

                bool isServiceChanged = inspectionItem.ActualLaundryServiceId.HasValue &&
                                        inspectionItem.ActualLaundryServiceId.Value != orderItem.LaundryServiceId;

                if (isItemTypeChanged || isServiceChanged)
                {
                    // تغير الصنف أو الخدمة: يتطلب سعراً جديداً من الكتالوج الموثوق
                    var targetItemTypeId = inspectionItem.ActualLaundryItemTypeId!.Value;
                    var targetServiceId = inspectionItem.ActualLaundryServiceId!.Value;

                    var priceRecord = currentPrices.FirstOrDefault(p =>
                        p.LaundryItemTypeId == targetItemTypeId &&
                        p.LaundryServiceId == targetServiceId &&
                        p.IsActive);

                    if (priceRecord == null)
                    {
                        throw new BusinessException(
                            laundry_SaaSDomainErrorCodes.OrderErrorCodes.ServicePriceNotFoundForInspectedItem,
                            $"No active ServicePrice found for item type '{targetItemTypeId}' and service '{targetServiceId}'.");
                    }

                    calculatedSubtotal += inspectionItem.ActualQuantity * priceRecord.Price;
                }
                else
                {
                    // لم يتغير الصنف ولا الخدمة (مطابق تماماً أو تغيرت الكمية فقط):
                    // قاعدة تاريخية إلزامية: نستخدم السعر المحفوظ في اللقطة UnitPriceSnapshot
                    // ولا نتأثر بأي تغيرات في أسعار الكتالوج الحالية!
                    calculatedSubtotal += inspectionItem.ActualQuantity * orderItem.UnitPriceSnapshot;
                }
            }
            else
            {
                // بند أصلي لم يُدرج في بنود الفحص: يحتفظ بكميته الأصلية ولقطته السعرية
                calculatedSubtotal += orderItem.Quantity * orderItem.UnitPriceSnapshot;
            }
        }

        // 3. معالجة البنود الإضافية المكتشفة في الفحص (Additional Items)
        foreach (var additionalItem in additionalInspectionItems)
        {
            if (additionalItem.ActualQuantity <= 0)
            {
                continue;
            }

            var targetItemTypeId = additionalItem.ActualLaundryItemTypeId!.Value;
            var targetServiceId = additionalItem.ActualLaundryServiceId!.Value;

            var priceRecord = currentPrices.FirstOrDefault(p =>
                p.LaundryItemTypeId == targetItemTypeId &&
                p.LaundryServiceId == targetServiceId &&
                p.IsActive);

            if (priceRecord == null)
            {
                throw new BusinessException(
                    laundry_SaaSDomainErrorCodes.OrderErrorCodes.ServicePriceNotFoundForInspectedItem,
                    $"No active ServicePrice found for additional item type '{targetItemTypeId}' and service '{targetServiceId}'.");
            }

            calculatedSubtotal += additionalItem.ActualQuantity * priceRecord.Price;
        }

        // 4. احتساب الإجمالي الجديد مع مراعاة رسوم التوصيل والخصم المعتمدين في الطلب
        var calculatedNewTotal = calculatedSubtotal + order.DeliveryFee - order.Discount;
        if (calculatedNewTotal < 0)
        {
            calculatedNewTotal = 0;
        }

        // 5. إذا لم يوجد أي فارق مالي بين الإجمالي المحسوب والإجمالي الحالي للطلب، فلا يتم إنشاء تعديل
        if (calculatedNewTotal == order.Total)
        {
            return null;
        }

        // 6. إنشاء التعديل المالي وفق قواعد النطاق:
        // الزيادة تتطلب موافقة العميل (WaitingForAdjustmentApproval / Pending)
        // النقصان يعتمد تلقائياً لصالح العميل (Auto Approved)
        return order.CreateAdjustment(reason, calculatedNewTotal);
    }
}
