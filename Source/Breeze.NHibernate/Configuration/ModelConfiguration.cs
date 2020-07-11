﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Breeze.NHibernate.Metadata;
using Breeze.NHibernate.Serialization;
using NHibernate;
using NHibernate.Cfg;

namespace Breeze.NHibernate.Configuration
{
    /// <summary>
    /// The model configuration used by <see cref="BreezeMetadataBuilder"/> and <see cref="BreezeContractResolver"/>.
    /// </summary>
    public class ModelConfiguration
    {
        private readonly ConcurrentDictionary<string, SyntheticMemberConfiguration> _syntheticMembers =
            new ConcurrentDictionary<string, SyntheticMemberConfiguration>();

        internal ModelConfiguration(Type type, IReadOnlyDictionary<string, MemberConfiguration> members)
        {
            ModelType = type;
            Members = members;
        }

        /// <summary>
        /// The model type.
        /// </summary>
        public Type ModelType { get; }

        /// <summary>
        /// The resource name that will be used when building <see cref="BreezeMetadata"/>.
        /// </summary>
        public string ResourceName { get; internal set; }

        /// <summary>
        /// Whether a <see cref="ISession.Refresh(object)"/> should be called after saving the model into the database. Default <see langword="false" />.
        /// </summary>
        public bool? RefreshAfterSave { get; internal set; }

        /// <summary>
        /// Whether a <see cref="ISession.Refresh(object)"/> should be called after updating the model in the database. Default <see langword="false" />.
        /// </summary>
        public bool? RefreshAfterUpdate { get; internal set; }

        /// <summary>
        /// The batch size for fetching existing entities. Default NHibernate <see cref="Settings.DefaultBatchFetchSize"/>.
        /// </summary>
        public int? BatchFetchSize { get; internal set; }

        /// <summary>
        /// Custom data that will be used when building <see cref="BreezeMetadata"/>.
        /// </summary>
        public object Custom { get; internal set; }

        /// <summary>
        /// The <see cref="AutoGeneratedKeyType"/> that will be used when building <see cref="BreezeMetadata"/>. Default <see cref="ModelMetadata.AutoGeneratedKeyType"/>.
        /// </summary>
        public AutoGeneratedKeyType? AutoGeneratedKeyType { get; internal set; }

        /// <summary>
        /// The members configuration.
        /// </summary>
        public IReadOnlyDictionary<string, MemberConfiguration> Members { get; }

        /// <summary>
        /// The synthetic members configuration.
        /// </summary>
        public IReadOnlyDictionary<string, SyntheticMemberConfiguration> SyntheticMembers => _syntheticMembers;

        /// <summary>
        /// Gets the member configuration for the given name.
        /// </summary>
        /// <param name="memberName">The member name.</param>
        /// <returns>The member configuration or <see langword="null" /> if not found.</returns>
        public MemberConfiguration GetMember(string memberName)
        {
            if (!Members.TryGetValue(memberName, out var configuration))
            {
                return null;
            }

            return configuration;
        }

        /// <summary>
        /// Gets the synthetic member configuration for the given name.
        /// </summary>
        /// <param name="memberName">The synthetic member name.</param>
        /// <returns>The synthetic member configuration or <see langword="null" /> if not found.</returns>
        public SyntheticMemberConfiguration GetSyntheticMember(string memberName)
        {
            return _syntheticMembers.TryGetValue(memberName, out var value) ? value : null;
        }

        internal SyntheticMemberConfiguration GetOrAdd(string memberName, Func<string, SyntheticMemberConfiguration> valueFactory)
        {
            return _syntheticMembers.GetOrAdd(memberName, valueFactory);
        }

        internal void MergeWith(ModelConfiguration model)
        {
            ResourceName = model.ResourceName ?? ResourceName;
            AutoGeneratedKeyType = model.AutoGeneratedKeyType ?? AutoGeneratedKeyType;
            RefreshAfterSave = model.RefreshAfterSave ?? RefreshAfterSave;
            RefreshAfterUpdate = model.RefreshAfterUpdate ?? RefreshAfterUpdate;
            BatchFetchSize = model.BatchFetchSize ?? BatchFetchSize;
            Custom = model.Custom ?? Custom;
        }
    }
}
