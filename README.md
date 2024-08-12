# WindowsRegistry.Serializer
<img width="192" height="auto" src="icon.png">

[![WindowsRegistry.Serializer](https://img.shields.io/nuget/vpre/WindowsRegistry.Serializer.svg?cacheSeconds=3600&label=WindowsRegistry.Serializer%20nuget)](https://www.nuget.org/packages/WindowsRegistry.Serializer)
[![NuGet](https://img.shields.io/nuget/dt/WindowsRegistry.Serializer.svg?cacheSeconds=3600&label=Downloads)](https://www.nuget.org/packages/WindowsRegistry.Serializer)

**'WindowsRegistry.Serializer'** is a .NET Standard 2.1 library for serializing and deserializing Windows Registry entries using defined classes similar to JsonSerializer.

## Features

- Serialize and deserialize Windows Registry entries with ease.
- Define classes to represent your registry data.
- Simple and intuitive API similar to JsonSerializer.

## Getting Started

### Define Your Classes
Create a class that represents your registry data. For example:
```cs
public class UninstallInformation
{
    [RegistryName("DisplayName")]
    public required string Name { get; set; }

    public string DisplayVersion { get; set; }
    public string InstallLocation { get; set; }
    public int EstimatedSize { get; set; }
    public DateTime? InstallDate { get; set; }
}
```

### Deserialize Data from the Registry
Read data from the registry using the `RegistrySerializer` class.
```cs
var uninstallInfo = RegistrySerializer.Deserialize<UninstallInformation>(@"Computer\HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\7-Zip");
Console.WriteLine($"Name: {uninstallInfo.Name}, DisplayVersion: {uninstallInfo.DisplayVersion}, InstallLocation: {uninstallInfo.InstallLocation}");
```

### Deserialize Lists from the Registry
You can also deserialize all subkeys as a list of a specific class.
```cs
var uninstallInfos = RegistrySerializer.Deserialize<IEnumerable<UninstallInformation>>(@"Computer\HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");

foreach (var info in uninstallInfos)
{
    Console.WriteLine($"Name: {info.Name}, DisplayVersion: {info.DisplayVersion}, InstallLocation: {info.InstallLocation}");
}
```

### Serialize Data to the Registry
Use the `RegistrySerializer` class to write data to the registry.
```cs
var uninstallInfo = new UninstallInformation
{
    Name = "Example Program",
    DisplayVersion = "1.0.0",
    InstallLocation = @"C:\Program Files\Example",
    EstimatedSize = 420
};

RegistrySerializer.Serialize<UninstallInformation>(@"Computer\HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Example", uninstallInfo);
```

## RegistryDeserializerPostProcess
The RegistryDeserializerPostProcess allows you to perform custom processing on properties after they are deserialized from the registry. This is useful for transforming data into a more useful format.

### Example
Suppose you have a property EstimatedSizeTest that needs to be processed after deserialization. First, define a post-process class:
```cs
public class EstimatedSizePostProcess : RegistryDeserializerPostProcess<int>
{
    public override int Effect(int data)
    {
        var intVal = (uint)data;
        return (int)intVal * 1000;
    }
}
```
Then, apply the post-process to the property using the RegistryDeserializerPostProcess attribute:
```cs
public class UninstallInformation
{
    [RegistryName("DisplayName")]
    public required string Name { get; set; }

    public string DisplayVersion { get; set; }
    public string InstallLocation { get; set; }

    [RegistryDeserializerPostProcess(typeof(EstimatedSizePostProcess))]
    public int EstimatedSize { get; set; }

    public DateTime? InstallDate { get; set; }
}
```
Now, whenever EstimatedSizeTest is deserialized, the EstimatedSizePostProcess will automatically be applied.

## Enums
WindowsRegistry.Serializer supports serialization and deserialization of enums either by their name or by their underlying value, similar to how JSON serialization works.

### Example
Suppose you have an enum `ShowcaseEnum`:
```cs
public enum ShowcaseEnum
{
    ValueA,
    ValueB,
    ValueC
}
```

And you have a property `Showcase` in your class:
```cs
public enum UninstallInformation
{
    public string Name { get; set; }
    public ShowcaseEnum Showcase { get; set; }
}
```
By default, the serializer will try to serialize and deserialize the enum based on its numeric value. During deserialization, if the numeric value cannot be mapped to an enum member, it will fall back to using the enum name.

### Using RegistryStringEnumConverter
If you want to explicitly use the enum names for serialization and deserialization, you can use the RegistryStringEnumConverter. This is a standard converter provided by the library.
```cs
public enum UninstallInformation
{
    public string Name { get; set; }

    [RegistryConverter(typeof(RegistryStringEnumConverter))]
    public ShowcaseEnum Showcase { get; set; }
}
```
With the `RegistryStringEnumConverter`, the enum will be serialized to and deserialized from the registry using its name rather than its numeric value.

## RegistryConverter

The WindowsRegistry.Serializer library allows you to customize how properties are serialized and deserialized by creating your own `RegistryConverter`. While the library provides some standard converters, such as `RegistryStringEnumConverter`, you can also implement your own custom converters.

### Example: Custom Float Converter

Suppose you need a custom converter for `float` values. Here’s how you can implement and use a custom `FloatTestConverter`:

#### Implementing a Custom Converter

First, define your custom converter by inheriting from `RegistryConverter<T>`:
```cs
public class FloatTestConverter : RegistryConverter<float>
{
    public override bool TryRead(object registryData, RegistrySerializerOptions registrySerializerOptions, out float result)
    {
        if (registryData is float floatValue)
        {
            result = floatValue;
            return true;
        }

        return float.TryParse(registryData.ToString(), NumberStyles.AllowThousands | NumberStyles.Float, registrySerializerOptions.Culture, out result);
    }

    public override void Write(object? propertyValue, RegistrySerializerOptions registrySerializerOptions, out object? valueToWrite)
    {
        if (propertyValue is float floatValue)
        {
            valueToWrite = floatValue.ToString(registrySerializerOptions.Culture);
            return;
        }

        valueToWrite = propertyValue;
    }
}
```
This custom converter attempts to read and write float values, handling both float and string representations.

### Using the Custom Converter
To use the custom converter, apply the RegistryConverter attribute to the property:
```cs
public enum UninstallInformation
{
    public string Name { get; set; }

    [RegistryConverter(typeof(FloatTestConverter))]
    public float ConverterTestField { get; set; }
}
```
With this setup, the `ConverterTestField` property will be serialized and deserialized using the custom `FloatTestConverter`.


## Ignoring Properties
The WindowsRegistry.Serializer library allows you to ignore certain properties during serialization using the `[RegistryIgnore]` attribute. This can be useful for excluding properties based on specific conditions.

### RegistryIgnoreCondition Enum

The `RegistryIgnoreCondition` enum provides the following options for ignoring properties:

- `Never`: The property is never ignored.
- `Always`: The property is always ignored.
- `WhenWritingDefault`: The property is ignored when it has the default value for its type.
- `WhenWritingNull`: The property is ignored when it is null.

### Example

Suppose you have a class with a property that you want to ignore based on its default value:
```cs
public enum UninstallInformation
{
    public string Name { get; set; }

    [RegistryIgnore(RegistryIgnoreCondition.WhenWritingDefault)]
    public int EstimatedSize { get; set; }
}
```
In this example, the EstimatedSize property will be ignored during serialization if it has the default value for an int (which is 0).

You can also use other conditions, such as:
```cs
[RegistryIgnore(RegistryIgnoreCondition.WhenWritingNull)]
public string? OptionalProperty { get; set; }
```
This will ignore the OptionalProperty during serialization if it is null.


## Custom Property Names

The WindowsRegistry.Serializer library allows you to map class properties to different names in the registry using the `[RegistryName]` and `[RegistryDeserializeNames]` attributes. This is useful when the registry keys have different names than your class properties.

### RegistryName Attribute

The `[RegistryName]` attribute specifies the name of the registry key to look for, instead of using the property name.

#### Example

Suppose you have a class with a property that maps to a different name in the registry:
```cs
public class UninstallInformation
{
    [RegistryName("DisplayName")]
    public required string Name { get; set; }

    public string DisplayVersion { get; set; }
    public string InstallLocation { get; set; }
    public int EstimatedSize { get; set; }
    public DateTime? InstallDate { get; set; }
}
```
In this example, the Name property will be mapped to the DisplayName key in the registry.

### RegistryDeserializeNames Attribute
The `[RegistryDeserializeNames]` attribute allows you to specify multiple potential names for a registry key. This is useful when the key might have different names in different contexts or versions.

#### Example
Suppose you have a property that could be found under multiple names in the registry:
```cs
public class UninstallInformation
{
    [RegistryDeserializeNames("DisplayName", "ProductName", "AppName")]
    public required string Name { get; set; }

    public string DisplayVersion { get; set; }
    public string InstallLocation { get; set; }
    public int EstimatedSize { get; set; }
    public DateTime? InstallDate { get; set; }
}
```
In this example, the `Name` property will be deserialized by looking for the `DisplayName` key first. If `DisplayName` is not found, it will then look for `ProductName`, and if `ProductName` is not found, it will finally look for `AppName`.

## Dependencies
The **'WindowsRegistry.Serializer'** library has a single dependency that is required to access the Windows Registry.

- [Microsoft.Win32.Registry](https://www.nuget.org/packages/Microsoft.Win32.Registry/)
