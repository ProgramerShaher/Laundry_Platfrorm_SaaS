using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Data;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.MultiTenancy;
using Volo.Abp.TenantManagement;
using laundry_SaaS.Complaints;
using laundry_SaaS.Customers;
using laundry_SaaS.Laundries;
using laundry_SaaS.Orders;
using laundry_SaaS.Permissions;
using laundry_SaaS.PickupDelivery;

namespace laundry_SaaS.HostManagement;

/// <summary>
/// تطبيق خدمة إدارة المضيف المركزي للمنصة (Host Platform Management App Service).
/// يتيح لإدارة المنصة المركزية إنشاء المستأجرين، إدارة دورة حياة المغاسل، ومتابعة إحصائيات النظام.
/// </summary>
[Authorize]
public class LaundryHostAppService : laundry_SaaSAppService, ILaundryHostAppService
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantManager _tenantManager;
    private readonly IDataSeeder _dataSeeder;
    private readonly IDataFilter _dataFilter;
    private readonly IRepository<Laundry, Guid> _laundryRepository;
    private readonly IRepository<Order, Guid> _orderRepository;
    private readonly IRepository<Customer, Guid> _customerRepository;
    private readonly IRepository<Driver, Guid> _driverRepository;
    private readonly IRepository<Complaint, Guid> _complaintRepository;

    public LaundryHostAppService(
        ITenantRepository tenantRepository,
        ITenantManager tenantManager,
        IDataSeeder dataSeeder,
        IDataFilter dataFilter,
        IRepository<Laundry, Guid> laundryRepository,
        IRepository<Order, Guid> orderRepository,
        IRepository<Customer, Guid> customerRepository,
        IRepository<Driver, Guid> driverRepository,
        IRepository<Complaint, Guid> complaintRepository)
    {
        _tenantRepository = tenantRepository;
        _tenantManager = tenantManager;
        _dataSeeder = dataSeeder;
        _dataFilter = dataFilter;
        _laundryRepository = laundryRepository;
        _orderRepository = orderRepository;
        _customerRepository = customerRepository;
        _driverRepository = driverRepository;
        _complaintRepository = complaintRepository;
    }

    /// <summary>
    /// يُنشئ مستأجر مغسلة جديد في النظام مع إنشاء المستخدم المدير الأول وإعداد الكيان التشغيلي للمغسلة.
    /// </summary>
    [Authorize(laundry_SaaSPermissions.Host.ManageLaundries)]
    public virtual async Task<LaundryHostDetailDto> CreateLaundryTenantAsync(CreateLaundryTenantInput input)
    {
        EnsureHostContext();

        var existingTenant = await _tenantRepository.FindByNameAsync(input.TenantName);
        if (existingTenant != null)
        {
            throw new BusinessException(
                laundry_SaaSDomainErrorCodes.Prefix + ":TenantAlreadyExists",
                $"Tenant with name '{input.TenantName}' already exists.");
        }

        var tenant = await _tenantManager.CreateAsync(input.TenantName);
        await _tenantRepository.InsertAsync(tenant, autoSave: true);

        Laundry laundry;
        using (CurrentTenant.Change(tenant.Id))
        {
            await _dataSeeder.SeedAsync(new DataSeedContext(tenant.Id)
                .WithProperty(IdentityDataSeedContributor.AdminEmailPropertyName, input.AdminEmail)
                .WithProperty(IdentityDataSeedContributor.AdminPasswordPropertyName, input.AdminPassword));

            var coverageArea = new CoverageArea(
                input.Latitude,
                input.Longitude,
                input.RadiusKm,
                input.MinimumOrderAmount);

            laundry = new Laundry(
                GuidGenerator.Create(),
                tenant.Id,
                input.LaundryName,
                input.PhoneNumber,
                input.Latitude,
                input.Longitude,
                coverageArea,
                deliveryFee: input.DeliveryFee,
                minimumOrderAmount: input.MinimumOrderAmount,
                estimatedProcessingHours: input.EstimatedProcessingHours,
                email: input.AdminEmail,
                isActive: true,
                acceptingOrders: true);

            await _laundryRepository.InsertAsync(laundry, autoSave: true);
        }

        return new LaundryHostDetailDto
        {
            Id = laundry.Id,
            TenantId = tenant.Id,
            TenantName = tenant.Name,
            Name = laundry.Name,
            PhoneNumber = laundry.PhoneNumber,
            Email = laundry.Email,
            Description = laundry.Description,
            Latitude = laundry.Latitude,
            Longitude = laundry.Longitude,
            IsActive = laundry.IsActive,
            AcceptingOrders = laundry.AcceptingOrders,
            DeliveryFee = laundry.DeliveryFee,
            MinimumOrderAmount = laundry.MinimumOrderAmount,
            EstimatedProcessingHours = laundry.EstimatedProcessingHours,
            TotalOrdersCount = 0,
            CreationTime = laundry.CreationTime
        };
    }

    /// <summary>
    /// يسترجع تفاصيل مغسلة محددة لإدارة المضيف بالمعرّف مع بيانات المستأجر وإجمالي الطلبات.
    /// </summary>
    [Authorize(laundry_SaaSPermissions.Host.ViewLaundries)]
    public virtual async Task<LaundryHostDetailDto> GetAsync(Guid id)
    {
        EnsureHostContext();

        using (_dataFilter.Disable<IMultiTenant>())
        {
            var laundry = await _laundryRepository.FindAsync(id);
            if (laundry == null)
            {
                throw new EntityNotFoundException(typeof(Laundry), id);
            }

            var tenant = laundry.TenantId.HasValue ? await _tenantRepository.FindAsync(laundry.TenantId.Value) : null;
            var ordersCount = await _orderRepository.CountAsync(o => o.LaundryId == id);

            return new LaundryHostDetailDto
            {
                Id = laundry.Id,
                TenantId = laundry.TenantId.GetValueOrDefault(),
                TenantName = tenant?.Name ?? string.Empty,
                Name = laundry.Name,
                PhoneNumber = laundry.PhoneNumber,
                Email = laundry.Email,
                Description = laundry.Description,
                Latitude = laundry.Latitude,
                Longitude = laundry.Longitude,
                IsActive = laundry.IsActive,
                AcceptingOrders = laundry.AcceptingOrders,
                DeliveryFee = laundry.DeliveryFee,
                MinimumOrderAmount = laundry.MinimumOrderAmount,
                EstimatedProcessingHours = laundry.EstimatedProcessingHours,
                TotalOrdersCount = ordersCount,
                CreationTime = laundry.CreationTime
            };
        }
    }

    /// <summary>
    /// يسترجع قائمة مفهرسة ومقسمة لصفحات بجميع المغاسل المسجلة في المنصة لإدارة المضيف.
    /// </summary>
    [Authorize(laundry_SaaSPermissions.Host.ViewLaundries)]
    public virtual async Task<PagedResultDto<LaundryHostListDto>> GetListAsync(PagedAndSortedResultRequestDto input)
    {
        EnsureHostContext();

        using (_dataFilter.Disable<IMultiTenant>())
        {
            var totalCount = await _laundryRepository.GetCountAsync();
            var query = await _laundryRepository.GetQueryableAsync();

            query = !string.IsNullOrWhiteSpace(input.Sorting)
                ? query.OrderBy(input.Sorting)
                : query.OrderByDescending(l => l.CreationTime);

            query = query.PageBy(input.SkipCount, input.MaxResultCount);
            var laundries = await AsyncExecuter.ToListAsync(query);

            var tenantIds = laundries.Where(l => l.TenantId.HasValue).Select(l => l.TenantId!.Value).Distinct().ToList();
            var tenantDict = new Dictionary<Guid, string>();
            foreach (var tId in tenantIds)
            {
                var t = await _tenantRepository.FindAsync(tId);
                if (t != null)
                {
                    tenantDict[t.Id] = t.Name;
                }
            }

            var items = laundries.Select(l => new LaundryHostListDto
            {
                Id = l.Id,
                TenantId = l.TenantId.GetValueOrDefault(),
                TenantName = l.TenantId.HasValue && tenantDict.TryGetValue(l.TenantId.Value, out var tName) ? tName : string.Empty,
                Name = l.Name,
                PhoneNumber = l.PhoneNumber,
                IsActive = l.IsActive,
                AcceptingOrders = l.AcceptingOrders,
                CreationTime = l.CreationTime
            }).ToList();

            return new PagedResultDto<LaundryHostListDto>(totalCount, items);
        }
    }

    /// <summary>
    /// يغير حالة تفعيل المغسلة في المنصة (تنشيط أو إيقاف إداري).
    /// </summary>
    [Authorize(laundry_SaaSPermissions.Host.ManageLaundries)]
    public virtual async Task SetActiveStatusAsync(Guid id, bool isActive)
    {
        EnsureHostContext();

        using (_dataFilter.Disable<IMultiTenant>())
        {
            var laundry = await _laundryRepository.GetAsync(id);
            laundry.SetActive(isActive);
            await _laundryRepository.UpdateAsync(laundry, autoSave: true);
        }
    }

    /// <summary>
    /// ينشط مستأجر المغسلة ومغسلته تشغيلياً على مستوى المنصة لاستقبال الطلبات.
    /// </summary>
    [Authorize(laundry_SaaSPermissions.Host.ManageLaundries)]
    public virtual async Task ActivateLaundryTenantAsync(Guid id)
    {
        EnsureHostContext();

        using (_dataFilter.Disable<IMultiTenant>())
        {
            var laundry = await _laundryRepository.GetAsync(id);
            laundry.SetActive(true);
            laundry.SetAcceptingOrders(true);
            await _laundryRepository.UpdateAsync(laundry, autoSave: true);
        }
    }

    /// <summary>
    /// يوقف مؤقتاً مستأجر المغسلة ومغسلته عن العمل من قبل إدارة المضيف (Suspend).
    /// </summary>
    [Authorize(laundry_SaaSPermissions.Host.ManageLaundries)]
    public virtual async Task SuspendLaundryTenantAsync(Guid id)
    {
        EnsureHostContext();

        using (_dataFilter.Disable<IMultiTenant>())
        {
            var laundry = await _laundryRepository.GetAsync(id);
            laundry.SetActive(false);
            laundry.SetAcceptingOrders(false);
            await _laundryRepository.UpdateAsync(laundry, autoSave: true);
        }
    }

    /// <summary>
    /// يسترجع الإحصائيات العامة الشاملة لمنصة المغاسل (أعداد، إيرادات، طلبات نشطة، شكاوى).
    /// </summary>
    [Authorize(laundry_SaaSPermissions.Host.ViewStatistics)]
    public virtual async Task<PlatformStatisticsDto> GetPlatformStatisticsAsync()
    {
        EnsureHostContext();

        using (_dataFilter.Disable<IMultiTenant>())
        {
            var totalLaundries = await _laundryRepository.CountAsync();
            var activeLaundries = await _laundryRepository.CountAsync(l => l.IsActive);
            var totalCustomers = await _customerRepository.CountAsync();
            var totalDrivers = await _driverRepository.CountAsync();
            var totalOrders = await _orderRepository.CountAsync();
            var activeOrders = await _orderRepository.CountAsync(o => o.Status != OrderStatus.Completed && o.Status != OrderStatus.Cancelled);
            var completedOrders = await _orderRepository.CountAsync(o => o.Status == OrderStatus.Completed);

            var orderQuery = await _orderRepository.GetQueryableAsync();
            var completedOrdersQuery = orderQuery.Where(o => o.Status == OrderStatus.Completed);
            var totalRevenue = await AsyncExecuter.SumAsync(completedOrdersQuery.Select(o => o.Total));

            var pendingComplaints = await _complaintRepository.CountAsync(c => c.Status == ComplaintStatus.Open || c.Status == ComplaintStatus.InReview);

            return new PlatformStatisticsDto
            {
                TotalLaundriesCount = (int)totalLaundries,
                ActiveLaundriesCount = (int)activeLaundries,
                TotalCustomersCount = totalCustomers,
                TotalDriversCount = (int)totalDrivers,
                TotalOrdersCount = totalOrders,
                ActiveOrdersCount = activeOrders,
                CompletedOrdersCount = completedOrders,
                TotalRevenueAmount = totalRevenue,
                TotalCodCollectedAmount = totalRevenue,
                PendingComplaintsCount = (int)pendingComplaints
            };
        }
    }

    private void EnsureHostContext()
    {
        if (CurrentTenant.Id.HasValue)
        {
            throw new BusinessException(
                laundry_SaaSDomainErrorCodes.Prefix + ":HostOnly",
                "Host platform operations cannot be executed within a tenant context.");
        }
    }
}
