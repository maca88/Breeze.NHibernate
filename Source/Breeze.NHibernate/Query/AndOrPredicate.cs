// Copied from Breeze.Core
#pragma warning disable CS1591
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Breeze.NHibernate;

namespace Breeze.Core {

  /**
   * @author IdeaBlade
   *
   */
  public class AndOrPredicate : BasePredicate {
    private List<BasePredicate> _predicates;

    public AndOrPredicate(Operator op, params BasePredicate[] predicates) : this(op, predicates.ToList()) {
    }

    public AndOrPredicate(Operator op, IEnumerable<BasePredicate> predicates) : base(op) {
      _predicates = predicates.ToList();
    }

    
    public IEnumerable<BasePredicate> Predicates {
      get { return _predicates.AsReadOnly(); }
    }

    #region Modified code - Added entityMetadataProvider parameter
    public override void Validate(Type entityType, IEntityMetadataProvider entityMetadataProvider) {
      _predicates.ForEach(p => p.Validate(entityType, entityMetadataProvider));
    }
    #endregion

    public override Expression ToExpression(ParameterExpression paramExpr) {
      var exprs = _predicates.Select(p => p.ToExpression(paramExpr));
      return BuildAndOrExpr(exprs, Operator);
      
    }

    private Expression BuildAndOrExpr(IEnumerable<Expression> exprs, Operator op) {
      if (op == Operator.And) {
        return exprs.Aggregate((result, expr) => Expression.AndAlso(result, expr));
      } else if (op == Operator.Or) {
        return exprs.Aggregate((result, expr) => Expression.OrElse(result, expr));
      } else {
        throw new Exception("Invalid AndOr operator " + op.Name);
      }
    }

  }
}
