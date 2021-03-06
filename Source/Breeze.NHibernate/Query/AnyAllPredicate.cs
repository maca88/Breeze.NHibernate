// Copied from Breeze.Core
#pragma warning disable CS1591
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Breeze.NHibernate;

namespace Breeze.Core {
  // Copied from Breeze.Core
  public class AnyAllPredicate : BasePredicate {

    public Object ExprSource { get; private set; }
    public PropBlock NavPropBlock { get; private set; } // calculated as a result of validate;
    public BasePredicate Predicate { get; private set; } 


    public AnyAllPredicate(Operator op, Object exprSource, BasePredicate predicate) : base(op) {
      ExprSource = exprSource;
      Predicate = predicate;
    }

    #region Modified code - Added entityMetadataProvider parameter
    public override void Validate(Type entityType, IEntityMetadataProvider entityMetadataProvider) {
      var block = BaseBlock.CreateLHSBlock(ExprSource, entityType, entityMetadataProvider);
      if (!(block is PropBlock)) {
        throw new Exception("The first expression of this AnyAllPredicate must be a PropertyExpression");
      }
      this.NavPropBlock = (PropBlock)block;
      var prop = NavPropBlock.Property;
      if (prop.IsDataProperty || prop.ElementType == null) {
        throw new Exception("The first expression of this AnyAllPredicate must be a nonscalar Navigation PropertyExpression");
      }

      
      this.Predicate.Validate(prop.ElementType, entityMetadataProvider);

    }
    #endregion

    public override Expression ToExpression(ParameterExpression paramExpr) {
      var navExpr = NavPropBlock.ToExpression(paramExpr);
      var elementType = NavPropBlock.Property.ElementType;
      MethodInfo mi;
      if (Operator == Operator.Any) {
        mi = TypeFns.GetMethodByExample((IEnumerable<String> list) => list.Any(x => x != null), elementType);
      } else {
        mi = TypeFns.GetMethodByExample((IEnumerable<String> list) => list.All(x => x != null), elementType);
      }
      var lambdaExpr = Predicate.ToLambda(elementType);
      var result = Expression.Call(mi, navExpr, lambdaExpr);
      return result;
    }
  }
}
