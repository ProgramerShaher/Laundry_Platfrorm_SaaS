namespace laundry_SaaS;

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

    public const string Prefix = "laundry_SaaS";
}

