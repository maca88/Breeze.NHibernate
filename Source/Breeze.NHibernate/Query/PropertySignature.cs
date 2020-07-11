// Copied from Breeze.Core
using System;
using System.Reflection;
using System.Linq;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Text;
using Breeze.NHibernate;
using Expression = System.Linq.Expressions.Expression;


namespace Breeze.Core {

  public class PropertySignature {
    private readonly HashSet<PropertyInfo> _syntheticProperties = new HashSet<PropertyInfo>();

    #region Modified code - Added entityMetadataProvider parameter
    public PropertySignature(Type instanceType, String propertyPath, IEntityMetadataProvider entityMetadataProvider) {
      InstanceType = instanceType;
      PropertyPath = propertyPath;
      Properties = GetProperties(InstanceType, PropertyPath, entityMetadataProvider, _syntheticProperties).ToList();
    }
    #endregion

    public static bool IsProperty(Type instanceType, String propertyPath, IEntityMetadataProvider entityMetadataProvider) {
      return GetProperties(instanceType, propertyPath, entityMetadataProvider, null, false).Any(pi => pi != null);
    }

    public Type InstanceType { get; private set; }
    public String PropertyPath { get; private set; }
    public List<PropertyInfo> Properties { get; private set; }

    #region Modified code - Avoid adding underscore for synthetic properties
    public String Name {
      get {
        StringBuilder sb = null;
        foreach (var property in Properties) {
          if (sb == null) {
            sb = new StringBuilder();
          } else if (!_syntheticProperties.Contains(property)) {
            sb.Append("_");
          }
          sb.Append(property.Name);
        }
        return sb?.ToString();
      }
    }
    #endregion

    public Type ReturnType {
      get { return Properties.Last().PropertyType; }
    }

    // returns null for scalar properties
    public Type ElementType {
      get { return TypeFns.GetElementType(ReturnType); }

    }

    public bool IsDataProperty {
      get { return TypeFns.IsPredefinedType(ReturnType) || TypeFns.IsEnumType(ReturnType); }
    }

    public bool IsNavigationProperty {
      get { return !IsDataProperty; }
    }



    #region Modified code - Added entityMetadataProvider parameter
    // returns an IEnumerable<PropertyInfo> with nulls if invalid and throwOnError = true
    public static IEnumerable<PropertyInfo> GetProperties(
      Type instanceType,
      String propertyPath,
      IEntityMetadataProvider entityMetadataProvider,
      bool throwOnError = true) {
      return GetProperties(instanceType, propertyPath, entityMetadataProvider, null, throwOnError);
    }

    private static IEnumerable<PropertyInfo> GetProperties(Type instanceType, String propertyPath, IEntityMetadataProvider entityMetadataProvider,
      ICollection<PropertyInfo> syntheticProperties, bool throwOnError = true) {
      var propertyNames = propertyPath.Split('.');

      var nextInstanceType = instanceType;
      var nextMetadata = entityMetadataProvider.IsEntityType(nextInstanceType) ? entityMetadataProvider.GetMetadata(instanceType) : null;
      foreach (var propertyName in propertyNames) {
        PropertyInfo property;
        if (nextMetadata != null && nextMetadata.SyntheticForeignKeyProperties.TryGetValue(propertyName, out var syntheticProperty)) {
          yield return GetProperty(nextInstanceType, syntheticProperty.AssociationPropertyName, throwOnError);
          property = GetProperty(syntheticProperty.EntityType, syntheticProperty.IdentifierPropertyName, throwOnError);
          syntheticProperties?.Add(property);
        } else {
          property = GetProperty(nextInstanceType, propertyName, throwOnError);
        }

        if (property != null) {
          yield return property;

          nextInstanceType = property.PropertyType;
          nextMetadata = entityMetadataProvider.IsEntityType(nextInstanceType) ? entityMetadataProvider.GetMetadata(nextInstanceType) : null;
        } else {
          break;
        }
      }
    }
    #endregion

    private static PropertyInfo GetProperty(Type instanceType, String propertyName, bool throwOnError = true) {
      var propertyInfo = (PropertyInfo)TypeFns.FindPropertyOrField(instanceType, propertyName,
        BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public);
      if (propertyInfo == null) {
        if (throwOnError) {
          var msg = String.Format("Unable to locate property '{0}' on type '{1}'.", propertyName, instanceType);
          throw new Exception(msg);
        } else {
          return null;
        }
      }
      return propertyInfo;
    }

    public Expression BuildMemberExpression(ParameterExpression parmExpr) {
      Expression memberExpr = BuildPropertyExpression(parmExpr, Properties.First());
      foreach (var property in Properties.Skip(1)) {
        memberExpr = BuildPropertyExpression(memberExpr, property);
      }
      return memberExpr;
    }

    public Expression BuildPropertyExpression(Expression baseExpr, PropertyInfo property) {
      return Expression.Property(baseExpr, property);
    }



  }


}
