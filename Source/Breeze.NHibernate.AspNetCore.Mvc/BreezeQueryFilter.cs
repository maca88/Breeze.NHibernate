using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Breeze.NHibernate.AspNetCore.Mvc {

  public class BreezeQueryFilter : IAsyncActionFilter {

    private readonly IEntityQueryExecutor _entityQueryExecutor;

    public BreezeQueryFilter(IEntityQueryExecutor entityQueryExecutor) {
      _entityQueryExecutor = entityQueryExecutor;
    }

    public virtual void OnActionExecuting(ActionExecutingContext context) {
      if (!context.ModelState.IsValid) {
        context.Result = new BadRequestObjectResult(context.ModelState);
      }
    }

    public virtual async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next) {
      OnActionExecuting(context);
      if (context.Result == null) {
        await OnActionExecutedAsync(await next());
      }
    }

    protected virtual Task OnActionExecutedAsync(ActionExecutedContext context) {
      var qs = QueryFns.ExtractAndDecodeQueryString(context);
      var queryable = QueryFns.ExtractQueryable(context);
      return _entityQueryExecutor.ShouldApplyAndExecute(queryable, qs) ? ApplyAndExecuteAsync() : Task.CompletedTask;

      async Task ApplyAndExecuteAsync() {
        context.Result = new ObjectResult(await _entityQueryExecutor.ApplyAndExecuteAsync(queryable, qs));
      }
    }
  }

}

