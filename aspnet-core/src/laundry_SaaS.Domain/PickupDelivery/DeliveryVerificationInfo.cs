using System;
using System.Collections.Generic;
using Volo.Abp;
using Volo.Abp.Domain.Values;

namespace laundry_SaaS.PickupDelivery;

/// <summary>
/// كائن قيمة (Value Object) مدمج داخل مهمة التوصيل يوثق ويدير الحالة التاريخية والتحققية
/// لرمز التحقق المؤقت (Delivery OTP) عند تسليم الملابس للعميل.
/// <para>
/// يُلبي هذا الكيان متطلبات الأمان الصارمة: لا يقوم النطاق (Domain) بتوليد الرمز المجرد (Plain OTP)،
/// ولا ينفذ خوارزميات التشفير مباشرة، ولا يرتبط بمزودي الرسائل (SMS/Push)،
/// بل يستقبل فقط القيمة المشفرة المجردة (<see cref="OtpHash"/>) كقيمة مبهمة (Opaque Hash) من الطبقات الموثوقة.
/// </para>
/// </summary>
public class DeliveryVerificationInfo : ValueObject
{
    /// <summary>
    /// عدد خانات رمز التحقق المعتمد (6 أرقام).
    /// </summary>
    public const int OtpDigits = 6;

    /// <summary>
    /// مدة صلاحية رمز التحقق بالدقائق (5 دقائق).
    /// </summary>
    public const int ExpiryMinutes = 5;

    /// <summary>
    /// الحد الأقصى الافتراضي لعدد محاولات إدخال الرمز الخاطئة المسموح بها قبل الإغلاق (3 محاولات).
    /// </summary>
    public const int DefaultMaxAttempts = 3;

    /// <summary>
    /// فترة الانتظار الإلزامية بالثواني بين طلبات إعادة إرسال الرمز (60 ثانية).
    /// </summary>
    public const int ResendCooldownSeconds = 60;

    /// <summary>
    /// الحد الأقصى المسموح به لمرات توليد وإرسال رمز التحقق لنفس المهمة (3 مرات).
    /// </summary>
    public const int MaxGenerations = 3;

    /// <summary>
    /// القيمة المشفرة المجردة لرمز التحقق (Hashed OTP)، لا يتم تخزين الرمز الصريح نهائياً.
    /// </summary>
    public string? OtpHash { get; private set; }

    /// <summary>
    /// تاريخ ووقت انتهاء صلاحية رمز التحقق الحالي.
    /// </summary>
    public DateTime? ExpiresAt { get; private set; }

    /// <summary>
    /// عدد المحاولات الفاشلة المسجلة لإدخال الرمز الحالي.
    /// </summary>
    public int FailedAttempts { get; private set; }

    /// <summary>
    /// الحد الأقصى للمحاولات الفاشلة المسموح بها قبل حظر التحقق.
    /// </summary>
    public int MaxAttempts { get; private set; }

    /// <summary>
    /// يشير إلى ما إذا كان قد تم التحقق من رمز التسليم بنجاح.
    /// </summary>
    public bool IsVerified { get; private set; }

    /// <summary>
    /// تاريخ ووقت إتمام التحقق الناجح من الرمز.
    /// </summary>
    public DateTime? VerifiedAt { get; private set; }

    /// <summary>
    /// تاريخ ووقت آخر إرسال أو توليد لرمز التحقق.
    /// </summary>
    public DateTime? LastSentAt { get; private set; }

    /// <summary>
    /// إجمالي عدد مرات توليد الرمز لهذه المهمة.
    /// </summary>
    public int GenerationCount { get; private set; }

    /// <summary>
    /// مُنشئ محمي خالي من المعاملات مخصص لـ Entity Framework Core.
    /// </summary>
    private DeliveryVerificationInfo()
    {
    }

    /// <summary>
    /// يُنشئ كائن معلومات تحقق جديد في حالة أولية غير مفعلة وبدون رمز.
    /// </summary>
    /// <param name="maxAttempts">الحد الأقصى للمحاولات الفاشلة المسموح بها (الافتراضي 3).</param>
    public DeliveryVerificationInfo(int maxAttempts = DefaultMaxAttempts)
    {
        MaxAttempts = maxAttempts > 0 ? maxAttempts : DefaultMaxAttempts;
        FailedAttempts = 0;
        IsVerified = false;
        GenerationCount = 0;
    }

