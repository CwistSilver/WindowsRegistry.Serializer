namespace WindowsRegistry.Serializer.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class RegistryDeserializeNamesAttribute(params string[] names) : Attribute
{
    public string[] Names { get; } = names;
}