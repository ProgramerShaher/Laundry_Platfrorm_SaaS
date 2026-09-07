namespace laundry_SaaS;

/// <summary>
/// ثوابت أكواد أخطاء الأعمال (Domain Error Codes) المعتمدة في النظام.
/// تتبع صيغة المعيار: laundry_SaaS:ModuleName:CodeNumber
/// وتُستخدم عند رمي استثناءات الأعمال من نوع BusinessException لتقديم رسائل خطأ مترجمة وموحدة للواجهات الأمامية.
/// </summary>
public static class laundry_SaaSDomainErrorCodes
{
    /*
     * Base Pattern Convention for Business Exceptions:
     * Format: laundry_SaaS:<ModuleName>:<CodeNumber>
     * 
     * Examples:
     * - laundry_SaaS:Orders:001
     * - laundry_SaaS:Pickup:001
     * - laundry_SaaS:Processing:001
     * - laundry_SaaS:Customer:001
     * - laundry_SaaS:Laundry:001
     * 
     * Each module owner will declare their specific error codes within their module scope.
     */

    /// <summary>
    /// البادئة الموحدة لجميع أكواد أخطاء النظام، تفيد في تمييز أخطاء نطاق العمل عن أخطاء البنية التحتية.
    /// </summary>
    public const string Prefix = "laundry_SaaS";

    /// <summary>
    /// رموز أخطاء نطاق مهام التوصيل ورمز التحقق (Delivery Task &amp; OTP Error Codes).
    /// </summary>
    public static class DeliveryTaskErrorCodes
    {
        public const string Prefix = laundry_SaaSDomainErrorCodes.Prefix + ":DeliveryTask:";

        /// <summary>
        /// رمز التحقق غير متوفر للمهمة الحالية.
        /// </summary>
        public const string OtpNotAvailable = Prefix + "001";

        /// <summary>
        /// انتهت صلاحية رمز التحقق.
        /// </summary>
        public const string OtpExpired = Prefix + "002";

        /// <summary>
        /// تم تجاوز الحد الأقصى لمحاولات إدخال الرمز الخاطئة.
        /// </summary>
        public const string OtpMaxAttemptsReached = Prefix + "003";

        /// <summary>
        /// فترة الانتظار (Cooldown) بين طلبات الرمز لم تنتهِ بعد.
        /// </summary>
        public const string OtpResendCooldown = Prefix + "004";

        /// <summary>
        /// تم الوصول إلى الحد الأقصى لمرات توليد الرمز.
        /// </summary>
        public const string OtpMaxGenerationsReached = Prefix + "005";

        /// <summary>
        /// لم يتم التحقق من رمز التسليم بنجاح بعد.
        /// </summary>
        public const string DeliveryOtpNotVerified = Prefix + "006";

        /// <summary>
        /// حالة المهمة الحالية لا تسمح بالعملية المطلوبة لرمز التحقق.
        /// </summary>
        public const string InvalidStatusForOtp = Prefix + "007";
    }

    /// <summary>
    /// رموز أخطاء نطاق المغسلة ومطابقة الفترات الزمنية (Laundry &amp; Pickup Slot Error Codes).
    /// </summary>
    public static class LaundryErrorCodes
    {
        public const string Prefix = laundry_SaaSDomainErrorCodes.Prefix + ":Laundry:";

        /// <summary>
        /// فترة الاستلام غير موجودة أو غير صالحة للمغسلة.
        /// </summary>
        public const string InvalidPickupSlot = Prefix + "001";

        /// <summary>
        /// فترة الاستلام غير مفعلة حالياً.
        /// </summary>
        public const string PickupSlotNotActive = Prefix + "002";

        /// <summary>
        /// يوم الأسبوع لتاريخ الاستلام لا يطابق يوم الفترة الزمنية.
        /// </summary>
        public const string PickupSlotDateMismatch = Prefix + "003";

        /// <summary>
        /// انتهت الفترة الزمنية لليوم الحالي بالفعل ولا يمكن حجزها.
        /// </summary>
        public const string PickupSlotExpired = Prefix + "004";

        /// <summary>
        /// تاريخ الاستلام في الماضي.
        /// </summary>
        public const string PickupDateInPast = Prefix + "005";

        /// <summary>
        /// المغسلة غير مفعلة أو مغلقة في هذا اليوم.
        /// </summary>
        public const string LaundryClosedOrInactive = Prefix + "006";

        /// <summary>
        /// المغسلة لا تستقبل طلبات جديدة حالياً.
        /// </summary>
        public const string LaundryNotAcceptingOrders = Prefix + "007";
    }

    /// <summary>
    /// رموز أخطاء نطاق الطلبات والأسعار (Order &amp; Pricing Error Codes).
    /// </summary>
    public static class OrderErrorCodes
    {
        public const string Prefix = laundry_SaaSDomainErrorCodes.Prefix + ":Orders:";

        /// <summary>
        /// جدول وموعد الاستلام إلزامي ولا يمكن إنشاء الطلب بدونه.
        /// </summary>
        public const string PickupScheduleRequired = Prefix + "001";

        /// <summary>
        /// لا يوجد تسعير مفعل ونشط (ServicePrice) لتركيبة الصنف والخدمة المفحوصة.
        /// </summary>
        public const string ServicePriceNotFoundForInspectedItem = Prefix + "002";
    }

    /// <summary>
    /// رموز أخطاء نطاق الفحص والمعاينة الفنية (Inspection Error Codes).
    /// </summary>
    public static class InspectionErrorCodes
    {
        public const string Prefix = laundry_SaaSDomainErrorCodes.Prefix + ":Inspection:";

        /// <summary>
        /// لا يمكن تكرار تمثيل بند الطلب الأصلي في محضر الفحص أكثر من مرة.
        /// </summary>
        public const string DuplicateOrderItemRepresentation = Prefix + "001";

        /// <summary>
        /// لا يمكن اعتماد محضر الفحص إلا بعد تمثيل وفحص كافة بنود الطلب الأصلية.
        /// </summary>
        public const string IncompleteOrderItemsInspected = Prefix + "002";

        /// <summary>
        /// البند الإضافي يجب أن يحدد نوع القطعة ونوع الخدمة الفعلية وكمية موجبة.
        /// </summary>
        public const string InvalidAdditionalItem = Prefix + "003";
    }
}

