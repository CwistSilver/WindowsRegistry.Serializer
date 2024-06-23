using System.Globalization;
using WindowsRegistry.Serializer.RegistryConverters;

namespace WindowsRegistry.Serializer.Data;
public class RegistrySerializerOptions
{
    internal const string DefaultDateTimeFormat = "yyyy-MM-ddTHH:mm:ss";
    internal const string DefaultDateTimeOffsetFormat = "yyyy-MM-ddTHH:mm:ss.fffffffzzz";
    internal const string DefaultGuidFormat = "D";

    private static readonly GenericRegistryConverter _genericRegistryConverter = new();
    private static readonly List<RegistryConverter> _defaultConverters =
    [
            new RegistryDateTimeConverter(),
            new RegistryIntConverter(),
            new RegistryStringConverter(),
            new RegistryVersionConverter(),
            new RegistryEnumConverter(),
            new RegistryBooleanConverter(),
            new RegistryDateTimeOffsetConverter(),
            new RegistryFloatConverter(),
    ];

    private readonly List<RegistryConverter> _converters = [];
    public IList<RegistryConverter> Converters => _converters;

    private static RegistrySerializerOptions? _default;
    public static RegistrySerializerOptions Default
    {
        get
        {
            _default ??= new RegistrySerializerOptions()
            {
                IgnoreThrownExceptions = true,
                DefaultIgnoreCondition = RegistryIgnoreCondition.WhenWritingDefault,
                CreateSubKeyWhenMissing = true
            };

            return _default;
        }
    }

    public bool IgnoreThrownExceptions { get; set; }
    public RegistryIgnoreCondition DefaultIgnoreCondition { get; set; } = RegistryIgnoreCondition.WhenWritingDefault;
    public bool CreateSubKeyWhenMissing { get; set; }
    public string? DateFormatString { get; set; }
    public string? DateTimeOffsetFormatString { get; set; }
    public string? GuidFormatString { get; set; }
    public DateTimeStyles DateStyles { get; set; } = DateTimeStyles.RoundtripKind;
    public CultureInfo Culture { get; set; } = CultureInfo.InvariantCulture;

    public void AddConverter<T>(RegistryConverter<T> converter) => _converters.Add(converter);

    public RegistryConverter GetConverter(PropertyCacheInfo propertyCacheInfo)
    {
        Type underlyingType = propertyCacheInfo.UnderlyingTyp ?? propertyCacheInfo.Property.PropertyType;

        foreach (var converter in _converters)
        {
            if (converter.CanConvert(underlyingType))
                return converter;
        }

        return _defaultConverters.FirstOrDefault(converter => converter.CanConvert(underlyingType)) ?? _genericRegistryConverter;
    }

    public RegistryConverter GetConverter(Type type)
    {
        foreach (var converter in _converters)
        {
            if (converter.CanConvert(type))
                return converter;
        }

        return _defaultConverters.FirstOrDefault(converter => converter.CanConvert(type)) ?? _genericRegistryConverter;
    }
}
