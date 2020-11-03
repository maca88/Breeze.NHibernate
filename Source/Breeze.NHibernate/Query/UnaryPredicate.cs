// Copied from Breeze.Core
#pragma warning disable CS1591
using System;
using System.Linq.Expressions;
using Breeze.NHibernate;

namespace Breeze.Core {

  public class UnaryPredicate : BasePredicate {
    
    public BasePredicate Predicate { get; private set; }
  
    public UnaryPredicate(Operator op, BasePredicate predicate) : base(op) {
      Predicate = predicate;
    }


    #region Modified code - Added entityMetadataProvider parameter
    public override void Validate(Type entityType, IEntityMetadataProvider entityMetadataProvider) {
      Predicate.Validate(entityType, entityMetadataProvider);
    }
    #endregion

    public override Expression ToExpression(ParameterExpression paramExpr) {
      var expr = Predicate.ToExpression(paramExpr);
      return Expression.Not(expr);
    }
  }

  }
