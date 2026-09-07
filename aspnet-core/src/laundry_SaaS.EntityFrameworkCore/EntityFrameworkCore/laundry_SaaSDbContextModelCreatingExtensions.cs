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
using Volo.Abp;
using Volo.Abp.EntityFrameworkCore.Modeling;

namespace laundry_SaaS.EntityFrameworkCore;

/// <summary>
/// فئة دوال التوسعة (Extension Methods) لتكوين نموذج البيانات (Entity Framework Core Model Configuration)
/// لمنصة المغاسل السحابية (Laundry SaaS Platform).
/// <para>
/// تضبط هذه الفئة:
/// <list type="bullet">
/// <item><description>أسماء الجداول ومخطط قاعدة البيانات (Table Prefix &amp; Schema).</description></item>
/// <item><description>تجاهل كائنات القيمة (Value Objects) كجداول منفصلة وتضمينها عبر <c>OwnsOne</c> مثل <see cref="CoverageArea"/> و <see cref="AddressSnapshot"/> و <see cref="CashCollectionInfo"/> و <see cref="ComplaintResolutionInfo"/>.</description></item>
/// <item><description>الدقة العشرية (Decimal Precision) لجميع الحقول واللقطات المالية إلى <c>decimal(18, 2)</c>.</description></item>
/// <item><description>الفهارس الفريدة والمركبة (Unique &amp; Composite Indexes) لضمان سلامة النطاق وسرعة البحث وعزل المستأجرين.</description></item>
/// <item><description>سلوك الحذف (DeleteBehavior) مع حظر الحذف الشلالي <c>Restrict</c> في السجلات التاريخية لمنع فقدان البيانات، واستخدام <c>Cascade</c> فقط مع الكيانات الفرعية التابعة مباشرة للجذر التجميعي.</description></item>
/// </list>
/// </para>
/// </summary>
public static class laundry_SaaSDbContextModelCreatingExtensions
{
    /// <summary>
    /// تكوين مخطط الجداول والعلاقات والقيود لجميع كيانات نطاق منصة المغاسل.
    /// </summary>
    /// <param name="builder">كائن بناء نموذج الكيانات <see cref="ModelBuilder"/>.</param>
    public static void ConfigureLaundryPlatform(this ModelBuilder builder)
    {
        Check.NotNull(builder, nameof(builder));

        // Ignore Value Objects as standalone entity types
        builder.Ignore<CoverageArea>();
        builder.Ignore<AddressSnapshot>();
        builder.Ignore<CashCollectionInfo>();
        builder.Ignore<ComplaintResolutionInfo>();

        var tablePrefix = laundry_SaaSConsts.DbTablePrefix;
        var schema = laundry_SaaSConsts.DbSchema;

        // 1. Laundry
        builder.Entity<Laundry>(b =>
        {
            b.ToTable(tablePrefix + "Laundries", schema);

            b.OwnsOne(x => x.CoverageArea, ca =>
            {
                ca.Property(c => c.MinimumOrderAmount).HasPrecision(18, 2);
            });

            b.ConfigureByConvention();

            b.Property(x => x.Name).IsRequired().HasMaxLength(200);
            b.Property(x => x.Description).HasMaxLength(1000);
            b.Property(x => x.PhoneNumber).IsRequired().HasMaxLength(30);
            b.Property(x => x.Email).HasMaxLength(200);
            b.Property(x => x.LogoBlobName).HasMaxLength(500);
            b.Property(x => x.DeliveryFee).HasPrecision(18, 2);
            b.Property(x => x.MinimumOrderAmount).HasPrecision(18, 2);

            b.HasMany(x => x.WorkingHours).WithOne().HasForeignKey(x => x.LaundryId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(x => x.TimeSlots).WithOne().HasForeignKey(x => x.LaundryId).OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(x => x.TenantId).IsUnique().HasFilter("[IsDeleted] = 0");
        });

        // 2. LaundryWorkingHour
        builder.Entity<LaundryWorkingHour>(b =>
        {
            b.ToTable(tablePrefix + "LaundryWorkingHours", schema);
            b.ConfigureByConvention();

            b.HasIndex(x => new { x.LaundryId, x.DayOfWeek }).IsUnique();
        });

        // 3. LaundryTimeSlot
        builder.Entity<LaundryTimeSlot>(b =>
        {
            b.ToTable(tablePrefix + "LaundryTimeSlots", schema);
            b.ConfigureByConvention();

            b.Property(x => x.DayOfWeek).IsRequired();

            b.HasIndex(x => new { x.LaundryId, x.DayOfWeek, x.SlotType, x.StartTime, x.EndTime }).IsUnique();
        });

        // 4. LaundryStaffProfile
        builder.Entity<LaundryStaffProfile>(b =>
        {
            b.ToTable(tablePrefix + "LaundryStaffProfiles", schema);
            b.ConfigureByConvention();

            b.Property(x => x.JobTitle).HasMaxLength(100);

            b.HasIndex(x => new { x.TenantId, x.UserId }).IsUnique().HasFilter("[IsDeleted] = 0");
        });

        // 5. LaundryItemType
        builder.Entity<LaundryItemType>(b =>
        {
            b.ToTable(tablePrefix + "LaundryItemTypes", schema);
            b.ConfigureByConvention();

            b.Property(x => x.Name).IsRequired().HasMaxLength(100);
            b.Property(x => x.Code).IsRequired().HasMaxLength(50);
            b.Property(x => x.Description).HasMaxLength(500);

            b.HasIndex(x => new { x.TenantId, x.Code }).IsUnique().HasFilter("[IsDeleted] = 0");
        });

        // 6. LaundryService
        builder.Entity<LaundryService>(b =>
        {
            b.ToTable(tablePrefix + "LaundryServices", schema);
            b.ConfigureByConvention();

            b.Property(x => x.Name).IsRequired().HasMaxLength(100);
            b.Property(x => x.Code).IsRequired().HasMaxLength(50);
            b.Property(x => x.Description).HasMaxLength(500);

            b.HasIndex(x => new { x.TenantId, x.Code }).IsUnique().HasFilter("[IsDeleted] = 0");
        });

        // 7. ServicePrice
        builder.Entity<ServicePrice>(b =>
        {
            b.ToTable(tablePrefix + "ServicePrices", schema);
            b.ConfigureByConvention();

            b.Property(x => x.Price).HasPrecision(18, 2);

            b.HasIndex(x => new { x.TenantId, x.LaundryItemTypeId, x.LaundryServiceId }).IsUnique().HasFilter("[IsDeleted] = 0");
        });

        // 8. Customer (Global, Host-level)
        builder.Entity<Customer>(b =>
        {
            b.ToTable(tablePrefix + "Customers", schema);
            b.ConfigureByConvention();

            b.Property(x => x.PhoneNumber).IsRequired().HasMaxLength(30);
            b.Property(x => x.DisabledReason).HasMaxLength(500);

            b.HasMany(x => x.Addresses).WithOne().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(x => x.UserId).IsUnique();
            b.HasIndex(x => x.PhoneNumber).IsUnique();
        });

        // 9. CustomerAddress
        builder.Entity<CustomerAddress>(b =>
        {
            b.ToTable(tablePrefix + "CustomerAddresses", schema);
            b.ConfigureByConvention();

            b.Property(x => x.Title).IsRequired().HasMaxLength(100);
            b.Property(x => x.AddressText).IsRequired().HasMaxLength(500);
            b.Property(x => x.Building).HasMaxLength(100);
            b.Property(x => x.Floor).HasMaxLength(50);
            b.Property(x => x.Apartment).HasMaxLength(50);
            b.Property(x => x.Notes).HasMaxLength(500);

            b.HasIndex(x => x.CustomerId);
            b.HasIndex(x => x.CustomerId, "IX_CustomerAddresses_CustomerId_Default")
                .IsUnique()
                .HasFilter("[IsDefault] = 1 AND [IsDeleted] = 0");
        });

        // 10. Order
        builder.Entity<Order>(b =>
        {
            b.ToTable(tablePrefix + "Orders", schema);

            b.OwnsOne(x => x.PickupAddress, a =>
            {
                a.Property(p => p.AddressText).IsRequired().HasMaxLength(500);
                a.Property(p => p.Street).HasMaxLength(200);
                a.Property(p => p.Building).HasMaxLength(100);
                a.Property(p => p.Floor).HasMaxLength(50);
                a.Property(p => p.Apartment).HasMaxLength(50);
                a.Property(p => p.Notes).HasMaxLength(500);
            });

            b.OwnsOne(x => x.DeliveryAddress, a =>
            {
                a.Property(p => p.AddressText).IsRequired().HasMaxLength(500);
                a.Property(p => p.Street).HasMaxLength(200);
                a.Property(p => p.Building).HasMaxLength(100);
                a.Property(p => p.Floor).HasMaxLength(50);
                a.Property(p => p.Apartment).HasMaxLength(50);
                a.Property(p => p.Notes).HasMaxLength(500);
            });

            b.ConfigureByConvention();

            b.Property(x => x.OrderNumber).IsRequired().HasMaxLength(50);
            b.Property(x => x.CustomerNotes).HasMaxLength(1000);
            b.Property(x => x.CancellationReason).HasMaxLength(500);

            b.Property(x => x.Subtotal).HasPrecision(18, 2);
            b.Property(x => x.DeliveryFee).HasPrecision(18, 2);
            b.Property(x => x.Discount).HasPrecision(18, 2);
            b.Property(x => x.Total).HasPrecision(18, 2);

            // Historical child entities with Restrict delete behavior
            b.HasMany(x => x.Items).WithOne().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
            b.HasMany(x => x.Adjustments).WithOne().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
            b.HasMany(x => x.StatusHistories).WithOne().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
            b.HasMany(x => x.ProcessingStageHistories).WithOne().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(x => new { x.TenantId, x.OrderNumber }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.CustomerId });
            b.HasIndex(x => new { x.TenantId, x.Status });
        });

        // 11. OrderItem
        builder.Entity<OrderItem>(b =>
        {
            b.ToTable(tablePrefix + "OrderItems", schema);
            b.ConfigureByConvention();

            b.Property(x => x.ItemNameSnapshot).IsRequired().HasMaxLength(100);
            b.Property(x => x.ServiceNameSnapshot).IsRequired().HasMaxLength(100);
            b.Property(x => x.UnitPriceSnapshot).HasPrecision(18, 2);
            b.Property(x => x.LineTotal).HasPrecision(18, 2);

            b.HasIndex(x => x.OrderId);
        });

        // 12. OrderAdjustment
        builder.Entity<OrderAdjustment>(b =>
        {
            b.ToTable(tablePrefix + "OrderAdjustments", schema);
            b.ConfigureByConvention();

            b.Property(x => x.Reason).IsRequired().HasMaxLength(500);
            b.Property(x => x.OldTotal).HasPrecision(18, 2);
            b.Property(x => x.NewTotal).HasPrecision(18, 2);
            b.Property(x => x.DifferenceAmount).HasPrecision(18, 2);

            b.HasIndex(x => x.OrderId);
        });

        // 13. OrderStatusHistory
        builder.Entity<OrderStatusHistory>(b =>
        {
            b.ToTable(tablePrefix + "OrderStatusHistories", schema);
            b.ConfigureByConvention();

            b.Property(x => x.Reason).HasMaxLength(500);

            b.HasIndex(x => x.OrderId);
        });

        // 14. ProcessingStageHistory
        builder.Entity<ProcessingStageHistory>(b =>
        {
            b.ToTable(tablePrefix + "ProcessingStageHistories", schema);
            b.ConfigureByConvention();

            b.HasIndex(x => x.OrderId);
        });

        // 15. Driver
        builder.Entity<Driver>(b =>
        {
            b.ToTable(tablePrefix + "Drivers", schema);
            b.ConfigureByConvention();

            b.Property(x => x.VehicleDetails).HasMaxLength(200);

            b.HasIndex(x => new { x.TenantId, x.UserId }).IsUnique().HasFilter("[IsDeleted] = 0");
        });

        // 16. PickupTask
        builder.Entity<PickupTask>(b =>
        {
            b.ToTable(tablePrefix + "PickupTasks", schema);
            b.ConfigureByConvention();

            b.Property(x => x.FailureReason).HasMaxLength(500);

            b.HasIndex(x => new { x.TenantId, x.OrderId, x.AttemptNumber }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.OrderId })
                .HasFilter("[Status] IN (0, 1, 2, 3, 4)")
                .IsUnique();
            b.HasIndex(x => new { x.TenantId, x.DriverId, x.Status });
        });

