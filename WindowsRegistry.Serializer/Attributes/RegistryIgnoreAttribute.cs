using WindowsRegistry.Serializer.Data;

namespace WindowsRegistry.Serializer.Attributes;
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class RegistryIgnoreAttribute(RegistryIgnoreCondition condition = RegistryIgnoreCondition.Always) : Attribute
{
    public RegistryIgnoreCondition Condition { get; } = condition;
}