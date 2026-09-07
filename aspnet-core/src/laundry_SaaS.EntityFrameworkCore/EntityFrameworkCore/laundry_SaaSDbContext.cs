using laundry_SaaS.Bags;
using laundry_SaaS.Catalog;
using laundry_SaaS.Complaints;
using laundry_SaaS.Customers;
using laundry_SaaS.Inspections;
using laundry_SaaS.Laundries;
using laundry_SaaS.Notifications;
using laundry_SaaS.Orders;
using laundry_SaaS.PickupDelivery;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.AuditLogging.EntityFrameworkCore;
using Volo.Abp.BackgroundJobs.EntityFrameworkCore;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.FeatureManagement.EntityFrameworkCore;
using Volo.Abp.Identity;
using Volo.Abp.Identity.EntityFrameworkCore;
using Volo.Abp.OpenIddict.EntityFrameworkCore;
using Volo.Abp.PermissionManagement.EntityFrameworkCore;
using Volo.Abp.SettingManagement.EntityFrameworkCore;
using Volo.Abp.TenantManagement;
using Volo.Abp.TenantManagement.EntityFrameworkCore;

namespace laundry_SaaS.EntityFrameworkCore;

/// <summary>
/// سياق قاعدة البيانات الرئيسي لنظام منصة المغاسل السحابية (Laundry SaaS Platform).
/// يرث من <see cref="AbpDbContext{TSelf}"/> ويدير جميع الجداول الخاصة بالنطاق (Domain Entities)
/// بالإضافة إلى استبدال سياقات إدارة الهوية (Identity) والمستأجرين (Tenant Management)
/// لتمكين الاستعلامات المباشرة وعمليات الربط (Joins) ضمن نفس سياق المعاملة.
/// </summary>
[ReplaceDbContext(typeof(IIdentityDbContext))]
[ReplaceDbContext(typeof(ITenantManagementDbContext))]
[ConnectionStringName("Default")]
public class laundry_SaaSDbContext :
    AbpDbContext<laundry_SaaSDbContext>,
    IIdentityDbContext,
    ITenantManagementDbContext
{
    /* Add DbSet properties for your Aggregate Roots / Entities here. */

    #region Laundries & Branch Profiles

    /// <summary>
    /// جدول المغاسل (Laundries) - يمثل الكيان الجذري الرئيسي لكل مستأجر مغسلة.
    /// </summary>
    public DbSet<Laundry> Laundries { get; set; } = null!;

    /// <summary>
    /// جدول ساعات العمل الأسبوعية للمغسلة.
    /// </summary>
    public DbSet<LaundryWorkingHour> LaundryWorkingHours { get; set; } = null!;

    /// <summary>
    /// جدول النطاقات الزمنية المتاحة للاستلام والتوصيل في المغسلة.
    /// </summary>
    public DbSet<LaundryTimeSlot> LaundryTimeSlots { get; set; } = null!;

    /// <summary>
    /// جدول ملفات موظفي المغاسل وربطهم بمستخدمي الهوية.
    /// </summary>
    public DbSet<LaundryStaffProfile> LaundryStaffProfiles { get; set; } = null!;

    #endregion

    #region Catalog & Pricing

    /// <summary>
    /// جدول أنواع قطع الملابس المفهرسة في النظام.
    /// </summary>
    public DbSet<LaundryItemType> LaundryItemTypes { get; set; } = null!;

    /// <summary>
    /// جدول الخدمات المتاحة للغسيل والكي والتنظيف.
    /// </summary>
    public DbSet<LaundryService> LaundryServices { get; set; } = null!;

    /// <summary>
    /// جدول مصفوفة أسعار الخدمات لكل نوع قطعة داخل كل مغسلة.
    /// </summary>
    public DbSet<ServicePrice> ServicePrices { get; set; } = null!;

    #endregion

    #region Customers & Addresses

    /// <summary>
    /// جدول العملاء العام (Global Customer Profiles) على مستوى المنصة.
    /// </summary>
    public DbSet<Customer> Customers { get; set; } = null!;

    /// <summary>
    /// جدول دفاتر عناوين العملاء والمواقع الجغرافية المسجلة.
    /// </summary>
    public DbSet<CustomerAddress> CustomerAddresses { get; set; } = null!;

    #endregion

    #region Orders & Workflow

    /// <summary>
    /// جدول الطلبات الرئيسي - مصدر الحقيقة لدورة حياة الطلب والمبالغ المالية المعتمدة.
    /// </summary>
    public DbSet<Order> Orders { get; set; } = null!;

    /// <summary>
    /// جدول بنود الطلب واللقطات التاريخية للأسعار والكميات وقت الإنشاء.
    /// </summary>
    public DbSet<OrderItem> OrderItems { get; set; } = null!;

    /// <summary>
    /// جدول تعديلات الطلب وتعديل الأسعار الناتجة عن محضر الفحص.
    /// </summary>
    public DbSet<OrderAdjustment> OrderAdjustments { get; set; } = null!;

    /// <summary>
    /// سجل تاريخ تغيير حالات الطلب (Append-Only Audit Log).
    /// </summary>
    public DbSet<OrderStatusHistory> OrderStatusHistories { get; set; } = null!;

    /// <summary>
    /// سجل تاريخ مراحل المعالجة والتشغيل الداخلي للطلب (Append-Only Audit Log).
    /// </summary>
    public DbSet<ProcessingStageHistory> ProcessingStageHistories { get; set; } = null!;

    #endregion

    #region Logistics & Drivers

    /// <summary>
    /// جدول ملفات السائقين المعتمدين في النظام اللوجستي.
    /// </summary>
    public DbSet<Driver> Drivers { get; set; } = null!;

    /// <summary>
    /// جدول مهام ومحاولات استلام الملابس من العملاء.
    /// </summary>
    public DbSet<PickupTask> PickupTasks { get; set; } = null!;

    /// <summary>
    /// جدول مهام ومحاولات توصيل الملابس النظيفة والتحصيل النقدي.
    /// </summary>
    public DbSet<DeliveryTask> DeliveryTasks { get; set; } = null!;

    #endregion

    #region Bags & Custody

    /// <summary>
    /// جدول أكياس الغسيل الذكية وسلسلة الحيازة الفعلية.
    /// </summary>
    public DbSet<Bag> Bags { get; set; } = null!;

    /// <summary>
    /// سجل أحداث حركة ومسار كيس الغسيل وتتبع الحيازة المادية (Append-Only).
    /// </summary>
    public DbSet<BagEvent> BagEvents { get; set; } = null!;

    #endregion

    #region Inspection & Quality

    /// <summary>
    /// جدول محاضر الفحص الرسمي للقطع والملابس المستلمة.
    /// </summary>
    public DbSet<Inspection> Inspections { get; set; } = null!;

    /// <summary>
    /// جدول بنود الفحص ومطابقة الكميات الفعلية بالكميات المتوقعة.
    /// </summary>
    public DbSet<InspectionItem> InspectionItems { get; set; } = null!;

    /// <summary>
    /// جدول توثيق الأضرار والعيوب السابقة للغسيل والصور المرفقة.
    /// </summary>
    public DbSet<Damage> Damages { get; set; } = null!;

    #endregion

    #region Notifications & CRM

    /// <summary>
    /// جدول الإشعارات داخل التطبيق الموجهة للمستخدمين.
    /// </summary>
    public DbSet<AppNotification> AppNotifications { get; set; } = null!;

    /// <summary>
    /// جدول بلاغات وشكاوى العملاء ومتابعة حلها.
    /// </summary>
    public DbSet<Complaint> Complaints { get; set; } = null!;

    /// <summary>
    /// جدول مراجع الملفات والصور المرفقة بالشكاوى.
    /// </summary>
    public DbSet<ComplaintAttachment> ComplaintAttachments { get; set; } = null!;

    #endregion

    #region Entities from the modules

    /* Notice: We only implemented IIdentityDbContext and ITenantManagementDbContext
     * and replaced them for this DbContext. This allows you to perform JOIN
     * queries for the entities of these modules over the repositories easily. You
     * typically don't need that for other modules. But, if you need, you can
     * implement the DbContext interface of the needed module and use ReplaceDbContext
     * attribute just like IIdentityDbContext and ITenantManagementDbContext.
     *
     * More info: Replacing a DbContext of a module ensures that the related module
     * uses this DbContext on runtime. Otherwise, it will use its own DbContext class.
     */

    //Identity
    public DbSet<IdentityUser> Users { get; set; }
    public DbSet<IdentityRole> Roles { get; set; }
    public DbSet<IdentityClaimType> ClaimTypes { get; set; }
    public DbSet<OrganizationUnit> OrganizationUnits { get; set; }
    public DbSet<IdentitySecurityLog> SecurityLogs { get; set; }
    public DbSet<IdentityLinkUser> LinkUsers { get; set; }
    public DbSet<IdentityUserDelegation> UserDelegations { get; set; }
    public DbSet<IdentitySession> Sessions { get; set; }
    // Tenant Management
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<TenantConnectionString> TenantConnectionStrings { get; set; }

    #endregion

    /// <summary>
    /// مُنشئ سياق قاعدة البيانات وتمرير خيارات الاتصال لـ ABP و EF Core.
    /// </summary>
    /// <param name="options">خيارات تكوين سياق قاعدة البيانات ومزود SQL Server.</param>
    public laundry_SaaSDbContext(DbContextOptions<laundry_SaaSDbContext> options)
        : base(options)
    {

    }

    /// <summary>
    /// بناء وتكوين نموذج البيانات (Entity Data Model) وتطبيق قواعد الجداول والعلاقات والفهارس.
    /// </summary>
    /// <param name="builder">كائن بناء نموذج الكيانات التابع لـ Entity Framework Core.</param>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        /* Include modules to your migration db context */

        builder.ConfigurePermissionManagement();
        builder.ConfigureSettingManagement();
        builder.ConfigureBackgroundJobs();
        builder.ConfigureAuditLogging();
        builder.ConfigureIdentity();
        builder.ConfigureOpenIddict();
        builder.ConfigureFeatureManagement();
        builder.ConfigureTenantManagement();

        /* Configure your own tables/entities inside here */
        builder.ConfigureLaundryPlatform();
    }
}
