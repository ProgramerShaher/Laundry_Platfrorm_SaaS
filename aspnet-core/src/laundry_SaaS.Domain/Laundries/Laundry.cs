using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using laundry_SaaS.Common;
using Volo.Abp;

namespace laundry_SaaS.Laundries;

/// <summary>
/// يمثل الجذر التجميعي (Aggregate Root) للمغسلة التابعة لمستأجر محدد (Tenant).
/// يعد الكيان الرئيسي الذي يمثل هوية المغسلة، وموقعها الجغرافي، وسياستها المالية والتشغيلية،
/// ويملك كلاً من ساعات العمل الرسمية والفترات الزمنية المجدولة للاستلام والتوصيل.
/// </summary>
public class Laundry : TenantFullAuditedAggregateRoot
{
    /// <summary>
    /// الاسم التجاري الرسمي للمغسلة، يظهر للعملاء في التطبيق وقوائم البحث.
    /// </summary>
    public string Name { get; private set; } = null!;

    /// <summary>
    /// نبذة تعريفية أو وصف للخدمات والمميزات الخاصة بالمغسلة.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// رقم الهاتف المعتمد للتواصل مع المغسلة والتنسيق التشغيلي.
    /// </summary>
    public string PhoneNumber { get; private set; } = null!;

    /// <summary>
    /// البريد الإلكتروني الرسمي للمراسلات وإشعارات المنصة.
    /// </summary>
    public string? Email { get; private set; }

    /// <summary>
    /// خط العرض الجغرافي للموقع الفعلي للمغسلة على الخريطة.
    /// </summary>
    public double Latitude { get; private set; }

    /// <summary>
    /// خط الطول الجغرافي للموقع الفعلي للمغسلة على الخريطة.
    /// </summary>
    public double Longitude { get; private set; }

    /// <summary>
    /// حالة تفعيل المغسلة في النظام؛ إذا كانت غير مفعلة يتم إخفاؤها من التطبيق وقوائم البحث.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// مفتاح تحكم تشغيلي فوري يتيح للمغسلة إيقاف استقبال طلبات جديدة مؤقتاً عند الازدحام.
    /// </summary>
    public bool AcceptingOrders { get; private set; }

    /// <summary>
    /// رسوم التوصيل الافتراضية المعتمدة للمغسلة (تُسجل بدقة decimal 18,2).
    /// </summary>
    public decimal DeliveryFee { get; private set; }

    /// <summary>
    /// الحد الأدنى لقيمة مشتريات الطلب قبل احتساب رسوم التوصيل والضرائب.
    /// </summary>
    public decimal MinimumOrderAmount { get; private set; }

    /// <summary>
    /// الزمن التقديري القياسي اللازم لإنهاء معالجة الطلب وغسيله بالساعات (مثل 24 أو 48 ساعة).
    /// </summary>
    public int EstimatedProcessingHours { get; private set; }

    /// <summary>
    /// اسم المرجع (Blob Name) لصورة الشعار في وحدة التخزين السحابية (لا تُخزن الصور بصيغة ثنائية في قاعدة البيانات).
    /// </summary>
    public string? LogoBlobName { get; private set; }

    /// <summary>
    /// كائن القيمة المدمج (Value Object) الذي يحدد نطاق التغطية الجغرافية وشروط المسافة للمغسلة.
    /// </summary>
    public CoverageArea CoverageArea { get; private set; } = null!;

    /// <summary>
    /// مجموعة الكيانات التابعة (Child Entities) التي تحدد ساعات العمل وأيام الفتح والإغلاق لكل يوم من أيام الأسبوع.
    /// </summary>
    public virtual ICollection<LaundryWorkingHour> WorkingHours { get; protected set; }

    /// <summary>
    /// مجموعة الكيانات التابعة (Child Entities) التي تمثل الفترات الزمنية المتاحة لجدولة الاستلام والتوصيل.
    /// </summary>
    public virtual ICollection<LaundryTimeSlot> TimeSlots { get; protected set; }

    /// <summary>
    /// مُنشئ محمي خالي من المعاملات مخصص لـ Entity Framework Core.
    /// </summary>
    private Laundry()
    {
        WorkingHours = new Collection<LaundryWorkingHour>();
        TimeSlots = new Collection<LaundryTimeSlot>();
    }