    /// <summary>
    /// يسجل رمز تحقق مشفر جديد لمهمة التوصيل، مع فرض قيود التبريد والحد الأقصى لمرات التوليد،
    /// وإبطال أي رمز سابق تلقائياً وتصفير عداد المحاولات الفاشلة.
    /// </summary>
    /// <param name="otpHash">القيمة المشفرة للرمز الجديد (غير فارغة).</param>
    /// <param name="generatedAt">تاريخ ووقت التوليد.</param>
    /// <param name="expiresAt">تاريخ ووقت انتهاء الصلاحية.</param>
    /// <exception cref="BusinessException">يتم رميها إذا تجاوز عدد مرات التوليد الحد الأقصى أو لم تنقضِ فترة التبريد.</exception>
    public void SetNewOtp(string otpHash, DateTime generatedAt, DateTime expiresAt)
    {
        Check.NotNullOrWhiteSpace(otpHash, nameof(otpHash), maxLength: 256);

        if (GenerationCount >= MaxGenerations)
        {
            throw new BusinessException(
                laundry_SaaSDomainErrorCodes.DeliveryTaskErrorCodes.OtpMaxGenerationsReached,
                $"Maximum OTP generation limit ({MaxGenerations}) has been reached for this delivery task.");
        }

        if (LastSentAt.HasValue)
        {
            var elapsedSeconds = (generatedAt - LastSentAt.Value).TotalSeconds;
            if (elapsedSeconds < ResendCooldownSeconds)
            {
                var remaining = (int)Math.Ceiling(ResendCooldownSeconds - elapsedSeconds);
                throw new BusinessException(
                    laundry_SaaSDomainErrorCodes.DeliveryTaskErrorCodes.OtpResendCooldown,
                    $"Please wait {remaining} seconds before requesting a new OTP.");
            }
        }

        if (expiresAt <= generatedAt)
        {
            throw new BusinessException("OTP expiration time must be later than generation time.");
        }

        OtpHash = otpHash;
        ExpiresAt = expiresAt;
        FailedAttempts = 0;
        IsVerified = false;
        VerifiedAt = null;
        LastSentAt = generatedAt;
        GenerationCount++;
    }

    /// <summary>
    /// يحدد ما إذا كانت حالة رمز التحقق الحالية تسمح بإجراء محاولة تحقق.
    /// </summary>
    /// <param name="currentDateTime">الوقت الحالي المعتمد.</param>
    /// <returns>صحيح إذا كان التحقق متاحاً وصالحاً، وإلا خطأ.</returns>
    public bool CanVerify(DateTime currentDateTime)
    {
        return !string.IsNullOrWhiteSpace(OtpHash)
               && ExpiresAt.HasValue
               && currentDateTime <= ExpiresAt.Value
               && FailedAttempts < MaxAttempts
               && !IsVerified;
    }

    /// <summary>
    /// يسجل محاولة تحقق فاشلة ويزيد عداد المحاولات الفاشلة بمقدار واحد.
    /// </summary>
    /// <exception cref="BusinessException">يتم رميها إذا كان الرمز قد تم تأكيده مسبقاً أو تجاوز الحد الأقصى للمحاولات.</exception>
    public void RecordFailedAttempt()
    {
        if (IsVerified)
        {
            throw new BusinessException("Cannot record failed attempt on an already verified delivery.");
        }

        if (FailedAttempts >= MaxAttempts)
        {
            throw new BusinessException(
                laundry_SaaSDomainErrorCodes.DeliveryTaskErrorCodes.OtpMaxAttemptsReached,
                $"Maximum verification attempts ({MaxAttempts}) exceeded.");
        }

        FailedAttempts++;
    }

    /// <summary>
    /// يعتمد نجاح عملية التحقق من رمز التسليم بعد مطابقة الرمز في طبقة التطبيق الموثوقة.
    /// </summary>
    /// <param name="verifiedAt">تاريخ ووقت التحقق.</param>
    /// <param name="currentDateTime">الوقت الحالي المعتمد.</param>
    /// <exception cref="BusinessException">يتم رميها إذا كان الرمز غير متاح، أو منتهي الصلاحية، أو تجاوز المحاولات، أو موثقاً مسبقاً.</exception>
    public void MarkAsVerified(DateTime verifiedAt, DateTime currentDateTime)
    {
        if (IsVerified)
        {
            throw new BusinessException("Delivery OTP is already verified.");
        }

        if (string.IsNullOrWhiteSpace(OtpHash) || !ExpiresAt.HasValue)
        {
            throw new BusinessException(
                laundry_SaaSDomainErrorCodes.DeliveryTaskErrorCodes.OtpNotAvailable,
                "No active OTP found for this delivery task.");
        }

        if (currentDateTime > ExpiresAt.Value)
        {
            throw new BusinessException(
                laundry_SaaSDomainErrorCodes.DeliveryTaskErrorCodes.OtpExpired,
                "Delivery OTP has expired.");
        }

        if (FailedAttempts >= MaxAttempts)
        {
            throw new BusinessException(
                laundry_SaaSDomainErrorCodes.DeliveryTaskErrorCodes.OtpMaxAttemptsReached,
                $"Maximum verification attempts ({MaxAttempts}) exceeded.");
        }

        IsVerified = true;
        VerifiedAt = verifiedAt;
    }

    /// <summary>
    /// يُرجع القيم الذرية المحددة لمساواة كائن القيمة.
    /// </summary>
    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return OtpHash ?? string.Empty;
        yield return ExpiresAt ?? DateTime.MinValue;
        yield return FailedAttempts;
        yield return MaxAttempts;
        yield return IsVerified;
        yield return VerifiedAt ?? DateTime.MinValue;
        yield return LastSentAt ?? DateTime.MinValue;
        yield return GenerationCount;
    }
}
