using System;
using System.Collections.Generic;
using Breeze.NHibernate.Metadata;
using NHibernate;
using NHibernate.Type;

namespace Breeze.NHibernate
{
    /// <summary>
    /// Provides information about <see cref="DataType"/> and its relation with NHibernate types.
    /// </summary>
    public interface IDataTypeProvider
    {
        /// <summary>
        /// Tries to get the <see cref="DataType"/> for he given <see cref="IType"/>.
        /// </summary>
        /// <param name="type">The NHibernate type.</param>
        /// <param name="dataType">The returned data type.</param>
        /// <returns>Whether the data type was found.</returns>
        bool TryGetDataType(IType type, out DataType dataType);

        /// <summary>
        /// Tries to get the <see cref="DataType"/> for he given <see cref="Type"/>.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="dataType">The returned data type.</param>
        /// <returns>Whether the data type was found.</returns>
        bool TryGetDataType(Type type, out DataType dataType);

        /// <summary>
        /// Gets the default <see cref="DataType"/>.
        /// </summary>
        /// <returns>The default <see cref="DataType"/>.</returns>
        DataType GetDefaultType();

        /// <summary>
        /// Gets whether the given <see cref="IType"/> has a client generator.
        /// </summary>
        /// <param name="type">The NHibernate type.</param>
        /// <returns>Whether the client generator exists.</returns>
        bool HasClientGenerator(IType type);
    }
}
