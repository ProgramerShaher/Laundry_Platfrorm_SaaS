using laundry_SaaS.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;
using Volo.Abp.MultiTenancy;

namespace laundry_SaaS.Permissions;

/// <summary>
/// مزود تعريف الصلاحيات (Permission Definition Provider) الخاص بمنصة المغاسل السحابية.
/// <para>
/// يقوم بتسجيل شجرة الصلاحيات المعتمدة وتحديد نطاق كل صلاحية (المضيف Host أو المستأجر Tenant).
/// </para>
/// </summary>
public class laundry_SaaSPermissionDefinitionProvider : PermissionDefinitionProvider
{
    /// <summary>
    /// يعرف مجموعات وبنود الصلاحيات الخاصة بالمنصة ويسجلها في سياق الصلاحيات.
    /// </summary>
    /// <param name="context">سياق تعريف الصلاحيات في ABP.</param>
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(laundry_SaaSPermissions.GroupName, L("Permission:laundry_SaaS"));

        // 1. Host Permissions (Host MultiTenancySide only)
        var hostGroup = myGroup.AddPermission(laundry_SaaSPermissions.Host.Default, L("Permission:Host"), MultiTenancySides.Host);
        hostGroup.AddChild(laundry_SaaSPermissions.Host.ViewLaundries, L("Permission:Host.Laundries.View"), MultiTenancySides.Host);
        hostGroup.AddChild(laundry_SaaSPermissions.Host.ManageLaundries, L("Permission:Host.Laundries.Manage"), MultiTenancySides.Host);
        hostGroup.AddChild(laundry_SaaSPermissions.Host.ViewStatistics, L("Permission:Host.Statistics.View"), MultiTenancySides.Host);

        // 2. Laundry Management Permissions (Tenant MultiTenancySide)
        var laundryGroup = myGroup.AddPermission(laundry_SaaSPermissions.Laundry.Default, L("Permission:Laundry"), MultiTenancySides.Tenant);
        laundryGroup.AddChild(laundry_SaaSPermissions.Laundry.ViewProfile, L("Permission:Laundry.Profile.View"), MultiTenancySides.Tenant);
        laundryGroup.AddChild(laundry_SaaSPermissions.Laundry.ManageProfile, L("Permission:Laundry.Profile.Manage"), MultiTenancySides.Tenant);
        laundryGroup.AddChild(laundry_SaaSPermissions.Laundry.ManageWorkingHours, L("Permission:Laundry.WorkingHours.Manage"), MultiTenancySides.Tenant);
        laundryGroup.AddChild(laundry_SaaSPermissions.Laundry.ManageTimeSlots, L("Permission:Laundry.TimeSlots.Manage"), MultiTenancySides.Tenant);

        // 3. Staff Permissions
        var staffGroup = myGroup.AddPermission(laundry_SaaSPermissions.Staff.Default, L("Permission:Staff"), MultiTenancySides.Tenant);
        staffGroup.AddChild(laundry_SaaSPermissions.Staff.Manage, L("Permission:Staff.Manage"), MultiTenancySides.Tenant);

        // 4. Catalog Permissions
        var catalogGroup = myGroup.AddPermission(laundry_SaaSPermissions.Catalog.Default, L("Permission:Catalog"), MultiTenancySides.Tenant);
        catalogGroup.AddChild(laundry_SaaSPermissions.Catalog.ManageItemTypes, L("Permission:Catalog.ItemTypes.Manage"), MultiTenancySides.Tenant);
        catalogGroup.AddChild(laundry_SaaSPermissions.Catalog.ManageServices, L("Permission:Catalog.Services.Manage"), MultiTenancySides.Tenant);
        catalogGroup.AddChild(laundry_SaaSPermissions.Catalog.ManagePrices, L("Permission:Catalog.Prices.Manage"), MultiTenancySides.Tenant);

        // 5. Orders Permissions
        var ordersGroup = myGroup.AddPermission(laundry_SaaSPermissions.Orders.Default, L("Permission:Orders"), MultiTenancySides.Tenant);
        ordersGroup.AddChild(laundry_SaaSPermissions.Orders.View, L("Permission:Orders.View"), MultiTenancySides.Tenant);
        ordersGroup.AddChild(laundry_SaaSPermissions.Orders.Process, L("Permission:Orders.Process"), MultiTenancySides.Tenant);
        ordersGroup.AddChild(laundry_SaaSPermissions.Orders.Adjust, L("Permission:Orders.Adjust"), MultiTenancySides.Tenant);
        ordersGroup.AddChild(laundry_SaaSPermissions.Orders.CancelAdmin, L("Permission:Orders.CancelAdmin"), MultiTenancySides.Tenant);

        // 6. Drivers & Logistics Permissions
        var driversGroup = myGroup.AddPermission(laundry_SaaSPermissions.Drivers.Default, L("Permission:Drivers"), MultiTenancySides.Tenant);
        driversGroup.AddChild(laundry_SaaSPermissions.Drivers.Manage, L("Permission:Drivers.Manage"), MultiTenancySides.Tenant);
        driversGroup.AddChild(laundry_SaaSPermissions.Drivers.AssignTasks, L("Permission:Logistics.AssignTasks"), MultiTenancySides.Tenant);
        driversGroup.AddChild(laundry_SaaSPermissions.Drivers.ExecuteTasks, L("Permission:Logistics.ExecuteTasks"), MultiTenancySides.Tenant);

        // 7. Processing & Bags Permissions
        var processingGroup = myGroup.AddPermission(laundry_SaaSPermissions.Processing.Default, L("Permission:Processing"), MultiTenancySides.Tenant);
        processingGroup.AddChild(laundry_SaaSPermissions.Processing.Execute, L("Permission:Processing.Execute"), MultiTenancySides.Tenant);
        processingGroup.AddChild(laundry_SaaSPermissions.Processing.ManageBags, L("Permission:Bags.Manage"), MultiTenancySides.Tenant);

        // 8. Inspection Permissions
        var inspectionGroup = myGroup.AddPermission(laundry_SaaSPermissions.Inspection.Default, L("Permission:Inspection"), MultiTenancySides.Tenant);
        inspectionGroup.AddChild(laundry_SaaSPermissions.Inspection.Manage, L("Permission:Inspection.Manage"), MultiTenancySides.Tenant);

        // 9. Complaints Permissions
        var complaintsGroup = myGroup.AddPermission(laundry_SaaSPermissions.Complaints.Default, L("Permission:Complaints"), MultiTenancySides.Tenant);
        complaintsGroup.AddChild(laundry_SaaSPermissions.Complaints.View, L("Permission:Complaints.View"), MultiTenancySides.Tenant);
        complaintsGroup.AddChild(laundry_SaaSPermissions.Complaints.Resolve, L("Permission:Complaints.Resolve"), MultiTenancySides.Tenant);
    }

    /// <summary>
    /// دالة مساعدة لإنشاء نصوص مترجمة لأسماء الصلاحيات باستخدام ملف الموارد المحلي للمنصة.
    /// </summary>
    /// <param name="name">مفتاح الترجمة.</param>
    /// <returns>نص محلي قابل للترجمة.</returns>
    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<laundry_SaaSResource>(name);
    }
}
