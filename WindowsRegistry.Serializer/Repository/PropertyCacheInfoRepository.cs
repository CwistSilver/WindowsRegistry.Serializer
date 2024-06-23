using System.Collections.Concurrent;
using System.Reflection;
using WindowsRegistry.Serializer.Attributes;
using WindowsRegistry.Serializer.Data;

namespace WindowsRegistry.Serializer.Repository;
public class PropertyCacheInfoRepository : IPropertyCacheInfoRepository
{
    private readonly ConcurrentDictionary<Type, List<PropertyCacheInfo>> _propertyCache = [];

    public List<PropertyCacheInfo> GetPropertyCacheInfos(Type type)
    {
        if (_propertyCache.TryGetValue(type, out var list))
            return list;

        var propertyInfos = GetPropertyInfos(type);
        var properties = new List<PropertyCacheInfo>(propertyInfos.Length);

        foreach (var property in propertyInfos)
        {
            var name = GetRegistryName(property);
            var deserializeNames = GetDeserializeNames(property, name);
            var registryIgnoreCondition = GetIgnoreCondition(property);

            properties.Add(new PropertyCacheInfo(property, deserializeNames, name)
            {
                Converter = GetConverter(property),
                IgnoreCondition = registryIgnoreCondition,
                IsIgnoreConditionDefault = registryIgnoreCondition is null,
                PostProcess = GetPostProcess(property)
            });
        }

        _propertyCache[type] = properties;

        return properties;
    }

    private static RegistryDeserializerPostProcess? GetPostProcess(PropertyInfo property)
    {
        var postProcessAttribute = property.GetCustomAttribute<RegistryDeserializerPostProcessAttribute>();
        if (postProcessAttribute is null)
            return null;

        return (RegistryDeserializerPostProcess?)Activator.CreateInstance(postProcessAttribute.ConverterType);
    }

    private static RegistryConverter? GetConverter(PropertyInfo property)
    {
        var converterAttribute = property.GetCustomAttribute<RegistryConverterAttribute>();
        if (converterAttribute is null)
            return null;

        return (RegistryConverter?)Activator.CreateInstance(converterAttribute.ConverterType);
    }

    private static string GetRegistryName(PropertyInfo property)
    {
        var registryNameAttribute = property.GetCustomAttribute<RegistryNameAttribute>();
        var name = registryNameAttribute?.Name ?? property.Name;

        return name;
    }

    private static string[] GetDeserializeNames(PropertyInfo property, string registryName)
    {
        var deserializeNamesAttribute = property.GetCustomAttribute<RegistryDeserializeNamesAttribute>();
        var deserializeNames = deserializeNamesAttribute?.Names ?? [registryName];

        return deserializeNames;
    }

    private static PropertyInfo[] GetPropertyInfos(Type type) => type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
    private static RegistryIgnoreCondition? GetIgnoreCondition(PropertyInfo property) => property.GetCustomAttribute<RegistryIgnoreAttribute>()?.Condition;
}
