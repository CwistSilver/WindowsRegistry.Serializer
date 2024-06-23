using System.Globalization;
using WindowsRegistry.Serializer.Data;

namespace WindowsRegistry.Serializer.RegistryConverters;
public class RegistryFloatConverter : RegistryConverter<float>
{
    public override bool TryRead(object registryData, RegistrySerializerOptions registrySerializerOptions, out float result)
    {
        if (registryData is float floatValue)
        {
            result = floatValue;
            return true;
        }

        return float.TryParse(registryData.ToString(), NumberStyles.AllowThousands | NumberStyles.Float, registrySerializerOptions.Culture, out result);
    }

    public override void Write(object? propertyValue, RegistrySerializerOptions registrySerializerOptions, out object? valueToWrite)
    {
        if (propertyValue is float floatValue)
        {
            valueToWrite = floatValue.ToString(registrySerializerOptions.Culture);
            return;
        }

        valueToWrite = propertyValue;
    }

}