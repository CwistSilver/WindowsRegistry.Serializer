using WindowsRegistry.Serializer.Data;

namespace WindowsRegistry.Serializer.RegistryConverters;
public class RegistryGuidConverter : RegistryConverter<Guid>
{
    public override bool TryRead(object registryData, RegistrySerializerOptions registrySerializerOptions, out Guid result)
    {
        string guidFormat = registrySerializerOptions.GuidFormatString ?? RegistrySerializerOptions.DefaultGuidFormat;

        if (registryData is Guid guidValue)
        {
            result = guidValue;
            return true;
        }

        if (Guid.TryParseExact(registryData.ToString(), guidFormat, out result))
            return true;

        return Guid.TryParse(registryData.ToString(), out result);
    }

    public override void Write(object? propertyValue, RegistrySerializerOptions registrySerializerOptions, out object? valueToWrite)
    {
        string guidFormat = registrySerializerOptions.GuidFormatString ?? RegistrySerializerOptions.DefaultGuidFormat;

        if (propertyValue is Guid guidValue)
        {
            valueToWrite = guidValue.ToString(guidFormat, registrySerializerOptions.Culture);
            return;
        }

        valueToWrite = propertyValue;
    }
}