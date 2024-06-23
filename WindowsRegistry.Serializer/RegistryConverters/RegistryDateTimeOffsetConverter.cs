using WindowsRegistry.Serializer.Data;

namespace WindowsRegistry.Serializer.RegistryConverters;
public class RegistryDateTimeOffsetConverter : RegistryConverter<DateTimeOffset>
{
    private static readonly string[] _formats =
    [
        "yyyy-MM-ddTHH:mm:sszzz",
        "M/d/yyyy HH:mm:ss zzz",
        "d/M/yyyy HH:mm:ss zzz",
        "yyyy/M/d HH:mm:ss zzz",
        "MM/dd/yyyy HH:mm:ss zzz",
        "dd/MM/yyyy HH:mm:ss zzz",
        "yyyy/MM/dd HH:mm:ss zzz",
        "MM-dd-yyyy HH:mm:ss zzz",
        "dd-MM-yyyy HH:mm:ss zzz",
        "yyyy-MM-dd HH:mm:ss zzz",
        "MM.dd.yyyy HH:mm:ss zzz",
        "dd.MM.yyyy HH:mm:ss zzz",
        "yyyy.MM.dd HH:mm:ss zzz",
        "MM,dd,yyyy HH:mm:ss zzz",
        "dd,MM,yyyy HH:mm:ss zzz",
        "yyyy,MM,dd HH:mm:ss zzz",
        "MM dd yyyy HH:mm:ss zzz",
        "dd MM yyyy HH:mm:ss zzz",
        "yyyy MM dd HH:mm:ss zzz"
    ];

    public override bool TryRead(object registryData, RegistrySerializerOptions registrySerializerOptions, out DateTimeOffset result)
    {
        string registryValue = registryData.ToString() ?? string.Empty;
        string dateFormat = registrySerializerOptions.DateTimeOffsetFormatString ?? RegistrySerializerOptions.DefaultDateTimeOffsetFormat;

        if (DateTimeOffset.TryParseExact(registryValue, dateFormat, registrySerializerOptions.Culture, registrySerializerOptions.DateStyles, out DateTimeOffset dateTimeOffset))
        {
            result = dateTimeOffset;
            return true;
        }

        foreach (string format in _formats)
        {
            if (DateTimeOffset.TryParseExact(registryValue, format, registrySerializerOptions.Culture, registrySerializerOptions.DateStyles, out dateTimeOffset))
            {
                result = dateTimeOffset;
                return true;
            }
        }

        result = default;
        return false;
    }

    public override void Write(object? propertyValue, RegistrySerializerOptions registrySerializerOptions, out object? valueToWrite)
    {
        string dateFormat = registrySerializerOptions.DateTimeOffsetFormatString ?? RegistrySerializerOptions.DefaultDateTimeOffsetFormat;

        if (propertyValue is DateTimeOffset dateTimeOffset)
        {
            valueToWrite = dateTimeOffset.ToString(dateFormat, registrySerializerOptions.Culture);
            return;
        }

        valueToWrite = propertyValue;
    }
}
