
namespace Breeze.NHibernate
{
    /// <summary>
    /// Represents an entity error.
    /// </summary>
    public class EntityError
    {
        /// <summary>
        /// The error name.
        /// </summary>
        public string ErrorName { get; set; }

        /// <summary>
        /// The entity type name.
        /// </summary>
        public string EntityTypeName { get; set; }

        /// <summary>
        /// The entity key values.
        /// </summary>
        public object[] KeyValues { get; set; }

        /// <summary>
        /// The entity property name that contains the error.
        /// </summary>
        public string PropertyName { get; set; }

        /// <summary>
        /// The error message.
        /// </summary>
        public string ErrorMessage { get; set; }
    }
}
