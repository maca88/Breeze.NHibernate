using System.Collections.Generic;
using System.Threading.Tasks;
using OpenQA.Selenium;
using Xunit;
using Xunit.Abstractions;

namespace Breeze.NHibernate.Tests
{
    public class NorthwindIBTests : IClassFixture<SeleniumWebDriver>
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly SeleniumWebDriver _webDriver;
        private readonly HashSet<string> _allowedFailedTests = new HashSet<string>
        {
            "attaching entities in ctor makes fk values update" // breeze.js issue
        };

        public NorthwindIBTests(ITestOutputHelper testOutputHelper, SeleniumWebDriver webDriver)
        {
            _testOutputHelper = testOutputHelper;
            _webDriver = webDriver;
        }

        [Theory]
        [InlineData("ajaxAdapter")]
        [InlineData("attach")]
        [InlineData("classRewrite")]
        [InlineData("complexTypes")]
        [InlineData("entity")]
        [InlineData("entityManager")]
        [InlineData("metadata")]
        [InlineData("misc")]
        [InlineData("misc - ES5 Property tests")]
        [InlineData("no tracking")]
        [InlineData("param")]
        [InlineData("predicates - safe construction")]
        [InlineData("query - any/all")]
        [InlineData("query - basic")]
        [InlineData("query - ctor")]
        [InlineData("query - datatype")]
        [InlineData("query - local")]
        [InlineData("query - named")]
        [InlineData("query - non EF")]
        [InlineData("query - raw odata")]
        [InlineData("query - select")]
        [InlineData("save")]
        [InlineData("save - transaction")]
        [InlineData("save interceptor")]
        [InlineData("server-side delete")]
        [InlineData("validate")]
        [InlineData("validate entity")]
        public async Task TestModule(string module)
        {
#if DEBUG
            // Skip this test when running tests inside VS
            return;
#endif

            var driver = _webDriver.ChromeDriver;
            driver.Navigate().GoToUrl($"http://localhost:5000/index.aspcore.nh.html?modelLibrary=backingStore&module={module}&canStart");
            await Task.Delay(200);
            var element = driver.FindElement(By.Id("qunit-testresult"));
            while (!element.Text.Contains("Tests completed"))
            {
                await Task.Delay(1000);
            }

            var totalFailedTests = 0;
            foreach (var failedTest in driver.FindElements(By.CssSelector("#qunit-tests > li.fail")))
            {
                var testName = failedTest.FindElement(By.ClassName("test-name")).Text;
                if (_allowedFailedTests.Contains(testName))
                {
                    continue;
                }

                totalFailedTests++;
                _testOutputHelper.WriteLine("Failed test for module {0}: {1}, ", module, testName);
                _testOutputHelper.WriteLine("Asserts:");
                var assertList = failedTest.FindElements(By.CssSelector(".qunit-assert-list > li"));
                foreach (var assert in assertList)
                {
                    if (assert.GetAttribute("class") == "pass")
                    {
                        _testOutputHelper.WriteLine("   Passed: {0}", assert.FindElement(By.ClassName("test-message")).Text);
                    }
                    else
                    {
                        _testOutputHelper.WriteLine("   Failed: {0}", assert.FindElement(By.ClassName("test-message")).Text);
                        _testOutputHelper.WriteLine("       Expected: {0}", assert.FindElement(By.CssSelector(".test-expected pre")).Text);
                        _testOutputHelper.WriteLine("       Actual: {0}", assert.FindElement(By.CssSelector(".test-actual pre")).Text);
                    }
                }
            }

            _testOutputHelper.WriteLine(element.Text);

            Assert.Equal(0, totalFailedTests);
        }
    }
}
