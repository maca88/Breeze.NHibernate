using System;
using System.Collections.Generic;
using System.Text;

namespace Breeze.NHibernate.Tests.Models
{
    public interface IAggregate
    {
        object GetAggregateRoot();
    }
}
