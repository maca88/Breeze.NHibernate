
namespace Breeze.NHibernate.Metadata
{
    public class DataService : MetadataObject
    {
        public string ServiceName
        {
            get => Get<string>(nameof(ServiceName));
            set => Set(nameof(ServiceName), value);
        }

        /// <summary>
        /// On deserialization, this must match the name of some 'dataService adapter' already registered on the breeze client.
        /// </summary>
        public string AdapterName
        {
            get => Get<string>(nameof(AdapterName));
            set => Set(nameof(AdapterName), value);
        }

        /// <summary>
        /// Whether the server can provide metadata for this service.
        /// </summary>
        public bool HasServerMetadata
        {
            get => Get(nameof(HasServerMetadata), true);
            set => Set(nameof(HasServerMetadata), value, true);
        }

        /// <summary>
        /// On deserialization, this must match the name of some jsonResultsAdapter registered on the breeze client.
        /// </summary>
        public string JsonResultsAdapter
        {
            get => Get<string>(nameof(JsonResultsAdapter));
            set => Set(nameof(JsonResultsAdapter), value);
        }

        /// <summary>
        /// Whether to use JSONP when performing a 'GET' request against this service.
        /// </summary>
        public string UseJsonp
        {
            get => Get<string>(nameof(UseJsonp));
            set => Set(nameof(UseJsonp), value);
        }

        /// <summary>
        /// The name of the uriBuilder to be used with this service.
        /// </summary>
        public string UriBuilderName
        {
            get => Get<string>(nameof(UriBuilderName));
            set => Set(nameof(UriBuilderName), value);
        }
    }
}
