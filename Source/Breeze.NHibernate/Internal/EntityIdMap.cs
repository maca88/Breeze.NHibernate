using System;
using System.Collections.Generic;

namespace Breeze.NHibernate.Internal
{
    internal class EntityIdMap : Dictionary<Type, Dictionary<object, EntityInfo>>
    {
        public EntityIdMap(int capacity) : base(capacity)
        {

        }

        public Dictionary<object, EntityInfo> GetTypeIdMap(Type type, int? capacity = null)
        {
            if (TryGetValue(type, out var idMap))
            {
                return idMap;
            }

            idMap = capacity.HasValue
                ? new Dictionary<object, EntityInfo>(capacity.Value)
                : new Dictionary<object, EntityInfo>();
            Add(type, idMap);

            return idMap;
        }

        public void AddToDerivedTypes(EntityInfo entityInfo, object id, int? capacity = null)
        {
            var metadata = entityInfo.EntityMetadata;
            if (metadata.DerivedTypes.Count == 0)
            {
                return;
            }

            foreach (var derivedType in metadata.DerivedTypes)
            {
                var idMap = GetTypeIdMap(derivedType, capacity);
                idMap.Add(id, entityInfo);
            }
        }
    }
}
