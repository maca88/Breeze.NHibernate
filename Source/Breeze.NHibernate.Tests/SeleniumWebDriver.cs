using System;
using OpenQA.Selenium.Chrome;

namespace Breeze.NHibernate.Tests
{
    public class SeleniumWebDriver : IDisposable
    {
        private readonly Lazy<ChromeDriver> _lazyDriver;

        public SeleniumWebDriver()
        {
            _lazyDriver = new Lazy<ChromeDriver>(() => new ChromeDriver(@"C:\Tools\WebDriver"));
        }

        public ChromeDriver ChromeDriver => _lazyDriver.Value;

        public void Dispose()
        {
            if (_lazyDriver.IsValueCreated)
            {
                ChromeDriver.Dispose();
            }
        }
    }
}
