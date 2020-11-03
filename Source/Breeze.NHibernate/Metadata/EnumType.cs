namespace Breeze.NHibernate.Metadata
{
    /// <summary>
    /// An enum type.
    /// </summary>
    public class EnumType
    {
        /// <summary>
        /// The name of the enum.
        /// </summary>
        public string ShortName { get; set; }

        /// <summary>
        /// The enum namespace.
        /// </summary>
        public string Namespace { get; set; }

        /// <summary>
        /// An array of enum values.
        /// </summary>
        public string[] Values { get; set; }
    }
}
