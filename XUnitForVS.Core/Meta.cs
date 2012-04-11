using System.Reflection;
using Xunit;
using Xunit.Runner.VisualStudio.VS2010;

namespace xunit.runner.visualstudio.vs2010.core
{
    public static class Meta
    {
        /// <summary>
        /// Returns a set of assemblies that must be installed to allow the QTAgent
        /// to find and load 'XUnitTestAdapter' type
        /// </summary>
        public static Assembly[] RequiredAssemblies
        {
            get
            {
                return new[]
                {
                    typeof(XUnitTestAdapter).Assembly,
                    typeof(ExecutorWrapper).Assembly,
                };
            }
        }
    }
}
