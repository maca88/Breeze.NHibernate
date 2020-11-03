using System;
using System.Collections.Generic;
using Breeze.NHibernate.Metadata;
using NHibernate;
using NHibernate.Type;

namespace Breeze.NHibernate
{
    /// <inheritdoc />
    public class DefaultDataTypeProvider : IDataTypeProvider
    {
        /// <summary>
        /// Map of NH datatype to Breeze datatype.
        /// </summary>
        protected readonly Dictionary<string, DataType> BreezeTypeMap = new Dictionary<string, DataType>
        {
            {NHibernateUtil.Binary.Name, DataType.Binary},
            {NHibernateUtil.BinaryBlob.Name, DataType.Binary},
#pragma warning disable 618
            {NHibernateUtil.Timestamp.Name, DataType.DateTime},
#pragma warning restore 618
            {NHibernateUtil.TimeAsTimeSpan.Name, DataType.Time},
            {NHibernateUtil.UtcDateTime.Name, DataType.DateTime},
            {NHibernateUtil.LocalDateTime.Name, DataType.DateTime}
        };

        /// <summary>
        /// List of DataTypes that have getNext method implemented in BreezeJS.
        /// </summary>
        protected readonly HashSet<DataType> SupportedClientDataTypeGenerators = new HashSet<DataType>
        {
            DataType.String,
            DataType.Int64,
            DataType.Int32,
            DataType.Int16,
            DataType.Decimal,
            DataType.Double,
            DataType.Single,
            DataType.DateTime,
            DataType.DateTimeOffset,
            DataType.Guid
        };

        /// <inheritdoc />
        public virtual bool TryGetDataType(Type type, out DataType dataType)
        {
            return TryGetDataType(NHibernateUtil.GuessType(type), out dataType);
        }

        /// <inheritdoc />
        public virtual bool TryGetDataType(IType type, out DataType dataType)
        {
            if (type.IsComponentType)
            {
                dataType = DataType.Undefined;
                return false;
            }

            if (BreezeTypeMap.TryGetValue(type.Name, out dataType))
            {
                return true;
            }

            if (Enum.TryParse(type.Name, out dataType))
            {
                return true;
            }

            if (type is AbstractEnumType || type is AbstractStringType)
            {
                dataType = DataType.String;
                return true;
            }

            dataType = DataType.Undefined;
            return false;
        }

        /// <inheritdoc />
        public virtual DataType GetDefaultType()
        {
            return DataType.Undefined;
        }

        /// <inheritdoc />
        public virtual bool HasClientGenerator(IType type)
        {
            return TryGetDataType(type, out var dataType) && SupportedClientDataTypeGenerators.Contains(dataType);
        }
    }
}
