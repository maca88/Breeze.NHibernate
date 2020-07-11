// Copied from Breeze.Core
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Breeze.NHibernate;

namespace Breeze.Core {
  /**
   * Represents a single orderBy clause that will be part of an EntityQuery. An orderBy 
   * clause represents either the name of a property or a path to the property of another entity via its navigation path 
   * from the current EntityType for a given query. 
   * @author IdeaBlade
   *
   */
  public class OrderByClause {

    private List<OrderByItem> _orderByItems;
    private List<String> _propertyPaths;

    
    // need to be able to take in a List<Object>
    public static OrderByClause From(IEnumerable propertyPaths) {
      return (propertyPaths == null) ? null : new OrderByClause(propertyPaths.Cast<String>());
    }

    public OrderByClause(IEnumerable<String> propertyPaths) {
      _propertyPaths = propertyPaths.ToList();
      _orderByItems = _propertyPaths.Select(pp => {
        var itemTrimmed = Regex.Replace(pp, @"\s+", " ").Trim();
        String[] itemParts = itemTrimmed.Split(' ');
        var isDesc = itemParts.Length == 1 ? false : itemParts[1].Equals("desc");
        return new OrderByItem(itemParts[0], isDesc);
      }).ToList();
    }

    #region Modified code - Added entityMetadataProvider parameter
    public void Validate(Type entityType, IEntityMetadataProvider entityMetadataProvider) {
      foreach (OrderByItem item in _orderByItems) {
        item.Validate(entityType, entityMetadataProvider);
      }
    }
    #endregion

    public IEnumerable<String> PropertyPaths {
      get { return _propertyPaths.AsReadOnly(); }
    }

    public IEnumerable<OrderByItem> OrderByItems {
      get { return _orderByItems.AsReadOnly(); }
    }

    public class OrderByItem {
      public string PropertyPath { get; private set; }
      public bool IsDesc { get; private set; }
      public PropertySignature Property { get; private set; }

      public OrderByItem(String propertyPath, bool isDesc) {
        PropertyPath = propertyPath;
        IsDesc = isDesc;
      }

      #region Modified code - Added entityMetadataProvider parameter
      public void Validate(Type entityType, IEntityMetadataProvider entityMetadataProvider) {
        Property = new PropertySignature(entityType, PropertyPath, entityMetadataProvider);
      }
      #endregion
    }
  }
}
