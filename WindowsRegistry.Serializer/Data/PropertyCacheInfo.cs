using System.Reflection;

namespace WindowsRegistry.Serializer.Data;
public class PropertyCacheInfo(PropertyInfo property, string[] deserializeNames, string registryName)
{
    private const string NullableAttributeName = "Nullable";

    private static bool IsPropertyNullable(PropertyInfo property)
    {
        var isNullable = Nullable.GetUnderlyingType(property.PropertyType) is not null;

        if (property.PropertyType != typeof(string))
            return isNullable;

        foreach (var attribute in property.DeclaringType.CustomAttributes)
        {
            if (attribute.AttributeType.Name.Contains(NullableAttributeName, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return isNullable;
    }

    public PropertyInfo Property { get; private set; } = property;
    public Type? UnderlyingTyp { get; private set; } = Nullable.GetUnderlyingType(property.PropertyType);
    public string[] DeserializeNames { get; private set; } = deserializeNames;
    public string RegistryName { get; private set; } = registryName;
    public object? DefaultValue { get; private set; } = property.PropertyType.IsValueType ? Activator.CreateInstance(property.PropertyType) : null;
    public RegistryConverter? Converter { get; set; }
    public RegistryDeserializerPostProcess? PostProcess { get; set; }
    public RegistryIgnoreCondition? IgnoreCondition { get; set; }
    public bool IsIgnoreConditionDefault { get; set; }
    public bool IsNullable { get; set; } = IsPropertyNullable(property);
}
