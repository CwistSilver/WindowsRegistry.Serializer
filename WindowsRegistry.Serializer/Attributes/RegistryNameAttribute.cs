namespace WindowsRegistry.Serializer.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class RegistryNameAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}