namespace laundry_SaaS.Permissions;

/// <summary>
/// ثوابت أسماء ومجموعات الصلاحيات (Permissions) المعتمدة في النظام.
/// <para>
/// تتبع تسمية الصلاحيات صيغة المعيار: laundry_SaaS.ModuleName.Action
/// وتُستخدم للتحقق من صلاحيات المستخدمين والأدوار (Roles) في طبقات التطبيق وواجهات برمجة التطبيقات.
/// </para>
/// </summary>
public static class laundry_SaaSPermissions
{
    /// <summary>
    /// اسم المجموعة الرئيسية لصلاحيات المنصة في شجرة إدارة الصلاحيات.
    /// </summary>
    public const string GroupName = "laundry_SaaS";

    /// <summary>
    /// صلاحيات إدارة المنصة المركزية على مستوى المضيف (Host).
    /// </summary>
    public static class Host
    {
        public const string Default = GroupName + ".Host";

        /// <summary>
        /// صلاحية استعراض المغاسل المسجلة في المنصة وإحصائياتها.
        /// </summary>
        public const string ViewLaundries = Default + ".Laundries.View";

        /// <summary>
        /// صلاحية إدارة وإنشاء وتفعيل وتعطيل مستأجري المغاسل.
        /// </summary>
        public const string ManageLaundries = Default + ".Laundries.Manage";

        /// <summary>
        /// صلاحية استعراض الإحصائيات الشاملة للمنصة والتقارير المالية والتشغيلية.
        /// </summary>
        public const string ViewStatistics = Default + ".Statistics.View";
    }

    /// <summary>
    /// صلاحيات إدارة الملف التعريفي والتشغيلي للمغسلة (Laundry).
    /// </summary>
    public static class Laundry
    {
        public const string Default = GroupName + ".Laundry";

        /// <summary>
        /// صلاحية استعراض الملف التعريفي للمغسلة ونطاق التغطية.
        /// </summary>
        public const string ViewProfile = Default + ".Profile.View";

        /// <summary>
        /// صلاحية تعديل بيانات المغسلة، الشعار، ونطاق التغطية ورسوم التوصيل.
        /// </summary>
        public const string ManageProfile = Default + ".Profile.Manage";

        /// <summary>
        /// صلاحية ضبط وتعديل ساعات وأيام العمل الأسبوعية للمغسلة.
        /// </summary>
        public const string ManageWorkingHours = Default + ".WorkingHours.Manage";

        /// <summary>
        /// صلاحية إنشاء وتعديل وتعطيل الفترات الزمنية المجدولة للاستلام والتوصيل.
        /// </summary>
        public const string ManageTimeSlots = Default + ".TimeSlots.Manage";
    }

    /// <summary>
    /// صلاحيات إدارة طاقم عمل المغسلة (Staff).
    /// </summary>
    public static class Staff
    {
        public const string Default = GroupName + ".Staff";

        /// <summary>
        /// صلاحية إدارة ملفات موظفي المغسلة، وتعيينهم، وتعديل أدوارهم.
        /// </summary>
        public const string Manage = Default + ".Manage";
    }

    /// <summary>
    /// صلاحيات إدارة كتالوج الخدمات والأسعار (Catalog).
    /// </summary>
    public static class Catalog
    {
        public const string Default = GroupName + ".Catalog";

        /// <summary>
        /// صلاحية إدارة أنواع الملابس والقطع المغسولة في الكتالوج.
        /// </summary>
        public const string ManageItemTypes = Default + ".ItemTypes.Manage";

        /// <summary>
        /// صلاحية إدارة أنواع الخدمات المتاحة للغسيل والكي والتنظيف الجاف.
        /// </summary>
        public const string ManageServices = Default + ".Services.Manage";

        /// <summary>
        /// صلاحية ضبط وتحديث جدول أسعار الخدمات لقطع الملابس.
        /// </summary>
        public const string ManagePrices = Default + ".Prices.Manage";
    }

    /// <summary>
    /// صلاحيات إدارة ومعالجة الطلبات (Orders).
    /// </summary>
    public static class Orders
    {
        public const string Default = GroupName + ".Orders";

        /// <summary>
        /// صلاحية استعراض تفاصيل وقوائم الطلبات الخاصة بالمغسلة.
        /// </summary>
        public const string View = Default + ".View";

        /// <summary>
        /// صلاحية تحريك المراحل التشغيلية للطلب وبدء المعالجة والتغليف.
        /// </summary>
        public const string Process = Default + ".Process";

        /// <summary>
        /// صلاحية إجراء التعديلات المالية على الطلب الناتجة عن محضر الفحص الفني.
        /// </summary>
        public const string Adjust = Default + ".Adjust";

        /// <summary>
        /// صلاحية الإلغاء الإداري للطلب من قبل إدارة المغسلة مع توثيق السبب.
        /// </summary>
        public const string CancelAdmin = Default + ".CancelAdmin";
    }

    /// <summary>
    /// صلاحيات إدارة السائقين والعمليات اللوجستية (Drivers & Logistics).
    /// </summary>
    public static class Drivers
    {
        public const string Default = GroupName + ".Drivers";

        /// <summary>
        /// صلاحية إنشاء وتعديل ملفات السائقين التابعين للمغسلة.
        /// </summary>
        public const string Manage = Default + ".Manage";

        /// <summary>
        /// صلاحية إسناد مهام الاستلام والتوصيل للسائقين المتاحين.
        /// </summary>
        public const string AssignTasks = Default + ".AssignTasks";

        /// <summary>
        /// صلاحية تنفيذ مهام الاستلام والتوصيل ميدانياً من قبل السائق.
        /// </summary>
        public const string ExecuteTasks = Default + ".ExecuteTasks";
    }

    /// <summary>
    /// صلاحيات العمليات التشغيلية والحقائب (Processing & Bags).
    /// </summary>
    public static class Processing
    {
        public const string Default = GroupName + ".Processing";

        /// <summary>
        /// صلاحية تنفيذ مراحل المعالجة والفرز والغسيل والكي.
        /// </summary>
        public const string Execute = Default + ".Execute";

        /// <summary>
        /// صلاحية مسح وإدارة عهدة الحقائب الذكية وتتبع تحركاتها.
        /// </summary>
        public const string ManageBags = Default + ".Bags.Manage";
    }

    /// <summary>
    /// صلاحيات المعاينة والفحص الفني (Inspection).
    /// </summary>
    public static class Inspection
    {
        public const string Default = GroupName + ".Inspection";

        /// <summary>
        /// صلاحية تسجيل نتائج الفحص، وإثبات الأضرار السابقة، واعتماد المحضر وإعادة فتحه.
        /// </summary>
        public const string Manage = Default + ".Manage";
    }

    /// <summary>
    /// صلاحيات إدارة ومتابعة الشكاوى (Complaints).
    /// </summary>
    public static class Complaints
    {
        public const string Default = GroupName + ".Complaints";

        /// <summary>
        /// صلاحية استعراض الشكاوى والمرفقات المقدمة من العملاء.
        /// </summary>
        public const string View = Default + ".View";

        /// <summary>
        /// صلاحية البت في الشكاوى، وتدوين الملاحظات، وإقرار التعويضات المالية.
        /// </summary>
        public const string Resolve = Default + ".Resolve";
    }
}
