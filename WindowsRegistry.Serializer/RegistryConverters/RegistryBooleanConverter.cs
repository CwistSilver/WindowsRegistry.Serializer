using WindowsRegistry.Serializer.Data;

namespace WindowsRegistry.Serializer.RegistryConverters;
public class RegistryBooleanConverter : RegistryConverter<bool>
{
    public override bool TryRead(object registryData, RegistrySerializerOptions registrySerializerOptions, out bool result)
    {
        if (registryData is int intValue)
        {
            if (intValue == 0)
            {
                result = false;
                return true;
            }
            else if (intValue == 1)
            {
                result = true;
                return true;
            }
            else
            {
                result = default;
                return false;
            }
        }

        if (registryData is bool boolValue)
        {
            result = boolValue;
            return true;
        }

        return bool.TryParse(registryData.ToString(), out result);
    }

    public override void Write(object? propertyValue, RegistrySerializerOptions registrySerializerOptions, out object? valueToWrite)
    {
        if (propertyValue is bool boolValue)
        {
            valueToWrite = boolValue ? 1 : 0;
            return;
        }

        valueToWrite = propertyValue;
    }
}