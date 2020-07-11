// Copied from Breeze.Core
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Breeze.NHibernate;

namespace Breeze.Core {
  public abstract class BaseBlock {

    // will return either a PropBlock or a FnBlock
    #region Modified code - Added entityMetadataProvider parameter
    public static BaseBlock CreateLHSBlock(Object exprSource,
        Type entityType, IEntityMetadataProvider entityMetadataProvider) {
      if (exprSource == null) {
        throw new Exception(
            "Null expressions are only permitted on the right hand side of a BinaryPredicate");
      }

      if (exprSource is IDictionary) {
        throw new Exception(
            "Object expressions are only permitted on the right hand side of a BinaryPredicate");
      }

      if (exprSource is IList) {
        throw new Exception(
            "Array expressions are only permitted on the right hand side of a BinaryPredicate");
      }

      if (!(exprSource is String)) {
        throw new Exception(
            "Only string expressions are permitted on this predicate");
      }

      String source = (String)exprSource;
      if (source.IndexOf("(") == -1) {
        return new PropBlock(source, entityType, entityMetadataProvider);
      } else {
        return FnBlock.CreateFrom(source, entityType, entityMetadataProvider);
      }


    }
    #endregion

    // will return either a PropBlock or a LitBlock
    #region Modified code - Added entityMetadataProvider parameter
    public static BaseBlock CreateRHSBlock(Object exprSource,
        Type entityType, DataType otherExprDataType, IEntityMetadataProvider entityMetadataProvider) {

      if (exprSource == null) {
        return new LitBlock(exprSource, otherExprDataType);
      }

      if (exprSource is String) {
        String source = (String)exprSource;
        if (entityType == null) {
          // if entityType is unknown then assume that the rhs is a
          // literal
          return new LitBlock(source, otherExprDataType);
        }

        if (PropertySignature.IsProperty(entityType, source, entityMetadataProvider)) {
          return new PropBlock(source, entityType, entityMetadataProvider);
        } else { 
          return new LitBlock(source, otherExprDataType);
        } 
      }

      if (TypeFns.IsPredefinedType(exprSource.GetType())) {
        return new LitBlock(exprSource, otherExprDataType);
      }

      if (exprSource is IDictionary<string, Object>) {
        var exprMap = (IDictionary<string, Object>)exprSource;
        // note that this is NOT the same a using get and checking for null
        // because null is a valid 'value'.
        if (!exprMap.ContainsKey("value")) {
          throw new Exception(
              "Unable to locate a 'value' property on: "
                  + exprMap.ToString());
        }
        Object value = exprMap["value"];

        if (exprMap.ContainsKey("isProperty")) {
          return new PropBlock((String)value, entityType, entityMetadataProvider);
        } else {
          String dt = (String)exprMap["dataType"];
          DataType dataType = (dt != null) ? DataType.FromName(dt) : otherExprDataType;
          return new LitBlock(value, dataType);
        }
      }

      if (exprSource is IList) {
        // right now this pretty much implies the values on an 'in' clause
        return new LitBlock(exprSource, otherExprDataType);
      }

      if (TypeFns.IsEnumType(exprSource.GetType())) {
        return new LitBlock(exprSource, otherExprDataType);
      }

      throw new Exception(
          "Unable to parse the right hand side of this BinaryExpression: "
              + exprSource.ToString());

    }
    #endregion

    public abstract DataType DataType {
      get;
    }

    

    public abstract Expression ToExpression(Expression inExpr);

  }

}
