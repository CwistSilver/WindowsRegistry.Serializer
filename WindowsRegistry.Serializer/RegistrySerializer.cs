using System.Collections;
using System.Dynamic;
using System.Reflection;
using WindowsRegistry.Serializer.Data;
using WindowsRegistry.Serializer.Repository;
using WindowsRegistry.Serializer.Services;

namespace WindowsRegistry.Serializer;
public static class RegistrySerializer
{
    public static readonly IRegistryService _registryService = new RegistryService();
    public static readonly IPropertyCacheInfoRepository _propertyCacheInfoRepository = new PropertyCacheInfoRepository();

    #region Serialize
    public static void Serialize<T>(string registryKey, T objectToSerialize, RegistrySerializerOptions? options = null)
    {
        if (objectToSerialize is null)
            throw new ArgumentNullException(nameof(objectToSerialize), "The object to serialize cannot be null. Please ensure you provide a valid object to serialize.");

        options ??= RegistrySerializerOptions.Default;

        Type type = typeof(T);

        Serialize(registryKey, objectToSerialize, type, options);
    }

    public static void Serialize(string registryKey, object objectToSerialize, Type type, RegistrySerializerOptions? options = null)
    {
        options ??= RegistrySerializerOptions.Default;

        if (!_registryService.ExistsSubKey(registryKey))
        {
            if (options.CreateSubKeyWhenMissing)
                _registryService.CreateSubKey(registryKey);
            else
                throw new InvalidOperationException($"The registry key '{registryKey}' does not exist. You must either manually create this key or set 'CreateSubKeyWhenMissing' to true in your 'RegistrySerializerOptions'.");
        }

        var classPropertyInfos = _propertyCacheInfoRepository.GetPropertyCacheInfos(type);
        var keyValuePairs = new Dictionary<string, object?>();

        foreach (PropertyCacheInfo propertyInfo in classPropertyInfos)
        {
            if (!propertyInfo.Property.CanRead)
                continue;

            RegistryIgnoreCondition ignoreCondition;
            if (propertyInfo.IsIgnoreConditionDefault)
                ignoreCondition = options.DefaultIgnoreCondition;
            else
                ignoreCondition = propertyInfo.IgnoreCondition!.Value;

            if (ignoreCondition == RegistryIgnoreCondition.Always)
                continue;

            var propertyValue = propertyInfo.Property.GetValue(objectToSerialize);
            object? valueToWrite;
            if (propertyInfo.Converter is not null)
            {
                propertyInfo.Converter.Write(propertyValue, options, out valueToWrite);
            }
            else
            {
                var converter = options.GetConverter(propertyInfo);
                converter.Write(propertyValue, options, out valueToWrite);
            }

            if (ShouldPropertyIgnoredForWriting(ignoreCondition, valueToWrite, propertyInfo.DefaultValue))
                continue;

            keyValuePairs.Add(propertyInfo.DeserializeNames[0], valueToWrite);
        }

        WriteToRegistry(keyValuePairs, registryKey, options);
    }

    private static void WriteToRegistry(Dictionary<string, object?> keyValuePairs, string registryKey, RegistrySerializerOptions options)
    {
        foreach (var pair in keyValuePairs)
        {
            try
            {
                _registryService.Set(registryKey, pair.Key, pair.Value!);
            }
            catch
            {
                if (options.IgnoreThrownExceptions)
                    continue;
                else
                    throw;
            }
        }
    }

    #endregion

    #region Deserialize

    public static T? Deserialize<T>(string path, RegistrySerializerOptions? options = null)
    {
        options ??= RegistrySerializerOptions.Default;

        Type type = typeof(T);

        if (IsCollectionType(type))
            return (T?)DeserializeCollection(path, type, options);

        T? deserializedObject = (T?)DeserializeSingleObject(path, type, options);
        if (deserializedObject is null)
            return default;

        return deserializedObject;
    }

    public static object? Deserialize(string path, Type type, RegistrySerializerOptions? options = null)
    {
        options ??= RegistrySerializerOptions.Default;

