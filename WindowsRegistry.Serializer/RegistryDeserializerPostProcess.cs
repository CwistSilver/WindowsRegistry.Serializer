namespace WindowsRegistry.Serializer;
public abstract class RegistryDeserializerPostProcess
{
    public abstract object? Effect(object? data);
}

public abstract class RegistryDeserializerPostProcess<T> : RegistryDeserializerPostProcess
{
    public abstract T? Effect(T? data);
    public override object? Effect(object? data) => Effect((T?)data);
}