        // 17. DeliveryTask
        builder.Entity<DeliveryTask>(b =>
        {
            b.ToTable(tablePrefix + "DeliveryTasks", schema);

            b.OwnsOne(x => x.CashCollectionInfo, c =>
            {
                c.Property(p => p.AmountToCollect).HasPrecision(18, 2);
                c.Property(p => p.CollectedAmount).HasPrecision(18, 2);
                c.Property(p => p.FailureReason).HasMaxLength(500);
            });

            b.ConfigureByConvention();

            b.Property(x => x.FailureReason).HasMaxLength(500);

            b.HasIndex(x => new { x.TenantId, x.OrderId, x.AttemptNumber }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.OrderId })
                .HasFilter("[Status] IN (0, 1, 2, 3, 4)")
                .IsUnique();
            b.HasIndex(x => new { x.TenantId, x.DriverId, x.Status });
        });

        // 18. Bag
        builder.Entity<Bag>(b =>
        {
            b.ToTable(tablePrefix + "Bags", schema);
            b.ConfigureByConvention();

            b.Property(x => x.BagNumber).IsRequired().HasMaxLength(50);
            b.Property(x => x.QrCode).IsRequired().HasMaxLength(100);

            b.HasMany(x => x.Events).WithOne().HasForeignKey(x => x.BagId).OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(x => new { x.TenantId, x.BagNumber }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.QrCode }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.OrderId });
        });

        // 19. BagEvent
        builder.Entity<BagEvent>(b =>
        {
            b.ToTable(tablePrefix + "BagEvents", schema);
            b.ConfigureByConvention();

            b.Property(x => x.Notes).HasMaxLength(500);

            b.HasIndex(x => x.BagId);
        });

        // 20. Inspection
        builder.Entity<Inspection>(b =>
        {
            b.ToTable(tablePrefix + "Inspections", schema);
            b.ConfigureByConvention();

            b.Property(x => x.Notes).HasMaxLength(1000);
            b.Property(x => x.ReopenReason).HasMaxLength(500);

            b.HasMany(x => x.Items).WithOne().HasForeignKey(x => x.InspectionId).OnDelete(DeleteBehavior.Restrict);
            b.HasMany(x => x.Damages).WithOne().HasForeignKey(x => x.InspectionId).OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(x => new { x.TenantId, x.OrderId }).IsUnique();
        });

        // 21. InspectionItem
        builder.Entity<InspectionItem>(b =>
        {
            b.ToTable(tablePrefix + "InspectionItems", schema);
            b.ConfigureByConvention();

            b.Property(x => x.Notes).HasMaxLength(500);

            b.HasIndex(x => x.InspectionId);
        });

        // 22. Damage
        builder.Entity<Damage>(b =>
        {
            b.ToTable(tablePrefix + "Damages", schema);
            b.ConfigureByConvention();

            b.Property(x => x.Description).IsRequired().HasMaxLength(500);
            b.Property(x => x.PhotoBlobName).IsRequired().HasMaxLength(500);

            b.HasIndex(x => x.InspectionId);
        });

        // 23. AppNotification (CreationAuditedAggregateRoot + IMultiTenant)
        builder.Entity<AppNotification>(b =>
        {
            b.ToTable(tablePrefix + "AppNotifications", schema);
            b.ConfigureByConvention();

            b.Property(x => x.Title).IsRequired().HasMaxLength(200);
            b.Property(x => x.Body).IsRequired().HasMaxLength(1000);
            b.Property(x => x.RelatedEntityType).HasMaxLength(100);

            b.HasIndex(x => new { x.TargetUserId, x.IsRead });
            b.HasIndex(x => x.TenantId);
        });

        // 24. Complaint
        builder.Entity<Complaint>(b =>
        {
            b.ToTable(tablePrefix + "Complaints", schema);

            b.OwnsOne(x => x.ResolutionInfo, r =>
            {
                r.Property(p => p.ResolutionNotes).HasMaxLength(1000);
                r.Property(p => p.CompensationAmount).HasPrecision(18, 2);
            });

            b.ConfigureByConvention();

            b.Property(x => x.ComplaintNumber).IsRequired().HasMaxLength(50);
            b.Property(x => x.Subject).IsRequired().HasMaxLength(200);
            b.Property(x => x.Description).IsRequired().HasMaxLength(2000);

            b.HasMany(x => x.Attachments).WithOne().HasForeignKey(x => x.ComplaintId).OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(x => new { x.TenantId, x.ComplaintNumber }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.OrderId });
            b.HasIndex(x => new { x.TenantId, x.CustomerId });
        });

        // 25. ComplaintAttachment
        builder.Entity<ComplaintAttachment>(b =>
        {
            b.ToTable(tablePrefix + "ComplaintAttachments", schema);
            b.ConfigureByConvention();

            b.Property(x => x.BlobName).IsRequired().HasMaxLength(500);
            b.Property(x => x.FileName).IsRequired().HasMaxLength(255);
            b.Property(x => x.ContentType).IsRequired().HasMaxLength(100);

            b.HasIndex(x => x.ComplaintId);
        });
    }
}
