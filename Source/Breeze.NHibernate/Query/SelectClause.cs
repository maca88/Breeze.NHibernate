// Copied from Breeze.Core
#pragma warning disable CS1591
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Breeze.NHibernate;

namespace Breeze.Core {
  /**
   * Represents a single selectClause clause that will be part of an EntityQuery. An orderBy 
   * clause represents either the name of a property or a path to the property of another entity via its navigation path 
   * from the current EntityType for a given query. 
   * @author IdeaBlade
   *
   */
  public class SelectClause {
    private List<String> _propertyPaths;
    private List<PropertySignature> _properties;

    public static SelectClause From(IEnumerable propertyPaths) {
      return (propertyPaths == null) ? null : new SelectClause(propertyPaths.Cast<String>());
    }

    public SelectClause(IEnumerable<String> propertyPaths) {
      _propertyPaths = propertyPaths.ToList();
    }


    public IEnumerable<String> PropertyPaths {
      get { return _propertyPaths.AsReadOnly(); }
    }

    public IEnumerable<PropertySignature> Properties {
      get { return _properties.AsReadOnly(); }
    }

    #region Modified code - Added entityMetadataProvider parameter
    public void Validate(Type entityType, IEntityMetadataProvider entityMetadataProvider) {
      _properties = _propertyPaths.Select(pp => new PropertySignature(entityType, pp, entityMetadataProvider)).ToList();
    }
    #endregion
  }
}