    /// <summary>
    /// يُنشئ سجلاً جديداً لمغسلة متكاملة تابعة لمستأجر محدد.
    /// </summary>
    /// <param name="id">المعرّف الفريد للكيان.</param>
    /// <param name="tenantId">معرّف المستأجر المالك.</param>
    /// <param name="name">اسم المغسلة التجاري.</param>
    /// <param name="phoneNumber">رقم هاتف المغسلة.</param>
    /// <param name="latitude">خط العرض للموقع الفعلي.</param>
    /// <param name="longitude">خط الطول للموقع الفعلي.</param>
    /// <param name="coverageArea">كائن القيمة الذي يحدد نطاق التغطية.</param>
    /// <param name="deliveryFee">رسوم التوصيل الأساسية.</param>
    /// <param name="minimumOrderAmount">الحد الأدنى لقيمة الطلب.</param>
    /// <param name="estimatedProcessingHours">الزمن التقديري بالساعات للمعالجة.</param>
    /// <param name="description">وصف اختياري لنشاط المغسلة.</param>
    /// <param name="email">بريد إلكتروني رسمي اختياري.</param>
    /// <param name="logoBlobName">اسم ملف الشعار المخزن في السحابة.</param>
    /// <param name="isActive">حالة التفعيل الأولية.</param>
    /// <param name="acceptingOrders">حالة استقبال الطلبات الأولية.</param>
    public Laundry(
        Guid id,
        Guid tenantId,
        string name,
        string phoneNumber,
        double latitude,
        double longitude,
        CoverageArea coverageArea,
        decimal deliveryFee = 0,
        decimal minimumOrderAmount = 0,
        int estimatedProcessingHours = 24,
        string? description = null,
        string? email = null,
        string? logoBlobName = null,
        bool isActive = true,
        bool acceptingOrders = true)
        : base(id, tenantId)
    {
        SetName(name);
        SetPhoneNumber(phoneNumber);
        GeoLocationValidator.ValidateCoordinates(latitude, longitude);
        Latitude = latitude;
        Longitude = longitude;
        CoverageArea = Check.NotNull(coverageArea, nameof(coverageArea));
        SetFees(deliveryFee, minimumOrderAmount);

        if (estimatedProcessingHours <= 0)
        {
            throw new BusinessException("EstimatedProcessingHours must be greater than zero.");
        }
        EstimatedProcessingHours = estimatedProcessingHours;

        Description = description;
        Email = email;
        LogoBlobName = logoBlobName;
        IsActive = isActive;
        AcceptingOrders = acceptingOrders;

        WorkingHours = new Collection<LaundryWorkingHour>();
        TimeSlots = new Collection<LaundryTimeSlot>();
    }

    /// <summary>
    /// يعدل الاسم التجاري للمغسلة مع التحقق من عدم فراغه ومطابقته للحد الأقصى للطول.
    /// </summary>
    /// <param name="name">الاسم الجديد.</param>
    public void SetName(string name)
    {
        Name = Check.NotNullOrWhiteSpace(name, nameof(name), maxLength: 200);
    }

    /// <summary>
    /// يعدل رقم هاتف المغسلة مع التحقق من صحته وطوله المسموح.
    /// </summary>
    /// <param name="phoneNumber">رقم الهاتف الجديد.</param>
    public void SetPhoneNumber(string phoneNumber)
    {
        PhoneNumber = Check.NotNullOrWhiteSpace(phoneNumber, nameof(phoneNumber), maxLength: 30);
    }

    /// <summary>
    /// يحدد رسوم التوصيل والحد الأدنى للطلب ويتحقق من عدم كونهما قيماً سالبة.
    /// </summary>
    /// <param name="deliveryFee">رسوم التوصيل الجديدة.</param>
    /// <param name="minimumOrderAmount">الحد الأدنى الجديد للطلب.</param>
    /// <exception cref="BusinessException">يتم رميها إذا كانت أي من القيم سالبة.</exception>
    public void SetFees(decimal deliveryFee, decimal minimumOrderAmount)
    {
        if (deliveryFee < 0 || minimumOrderAmount < 0)
        {
            throw new BusinessException("Fees and minimum order amount must be greater than or equal to zero.");
        }
        DeliveryFee = deliveryFee;
        MinimumOrderAmount = minimumOrderAmount;
    }

