using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Breeze.NHibernate.AspNetCore.Mvc {
  internal class MvcOptionsConfigurator : IConfigureOptions<MvcOptions> {

    private readonly IOptions<BreezeNHibernateOptions> _options;

    public MvcOptionsConfigurator(IOptions<BreezeNHibernateOptions> options) {
      _options = options;
    }

    public void Configure(MvcOptions options) {
      if (_options.Value.UseGlobalExceptionFilter) {
        options.Filters.Add(new GlobalExceptionFilter());
      }
    }
  }
}
