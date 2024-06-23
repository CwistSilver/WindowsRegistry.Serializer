using WindowsRegistry.Serializer.Data;

namespace WindowsRegistry.Serializer.RegistryConverters;
public class RegistryVersionConverter : RegistryConverter<Version>
{
    public override bool TryRead(object registryData, RegistrySerializerOptions registrySerializerOptions, out Version? result)
    {
        if (registryData is string registryValue)
        {
            if (int.TryParse(registryValue, out int versionInt))
            {
                result = ConvertToVersion((uint)versionInt);
                return result is not null;
            }

            return Version.TryParse(registryValue, out result);
        }

        result = default;
        return false;
    }

    private static Version ConvertToVersion(uint versionUint)
    {
        int major = (int)(versionUint >> 24);
        int minor = (int)((versionUint >> 16) & 0xFF);
        int build = (int)((versionUint >> 8) & 0xFF);
        int revision = (int)(versionUint & 0xFF);
        return new Version(major, minor, build, revision);
    }
}