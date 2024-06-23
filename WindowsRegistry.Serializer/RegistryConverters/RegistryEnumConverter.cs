using System.Collections.Concurrent;
using WindowsRegistry.Serializer.Data;

namespace WindowsRegistry.Serializer.RegistryConverters;
public class RegistryEnumConverter : RegistryConverter
{
    private static readonly RegistryStringEnumConverter _registryStringEnumConverter = new();

    private static readonly ConcurrentDictionary<Type, Type> _underlyingEnumTypes = new();
    private static readonly ConcurrentDictionary<Type, Type> _noNullableUnderlyingEnumTypes = new();
    private static readonly ConcurrentDictionary<Type, RegistryConverter> _underlyingRegistryConverterCache = new();

    public (Type enumType, Type underlyingType) GetEnumTypes(Type type)
    {
        Type enumType = type;

        if (!_underlyingEnumTypes.TryGetValue(type, out var underlyingType))
        {
            var nullableUnderlyingType = Nullable.GetUnderlyingType(type);
            if (nullableUnderlyingType is null)
            {
                underlyingType = Enum.GetUnderlyingType(type);
            }
            else
            {
                underlyingType = Enum.GetUnderlyingType(nullableUnderlyingType);
                enumType = nullableUnderlyingType;
            }

            _noNullableUnderlyingEnumTypes.TryAdd(type, enumType);
            _underlyingEnumTypes.TryAdd(type, underlyingType);
        }

        _noNullableUnderlyingEnumTypes.TryGetValue(type, out enumType);

        return (enumType, underlyingType);
    }

    public override bool TryRead(object registryData, Type typeToConvert, RegistrySerializerOptions registrySerializerOptions, out object? result)
    {
        if (registryData is string stringData && stringData.Length != 0 && !char.IsNumber(stringData[0]))
            return _registryStringEnumConverter.TryRead(registryData, typeToConvert, registrySerializerOptions, out result);

        (Type enumType, Type underlyingType) = GetEnumTypes(typeToConvert);

        if (registryData.GetType() == underlyingType)
        {
            return GetEnumFromValue(enumType, registryData, out result);
        }

        if (!_underlyingRegistryConverterCache.TryGetValue(underlyingType, out var converter))
        {
            converter = registrySerializerOptions.GetConverter(underlyingType);
            _underlyingRegistryConverterCache.TryAdd(underlyingType, converter);
        }

        if (!converter.TryRead(registryData, underlyingType, registrySerializerOptions, out var enumValue))
        {
            result = null;
            return false;
        }

        if (enumValue is null)
        {
            result = null;
            return false;
        }

        return GetEnumFromValue(enumType, enumValue, out result);
    }

    public override void Write(object? propertyValue, RegistrySerializerOptions registrySerializerOptions, out object? valueToWrite)
    {
        if (propertyValue is null)
        {
            valueToWrite = null;
            return;
        }

        var type = propertyValue.GetType();
        (_, Type underlyingType) = GetEnumTypes(type);

        valueToWrite = Convert.ChangeType(propertyValue, underlyingType);
    }
    private static bool GetEnumFromValue(Type enumType, object registryData, out object? enumObject)
    {
        if (!Enum.IsDefined(enumType, registryData))
        {
            enumObject = null;
            return false;
        }

        try
        {
            enumObject = Enum.ToObject(enumType, registryData);
            return enumObject is not null;
        }
        catch
        {
            enumObject = null;
            return false;
        }
    }

    public override bool CanConvert(Type typeToConvert) => typeToConvert.IsEnum;
}