    /// <summary>
    /// يحدّث كائن القيمة الخاص بنطاق التغطية الجغرافية والتشغيلية للمغسلة.
    /// </summary>
    /// <param name="coverageArea">كائن القيمة الجديد.</param>
    public void SetCoverageArea(CoverageArea coverageArea)
    {
        CoverageArea = Check.NotNull(coverageArea, nameof(coverageArea));
    }

    /// <summary>
    /// يغير حالة تفعيل المغسلة في النظام.
    /// </summary>
    /// <param name="isActive">القيمة الجديدة للتفعيل.</param>
    public void SetActive(bool isActive)
    {
        IsActive = isActive;
    }

    /// <summary>
    /// يغير حالة استقبال الطلبات الجديدة مؤقتاً أو دائماً.
    /// </summary>
    /// <param name="acceptingOrders">القيمة الجديدة لاستقبال الطلبات.</param>
    public void SetAcceptingOrders(bool acceptingOrders)
    {
        AcceptingOrders = acceptingOrders;
    }

    /// <summary>
    /// يحدّث مرجع اسم ملف الشعار في التخزين السحابي.
    /// </summary>
    /// <param name="logoBlobName">اسم المرجع الجديد في التخزين السحابي.</param>
    public void SetLogo(string? logoBlobName)
    {
        LogoBlobName = logoBlobName;
    }

    /// <summary>
    /// يحدد أو يحدّث ساعات عمل المغسلة ليوم محدد من أيام الأسبوع،
    /// مع التحقق الصارم من عدم تعارض المواعيد الجديدة مع أي فترات زمنية نشطة (<see cref="LaundryTimeSlot"/>) موجودة مسبقاً لهذا اليوم.
    /// </summary>
    /// <param name="dayOfWeek">يوم الأسبوع المراد ضبط ساعات عمله.</param>
    /// <param name="isOpen">ما إذا كانت المغسلة مفتوحة في هذا اليوم.</param>
    /// <param name="openTime">وقت بدء العمل اليومي (إلزامي إذا كان اليوم مفتوحاً).</param>
    /// <param name="closeTime">وقت انتهاء العمل اليومي (إلزامي إذا كان اليوم مفتوحاً).</param>
    /// <returns>كيان ساعات العمل الذي تم إنشاؤه أو تحديثه.</returns>
    /// <exception cref="BusinessException">يتم رميها إذا تم إغلاق يوم يحتوي على فترات نشطة أو إذا كانت المواعيد الجديدة تخرج الفترات النشطة عن النطاق المسموح.</exception>
    public LaundryWorkingHour SetWorkingHour(
        DayOfWeek dayOfWeek,
        bool isOpen,
        TimeOnly? openTime = null,
        TimeOnly? closeTime = null)
    {
        var activeSlotsOnDay = TimeSlots.Where(s => s.DayOfWeek == dayOfWeek && s.IsActive).ToList();

        if (!isOpen && activeSlotsOnDay.Any())
        {
            throw new BusinessException($"Cannot set {dayOfWeek} to closed because there are {activeSlotsOnDay.Count} active time slot(s) assigned to this day. Disable or remove them first.");
        }

        if (isOpen)
        {
            if (openTime == null || closeTime == null)
            {
                throw new BusinessException("OpenTime and CloseTime must be provided when day is open.");
            }

            foreach (var slot in activeSlotsOnDay)
            {
                if (slot.StartTime < openTime.Value || slot.EndTime > closeTime.Value)
                {
                    throw new BusinessException($"Cannot change working hours for {dayOfWeek} to [{openTime.Value}-{closeTime.Value}] because existing active slot [{slot.StartTime}-{slot.EndTime}] falls outside this range.");
                }
            }
        }

        var workingHour = WorkingHours.FirstOrDefault(w => w.DayOfWeek == dayOfWeek);
        if (workingHour == null)
        {
            workingHour = new LaundryWorkingHour(Guid.NewGuid(), Id, dayOfWeek, isOpen, openTime, closeTime);
            WorkingHours.Add(workingHour);
        }
        else
        {
            workingHour.SetSchedule(isOpen, openTime, closeTime);
        }

        return workingHour;
    }

