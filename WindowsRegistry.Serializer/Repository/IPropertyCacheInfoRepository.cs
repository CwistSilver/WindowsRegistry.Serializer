using WindowsRegistry.Serializer.Data;

namespace WindowsRegistry.Serializer.Repository;
public interface IPropertyCacheInfoRepository
{
    List<PropertyCacheInfo> GetPropertyCacheInfos(Type type);
}