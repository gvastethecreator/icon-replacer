namespace IconReplacer.AppModel;

public static class DistributionChannelPolicy
{
#if ICON_REPLACER_STORE
    public const string Current = "store";
#else
    public const string Current = "direct";
#endif

    public static bool UpdatesManagedByStore => IsStoreChannel(Current);

    public static bool IsStoreChannel(string? value) =>
        string.Equals(value, "store", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(value, "microsoft-store", StringComparison.OrdinalIgnoreCase);
}
