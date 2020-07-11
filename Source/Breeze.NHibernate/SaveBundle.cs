using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Breeze.NHibernate
{
    /// <summary>
    /// Contains data send by the client to be saved.
    /// </summary>
    public class SaveBundle
    {
        /// <summary>
        /// List of entities with their modifications.
        /// </summary>
        public List<JObject> Entities { get; set; }

        /// <summary>
        /// The save options.
        /// </summary>
        public SaveOptions SaveOptions { get; set; }
    }
}
