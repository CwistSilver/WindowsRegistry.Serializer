using WindowsRegistry.Serializer.Data;

namespace WindowsRegistry.Serializer.RegistryConverters;
public class RegistryDateTimeConverter : RegistryConverter<DateTime>
{
    private static readonly string[] _formatsL8 =
    [
        "M/d/yyyy",
        "d/M/yyyy",
        "yyyy/M/d",
        "M-d-yyyy",
        "d-M-yyyy",
        "yyyy-M-d",
        "M.d.yyyy",
        "d.M.yyyy",
        "yyyy.M.d",
        "M,d,yyyy",
        "d,M,yyyy",
        "yyyy,M,d",
        "M d yyyy",
        "d M yyyy",
        "yyyy M d",
    ];

    private static readonly string[] _formatsL10 =
    [
        "MM/dd/yyyy",
        "dd/MM/yyyy",
        "yyyy/MM/dd",
        "MM-dd-yyyy",
        "dd-MM-yyyy",
        "yyyy-MM-dd",
        "MM.dd.yyyy",
        "dd.MM.yyyy",
        "yyyy.MM.dd",
        "MM,dd,yyyy",
        "dd,MM,yyyy",
        "yyyy,MM,dd",
        "MM dd yyyy",
        "dd MM yyyy",
        "yyyy MM dd"
    ];

    public override bool TryRead(object registryData, RegistrySerializerOptions registrySerializerOptions, out DateTime result)
    {
        string registryValue = registryData.ToString() ?? string.Empty;
        string dateFormat = registrySerializerOptions.DateFormatString ?? RegistrySerializerOptions.DefaultDateTimeFormat;

        DateTime date;

        if (registryValue.Length == dateFormat.Length)
        {
            if (DateTime.TryParseExact(registryValue, dateFormat, registrySerializerOptions.Culture, registrySerializerOptions.DateStyles, out date))
            {
                result = date;
                return true;
            }
        }

        if (registryValue.Length == 8)
        {
            if (DateTime.TryParseExact(registryValue, "yyyyMMdd", registrySerializerOptions.Culture, registrySerializerOptions.DateStyles, out date))
            {
                result = date;
                return true;
            }
            else
            {
                foreach (string format in _formatsL8)
                {
                    if (DateTime.TryParseExact(registryValue, format, registrySerializerOptions.Culture, registrySerializerOptions.DateStyles, out date))
                    {
                        result = date;
                        return true;
                    }
                }
            }
        }
        else if (registryValue.Length == 10)
        {
            foreach (string format in _formatsL10)
            {
                if (DateTime.TryParseExact(registryValue, format, registrySerializerOptions.Culture, registrySerializerOptions.DateStyles, out date))
                {
                    result = date;
                    return true;
                }
            }
        }

        if (DateTime.TryParse(registryValue, registrySerializerOptions.Culture, registrySerializerOptions.DateStyles, out date))
        {
            result = date;
            return true;
        }

        result = default;
        return false;
    }

    public override void Write(object? propertyValue, RegistrySerializerOptions registrySerializerOptions, out object? valueToWrite)
    {
        string dateFormat = registrySerializerOptions.DateFormatString ?? RegistrySerializerOptions.DefaultDateTimeFormat;

        if (propertyValue is DateTime date)
        {
            valueToWrite = date.ToString(dateFormat, registrySerializerOptions.Culture);
            return;
        }

        valueToWrite = propertyValue;
    }
}