using System.Collections.Generic;

namespace Breeze.NHibernate.Metadata
{
    public abstract class BaseProperty : MetadataObject
    {
        /// <summary>
        /// The client side name of this property.
        /// </summary>
        public string Name
        {
            get => Get<string>(nameof(Name));
            set => Set(nameof(Name), value);
        }

        public string DisplayName
        {
            get => Get<string>(nameof(DisplayName));
            set => Set(nameof(DisplayName), value);
        }

        /// <summary>
        /// The server side side name of this property. Either name or nameOnServer must be specified and either is sufficient.
        /// </summary>
        public string NameOnServer
        {
            get => Get<string>(nameof(NameOnServer));
            set => Set(nameof(NameOnServer), value);
        }

        public object Custom
        {
            get => Get<object>(nameof(Custom));
            set => Set(nameof(Custom), value);
        }

        /// <summary>
        /// A list of the validators (validations) that will be associated with this property.
        /// </summary>
        public IReadOnlyCollection<Validator> Validators
        {
            get => Get<IReadOnlyCollection<Validator>>(nameof(Validators));
            set => Set(nameof(Validators), value);
        }
    }
}
