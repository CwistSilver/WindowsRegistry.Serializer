namespace WindowsRegistry.Serializer.Services;

public interface IRegistryService
{
    object? Get(string subkey, string key);
    string[]? GetSubKeyNames(string subkey);
    string[]? GetValueNames(string subkey);
    void Set(string subkey, string key, object value);
    void CreateSubKey(string subkey);
    bool ExistsSubKey(string subkey);
}