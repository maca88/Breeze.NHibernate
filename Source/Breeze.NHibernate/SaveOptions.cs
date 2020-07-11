
using Newtonsoft.Json.Linq;

namespace Breeze.NHibernate
{
    /// <summary>
    /// The save option send by the client
    /// </summary>
    public class SaveOptions
    {
        /// <summary>
        /// Whether concurrent saves are allowed.
        /// </summary>
        public bool AllowConcurrentSaves { get; set; }

        /// <summary>
        /// Custom data send by the client.
        /// </summary>
        public JToken Tag { get; set; }
    }
}
