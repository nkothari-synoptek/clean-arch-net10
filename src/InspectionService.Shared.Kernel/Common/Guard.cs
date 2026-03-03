namespace InspectionService.Shared.Kernel.Common;

/// <summary>
/// Guard clauses for input validation
/// </summary>
public static class Guard
{
    public static void AgainstNull(object? value, string parameterName)
    {
        if (value is null)
            throw new ArgumentNullException(parameterName);
    }

    public static void AgainstNullOrEmpty(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{parameterName} cannot be null or empty.", parameterName);
    }

    public static void AgainstNegativeOrZero(int value, string parameterName)
    {
        if (value <= 0)
            throw new ArgumentException($"{parameterName} must be greater than zero.", parameterName);
    }

    public static void AgainstNegativeOrZero(decimal value, string parameterName)
    {
        if (value <= 0)
            throw new ArgumentException($"{parameterName} must be greater than zero.", parameterName);
    }

    public static void AgainstOutOfRange(int value, int min, int max, string parameterName)
    {
        if (value < min || value > max)
            throw new ArgumentOutOfRangeException(parameterName, $"{parameterName} must be between {min} and {max}.");
    }

    public static void AgainstInvalidEnum<TEnum>(TEnum value, string parameterName) where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(typeof(TEnum), value))
            throw new ArgumentException($"{parameterName} has an invalid value.", parameterName);
    }

    public static void AgainstEmptyGuid(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
            throw new ArgumentException($"{parameterName} cannot be an empty GUID.", parameterName);
    }
}
