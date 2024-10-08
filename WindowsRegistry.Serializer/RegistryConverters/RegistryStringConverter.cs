﻿using WindowsRegistry.Serializer.Data;

namespace WindowsRegistry.Serializer.RegistryConverters;
public class RegistryStringConverter : RegistryConverter<string>
{
    public override bool TryRead(object registryData, RegistrySerializerOptions registrySerializerOptions, out string? result)
    {
        if (registryData is string stringValue)
        {
            result = stringValue;
            return true;
        }

        result = registryData.ToString();
        return true;
    }
}