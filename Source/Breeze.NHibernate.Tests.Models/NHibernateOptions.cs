using System;
using System.Collections.Generic;
using System.Text;

namespace Breeze.NHibernate.Tests.Models
{
    public class NHibernateOptions
    {
        public string Dialect { get; set; }

        public string ConnectionDriver { get; set; }

        public string ConnectionString { get; set; }
    }
}
