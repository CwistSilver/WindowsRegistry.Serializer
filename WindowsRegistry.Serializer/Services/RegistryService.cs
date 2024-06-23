using Microsoft.Win32;
using System.Collections.Concurrent;

namespace WindowsRegistry.Serializer.Services;
public class RegistryService : IRegistryService
{
    private const string ComputerPrefix = @"Computer\";

    private readonly ConcurrentDictionary<string, RegistryKey?> _readOnlyCache = new();
    private readonly ConcurrentDictionary<string, RegistryKey?> _writableCache = new();
    private readonly Dictionary<string, RegistryKey> _registryMap = new()
    {
        {"HKLM", Registry.LocalMachine},
        {"HKEY_LOCAL_MACHINE", Registry.LocalMachine},

        {"HKCU", Registry.CurrentUser},
        {"HKEY_CURRENT_USER", Registry.CurrentUser},

        {"HKU", Registry.Users},
        {"HKEY_USERS", Registry.Users},

        {"HKCC", Registry.CurrentConfig},
        {"HKEY_CURRENT_CONFIG", Registry.CurrentConfig},

        {"HKCR", Registry.ClassesRoot},
        {"HKEY_CLASSES_ROOT", Registry.ClassesRoot},

        {"HKPD", Registry.PerformanceData},
        {"HKEY_PERFORMANCE_DATA", Registry.PerformanceData},
    };

    private RegistryKey? GetRegistryKey(string subkey, bool writable)
    {
        var cache = writable ? _writableCache : _readOnlyCache;
        if (cache.TryGetValue(subkey, out var cachedKey) && cachedKey is not null)
        {
            try
            {
                var dummyValue = cachedKey.GetValue(null);
                return cachedKey;
            }
            catch
            {
                cache.TryRemove(subkey, out _);
            }
        }

        ReadOnlySpan<char> pathSpan = subkey.AsSpan();
        if (pathSpan.StartsWith(ComputerPrefix, StringComparison.OrdinalIgnoreCase))
            pathSpan = pathSpan[ComputerPrefix.Length..];

        var separatorIndex = pathSpan.IndexOf(Path.DirectorySeparatorChar);
        if (separatorIndex == -1)
            return null;

        Span<char> locationBuffer = stackalloc char[separatorIndex];
        pathSpan[..separatorIndex].ToUpperInvariant(locationBuffer);
        var location = new string(locationBuffer);

        pathSpan = pathSpan[(separatorIndex + 1)..];

        if (!_registryMap.TryGetValue(location, out var openSubKeyFunc))
            return null;

        var regKey = openSubKeyFunc.OpenSubKey(pathSpan.ToString(), writable);
        if (regKey is not null)
            cache[subkey] = regKey;

        return regKey;
    }

    public object? Get(string subkey, string key)
    {
        var regKey = GetRegistryKey(subkey, false);
        if (regKey is null)
            return null;

        return regKey.GetValue(key);
    }

    public void Set(string subkey, string key, object value)
    {
        var regKey = GetRegistryKey(subkey, true);

        if (regKey is null)
            return;

        if (value is null)
        {
            regKey.SetValue(key, new byte[0], RegistryValueKind.None);
        }
        else
            regKey.SetValue(key, value);
    }

    public void CreateSubKey(string subkey)
    {
        ReadOnlySpan<char> pathSpan = subkey.AsSpan();
        if (pathSpan.StartsWith(ComputerPrefix, StringComparison.OrdinalIgnoreCase))
            pathSpan = pathSpan[ComputerPrefix.Length..];

        var separatorIndex = pathSpan.IndexOf(Path.DirectorySeparatorChar);
        if (separatorIndex == -1)
            return;

        Span<char> locationBuffer = stackalloc char[separatorIndex];
        pathSpan[..separatorIndex].ToUpperInvariant(locationBuffer);
        var location = new string(locationBuffer);

        pathSpan = pathSpan[(separatorIndex + 1)..];

        if (!_registryMap.TryGetValue(location, out var openSubKeyFunc))
            return;

        var regKey = openSubKeyFunc.CreateSubKey(pathSpan.ToString());
    }

    public bool ExistsSubKey(string subkey)
    {
        ReadOnlySpan<char> pathSpan = subkey.AsSpan();
        if (pathSpan.StartsWith(ComputerPrefix, StringComparison.OrdinalIgnoreCase))
            pathSpan = pathSpan[ComputerPrefix.Length..];

        var separatorIndex = pathSpan.IndexOf(Path.DirectorySeparatorChar);
        if (separatorIndex == -1)
            return false;

        Span<char> locationBuffer = stackalloc char[separatorIndex];
        pathSpan[..separatorIndex].ToUpperInvariant(locationBuffer);
        var location = new string(locationBuffer);

        pathSpan = pathSpan[(separatorIndex + 1)..];

        if (!_registryMap.TryGetValue(location, out var openSubKeyFunc))
            return false;

        var regKey = openSubKeyFunc.OpenSubKey(pathSpan.ToString());
        return regKey != null;
    }

    public string[]? GetSubKeyNames(string subkey)
    {
        var regKey = GetRegistryKey(subkey, false);
        return regKey?.GetSubKeyNames();
    }

    public string[]? GetValueNames(string subkey)
    {
        var regKey = GetRegistryKey(subkey, false);
        return regKey?.GetValueNames();
    }
}