        if (IsCollectionType(type))
            return DeserializeCollection(path, type, options);

        var deserializedObject = DeserializeSingleObject(path, type, options);
        if (deserializedObject is null)
            return default;

        return deserializedObject;
    }

    public static object? Deserialize(string path, RegistrySerializerOptions? options = null)
    {
        options ??= RegistrySerializerOptions.Default;

        dynamic obj = new ExpandoObject();
        var objAsDict = obj as IDictionary<string, object>;

        var propertyNames = _registryService.GetValueNames(path);
        if (propertyNames is null || propertyNames.Length == 0)
            return null;

        foreach (string propertyName in propertyNames)
        {
            object? propertyValue;
            try
            {
                propertyValue = _registryService.Get(path, propertyName);
            }
            catch
            {
                if (options.IgnoreThrownExceptions)
                    continue;
                else
                    throw;
            }

            if (propertyValue is null)
                continue;

            objAsDict![propertyName] = propertyValue;
        }

        if (objAsDict!.Count == 0)
            return null;

        return obj;
    }

    public static object? DeserializeSingleObject(string path, Type type, RegistrySerializerOptions options)
    {
        uint successfulyDeserializedProperties = 0;

        var classPropertyInfos = _propertyCacheInfoRepository.GetPropertyCacheInfos(type);
        var obj = Activator.CreateInstance(type);

        foreach (var propertyInfo in classPropertyInfos)
        {
            RegistryIgnoreCondition ignoreCondition;
            if (propertyInfo.IsIgnoreConditionDefault)
                ignoreCondition = options.DefaultIgnoreCondition;
            else
                ignoreCondition = propertyInfo.IgnoreCondition!.Value;

            if (!propertyInfo.Property.CanWrite || ignoreCondition == RegistryIgnoreCondition.Always)
                continue;

            foreach (var name in propertyInfo.DeserializeNames)
            {
                object? propertyValue = null;

                if (propertyInfo.Converter is not null)
                {
                    if (GetPropertyValue(propertyInfo, propertyInfo.Converter, path, name, options, ref propertyValue))
                        successfulyDeserializedProperties++;
                }
                else
                {
                    var converter = options.GetConverter(propertyInfo);
                    if (GetPropertyValue(propertyInfo, converter, path, name, options, ref propertyValue))
                        successfulyDeserializedProperties++;
                }

                if (propertyValue is not null)
                {
                    if (propertyInfo.PostProcess is not null)
                        propertyValue = propertyInfo.PostProcess.Effect(propertyValue);

                    propertyInfo.Property.SetValue(obj, propertyValue);
                    break;
                }
            }
        }

        if (successfulyDeserializedProperties == 0)
            return null;

        return obj;
    }

    public static object? DeserializeCollection(string rootDirectoryPath, Type collectionType, RegistrySerializerOptions options)
    {
        Type itemType = GetCollectionItemType(collectionType);
        var subKeys = _registryService.GetSubKeyNames(rootDirectoryPath);
        if (subKeys is null || subKeys.Length == 0)
            return null;

        var itemList = CreateGenericList(itemType, subKeys.Length);

        foreach (var program in subKeys)
        {
            var key = Path.Combine(rootDirectoryPath, program);
            var item = DeserializeSingleObject(key, itemType, options);
            if (item is not null)
                itemList.Add(item);
        }

        return ConvertChangeType(itemList, collectionType, itemType);
    }

    #endregion

    private static bool GetPropertyValue(PropertyCacheInfo propertyInfo, RegistryConverter converter, string registryPath, string keyName, RegistrySerializerOptions options, ref object? propertyValue)
    {
        propertyValue = null;

        try
        {
            var objectValue = _registryService.Get(registryPath, keyName);
            if (objectValue is null)
                return false;

            if (propertyInfo.IsNullable && objectValue is byte[] byteArray && byteArray.Length == 0)
            {
                propertyValue = null;
                return true;
            }

            return ReadPropertyFromConverter(objectValue, propertyInfo, converter, options, ref propertyValue);
        }
        catch
        {
            if (options.IgnoreThrownExceptions)
            {
                propertyValue = null;
                return false;
            }
            else
                throw;
        }
    }