    /// <summary>
    /// يضيف فترة زمنية مجدولة جديدة للاستلام أو التوصيل تابعة للمغسلة،
    /// مع التحقق الصارم من أن اليوم مفتوح، وأن الفترة تقع بالكامل داخل ساعات عمل المغسلة المعتمدة لذلك اليوم،
    /// ولا تتداخل (Overlap) مع أي فترة نشطة أخرى من نفس النوع في نفس اليوم.
    /// </summary>
    /// <param name="id">المعرّف الفريد للفترة الزمنية.</param>
    /// <param name="dayOfWeek">يوم الأسبوع المخصص للفترة.</param>
    /// <param name="slotType">نوع الفترة (استلام Pickup أو توصيل Delivery).</param>
    /// <param name="startTime">وقت بدء الفترة الزمنية.</param>
    /// <param name="endTime">وقت انتهاء الفترة الزمنية.</param>
    /// <param name="isActive">حالة تفعيل الفترة الأولية.</param>
    /// <returns>كيان الفترة الزمنية المُنشأ والمضاف.</returns>
    /// <exception cref="BusinessException">يتم رميها إذا كان اليوم مغلقاً، أو كانت المواعيد خارج ساعات العمل، أو كان هناك تداخل مع فترة نشطة أخرى.</exception>
    public LaundryTimeSlot AddTimeSlot(
        Guid id,
        DayOfWeek dayOfWeek,
        SlotType slotType,
        TimeOnly startTime,
        TimeOnly endTime,
        bool isActive = true)
    {
        if (startTime >= endTime)
        {
            throw new BusinessException("StartTime must be earlier than EndTime.");
        }

        var workingHour = WorkingHours.FirstOrDefault(w => w.DayOfWeek == dayOfWeek);
        if (workingHour == null || !workingHour.IsOpen)
        {
            throw new BusinessException($"Cannot add time slot on {dayOfWeek} because the laundry is closed or working hours are not defined for this day.");
        }

        if (startTime < workingHour.OpenTime!.Value || endTime > workingHour.CloseTime!.Value)
        {
            throw new BusinessException($"Time slot [{startTime}-{endTime}] is outside the laundry working hours [{workingHour.OpenTime!.Value}-{workingHour.CloseTime!.Value}] on {dayOfWeek}.");
        }

        if (isActive)
        {
            var hasOverlap = TimeSlots.Any(s =>
                s.DayOfWeek == dayOfWeek &&
                s.SlotType == slotType &&
                s.IsActive &&
                startTime < s.EndTime &&
                endTime > s.StartTime);

            if (hasOverlap)
            {
                throw new BusinessException($"Active time slot [{startTime}-{endTime}] for {slotType} on {dayOfWeek} overlaps with an existing active time slot.");
            }
        }

        var slot = new LaundryTimeSlot(id, Id, dayOfWeek, slotType, startTime, endTime, isActive);
        TimeSlots.Add(slot);
        return slot;
    }

