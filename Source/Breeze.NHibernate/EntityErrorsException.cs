using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Breeze.NHibernate
{
    /// <summary>
    /// An exception thrown when one or more <see cref="EntityError"/> occur.
    /// </summary>
    public class EntityErrorsException : Exception
    {
        /// <summary>
        /// Constructs an instance of <see cref="EntityErrorsException"/>.
        /// </summary>
        public EntityErrorsException(IEnumerable<EntityError> entityErrors)
        {
            EntityErrors = entityErrors.ToList();
            StatusCode = HttpStatusCode.BadRequest;
        }

        /// <summary>
        /// Constructs an instance of <see cref="EntityErrorsException"/>.
        /// </summary>
        public EntityErrorsException(string message, IEnumerable<EntityError> entityErrors)
            : base(message)
        {
            EntityErrors = entityErrors.ToList();
            StatusCode = HttpStatusCode.BadRequest;
        }

        /// <summary>
        /// The http status to use for the http response message.
        /// </summary>
        public HttpStatusCode StatusCode { get; set; }

        /// <summary>
        /// The list of entity errors.
        /// </summary>
        public List<EntityError> EntityErrors { get; protected set; }
    }
}
