namespace WindowsRegistry.Serializer.Data;

public abstract class RegistryConverter
{
    public abstract bool TryRead(object registryData, Type typeToConvert, RegistrySerializerOptions registrySerializerOptions, out object? result);
    public virtual void Write(object? propertyValue, RegistrySerializerOptions registrySerializerOptions, out object? valueToWrite) => valueToWrite = propertyValue;

    public abstract bool CanConvert(Type typeToConvert);
}

public abstract class RegistryConverter<T> : RegistryConverter
{
    public abstract bool TryRead(object registryData, RegistrySerializerOptions registrySerializerOptions, out T? result);
    public override bool TryRead(object registryData, Type typeToConvert, RegistrySerializerOptions registrySerializerOptions, out object? result)
    {
        bool success = TryRead(registryData, registrySerializerOptions, out T? typedResult);
        result = typedResult;
        return success;
    }

    public virtual void Write(T? propertyValue, RegistrySerializerOptions registrySerializerOptions, out T? valueToWrite) => valueToWrite = propertyValue;
    public override void Write(object? propertyValue, RegistrySerializerOptions registrySerializerOptions, out object? valueToWrite)
    {
        Write((T?)propertyValue, registrySerializerOptions, out T? valueToWriteT);
        valueToWrite = valueToWriteT;
    }

    public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(T);
}