    /// <summary>
    /// يتحقق من صحة وصلاحية فترة استلام محددة لطلب ما بناءً على حالة المغسلة وساعات عمل اليوم وتاريخ الاستلام المطلوب،
    /// ويرجع كائن الفترة الزمنية المعتمدة لتمكين طبقة التطبيق من إنشاء اللقطة التاريخية (<see cref="laundry_SaaS.Orders.PickupScheduleSnapshot"/>).
    /// </summary>
    /// <param name="slotId">المعرّف الفريد للفترة الزمنية المطلوبة.</param>
    /// <param name="pickupDate">تاريخ الاستلام التقويمي المطلوب.</param>
    /// <param name="currentDateTime">التوقيت واللحظة الزمنية الحالية المعتمدة (عبر IClock أو التوقيت الموثوق).</param>
    /// <returns>كيان الفترة الزمنية المعتمد بعد اجتياز كافة قيود التحقق.</returns>
    /// <exception cref="BusinessException">يتم رميها إذا كانت المغسلة غير نشطة، أو لا تستقبل طلبات، أو كانت الفترة غير صالحة أو غير نشطة أو لا تطابق اليوم أو منتهية.</exception>
    public LaundryTimeSlot ValidateAndGetPickupSlot(
        Guid slotId,
        DateOnly pickupDate,
        DateTime currentDateTime)
    {
        if (slotId == Guid.Empty)
        {
            throw new BusinessException(
                laundry_SaaSDomainErrorCodes.LaundryErrorCodes.InvalidPickupSlot,
                "SlotId must not be empty.");
        }

        if (!IsActive)
        {
            throw new BusinessException(
                laundry_SaaSDomainErrorCodes.LaundryErrorCodes.LaundryClosedOrInactive,
                "Laundry is currently inactive.");
        }

        if (!AcceptingOrders)
        {
            throw new BusinessException(
                laundry_SaaSDomainErrorCodes.LaundryErrorCodes.LaundryNotAcceptingOrders,
                "Laundry is not accepting new orders at this time.");
        }

        var slot = TimeSlots.FirstOrDefault(s => s.Id == slotId);
        if (slot == null)
        {
            throw new BusinessException(
                laundry_SaaSDomainErrorCodes.LaundryErrorCodes.InvalidPickupSlot,
                $"Pickup slot with id '{slotId}' does not exist in this laundry.");
        }

        if (slot.SlotType != SlotType.Pickup)
        {
            throw new BusinessException(
                laundry_SaaSDomainErrorCodes.LaundryErrorCodes.InvalidPickupSlot,
                $"Time slot '{slotId}' is configured as '{slot.SlotType}', but must be 'Pickup'.");
        }

        if (!slot.IsActive)
        {
            throw new BusinessException(
                laundry_SaaSDomainErrorCodes.LaundryErrorCodes.PickupSlotNotActive,
                $"Pickup slot '{slotId}' is not active.");
        }

        if (pickupDate.DayOfWeek != slot.DayOfWeek)
        {
            throw new BusinessException(
                laundry_SaaSDomainErrorCodes.LaundryErrorCodes.PickupSlotDateMismatch,
                $"Pickup date '{pickupDate}' falls on {pickupDate.DayOfWeek}, which does not match the slot's scheduled day {slot.DayOfWeek}.");
        }

        var currentDate = DateOnly.FromDateTime(currentDateTime);
        if (pickupDate < currentDate)
        {
            throw new BusinessException(
                laundry_SaaSDomainErrorCodes.LaundryErrorCodes.PickupDateInPast,
                $"Pickup date '{pickupDate}' is in the past. Current date is '{currentDate}'.");
        }

        var workingHour = WorkingHours.FirstOrDefault(w => w.DayOfWeek == pickupDate.DayOfWeek);
        if (workingHour == null || !workingHour.IsOpen)
        {
            throw new BusinessException(
                laundry_SaaSDomainErrorCodes.LaundryErrorCodes.LaundryClosedOrInactive,
                $"Laundry is closed or has no operating hours on {pickupDate.DayOfWeek}.");
        }

        if (slot.StartTime < workingHour.OpenTime!.Value || slot.EndTime > workingHour.CloseTime!.Value)
        {
            throw new BusinessException(
                laundry_SaaSDomainErrorCodes.LaundryErrorCodes.InvalidPickupSlot,
                $"Time slot [{slot.StartTime}-{slot.EndTime}] falls outside the operating hours [{workingHour.OpenTime!.Value}-{workingHour.CloseTime!.Value}] on {pickupDate.DayOfWeek}.");
        }

        // Same-day check: Cannot select a slot that has already ended
        if (pickupDate == currentDate)
        {
            var currentTime = TimeOnly.FromDateTime(currentDateTime);
            if (currentTime >= slot.EndTime)
            {
                throw new BusinessException(
                    laundry_SaaSDomainErrorCodes.LaundryErrorCodes.PickupSlotExpired,
                    $"Cannot select pickup slot [{slot.StartTime}-{slot.EndTime}] because it has already ended for today. Current time is '{currentTime}'.");
            }
        }

        return slot;
    }
}
