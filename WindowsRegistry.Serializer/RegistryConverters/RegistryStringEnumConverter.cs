using System.Collections.Concurrent;
using WindowsRegistry.Serializer.Data;

namespace WindowsRegistry.Serializer.RegistryConverters;
public class RegistryStringEnumConverter : RegistryConverter
{
    private static readonly ConcurrentDictionary<Type, Type> _noNullableUnderlyingEnumTypes = new();

    public override bool TryRead(object registryData, Type typeToConvert, RegistrySerializerOptions registrySerializerOptions, out object? result)
    {
        if (!_noNullableUnderlyingEnumTypes.TryGetValue(typeToConvert, out Type enumType))
        {
            var nullableUnderlyingType = Nullable.GetUnderlyingType(typeToConvert);
            if (nullableUnderlyingType is null)
                enumType = typeToConvert;
            else
                enumType = nullableUnderlyingType;

            _noNullableUnderlyingEnumTypes.TryAdd(typeToConvert, enumType);
        }

        if (registryData is string stringEnumValue)
            return GetEnumFromString(enumType, stringEnumValue, out result);

        var objectStringValue = registryData.ToString();
        if (objectStringValue is null)
        {
            result = null;
            return false;
        }

        return GetEnumFromString(enumType, objectStringValue, out result);
    }

    private static bool GetEnumFromString(Type enumType, string stringEnumValue, out object? enumObject)
    {
        if (Enum.TryParse(enumType, stringEnumValue, true, out enumObject))
            return true;

        enumObject = null;
        return false;
    }

    public override bool CanConvert(Type typeToConvert) => typeToConvert.IsEnum;
}