    private static bool ReadPropertyFromConverter(object objectValue, PropertyCacheInfo propertyInfo, RegistryConverter converter, RegistrySerializerOptions registrySerializerOptions, ref object? propertyValue)
    {
        bool success = converter.TryRead(objectValue, propertyInfo.Property.PropertyType, registrySerializerOptions, out propertyValue);
        if (!success)
        {
            if (propertyInfo.IsNullable)
            {
                propertyValue = null;
                return true;
            }

            propertyValue = GetPropertyDefault(propertyInfo);
        }

        return success;
    }

    private static object? GetPropertyDefault(PropertyCacheInfo propertyInfo)
    {
        if (propertyInfo.Property.PropertyType.IsValueType)
        {
            return Activator.CreateInstance(propertyInfo.Property.PropertyType);
        }
        else
            return null;
    }

    public static bool ShouldPropertyIgnoredForWriting(RegistryIgnoreCondition ignoreCondition, object? propertyValue, object? defaultValue) => ignoreCondition switch
    {
        RegistryIgnoreCondition.WhenWritingDefault => Equals(propertyValue, defaultValue),
        RegistryIgnoreCondition.WhenWritingNull => propertyValue is null,
        RegistryIgnoreCondition.Never => false,
        RegistryIgnoreCondition.Always => true,
        _ => throw new NotImplementedException()
    };

    private static Type GetCollectionItemType(Type collectionType)
    {
        if (collectionType.IsArray)
            return collectionType.GetElementType();
        else if (collectionType.IsGenericType)
            return collectionType.GetGenericArguments()[0];

        throw new InvalidOperationException("Unsupported collection type");
    }

    private static object ConvertChangeType(IList source, Type targetType, Type itemType)
    {
        if (targetType.IsArray)
        {
            Array targetArray = Array.CreateInstance(itemType, source.Count);
            source.CopyTo(targetArray, 0);
            return targetArray;
        }

        if (typeof(List<>).MakeGenericType(itemType).IsAssignableFrom(targetType))
            return source;

        if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(IList<>) && targetType.GenericTypeArguments[0] == itemType)
            return source;

        if (targetType == typeof(IEnumerable) || (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
            return source;

        Type genericIListType = typeof(IList<>).MakeGenericType(itemType);

        if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(ISet<>))
        {
            Type iSetImplementationType = typeof(HashSet<>).MakeGenericType(itemType);
            ConstructorInfo hashSetConstructor = iSetImplementationType.GetConstructor([genericIListType]);
            if (hashSetConstructor is not null)
                return hashSetConstructor.Invoke([source]);
        }

        if (targetType.IsInterface)
            throw new InvalidOperationException($"The target type '{targetType.FullName}' is an interface. Please specify the target type as a class.");

        object targetCollection = Activator.CreateInstance(targetType);

        ConstructorInfo constructor = targetType.GetConstructor([genericIListType]);
        if (constructor is not null)
            return constructor.Invoke([source]);


        MethodInfo addRangeMethod = targetType.GetMethod("AddRange");
        if (addRangeMethod is not null)
        {
            addRangeMethod.Invoke(targetCollection, [source]);
            return targetCollection;
        }

        MethodInfo addMethod = targetType.GetMethod("Add");
        if (addMethod is not null)
        {
            foreach (var item in source)
            {
                addMethod.Invoke(targetCollection, [item]);
            }
        }
        else
        {
            throw new InvalidOperationException($"The collection type '{targetType.FullName}' does not have an 'Add' method.");
        }

        return targetCollection;
    }

    private static IList CreateGenericList(Type type, in int capacity)
    {
        Type genericListType = typeof(List<>).MakeGenericType(type);
        return (IList)Activator.CreateInstance(genericListType, [capacity]);
    }

    private static bool IsCollectionType(Type type) => type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type);
}
