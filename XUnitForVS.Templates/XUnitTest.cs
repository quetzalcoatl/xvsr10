using System;
using System.Collections.Generic;
$if$ ($targetframeworkversion$ >= 3.5)using System.Linq;
$endif$using System.Text;
using Xunit;
using Xunit.Extensions;

namespace $rootnamespace$
{
    public class $safeitemrootname$
    {
        [Fact]
        public void SomeMethod_GivenSomeArguments_ReturnsSomeResult()
        {
            double expected = 1.0;
            double someArgument = 0.0;
            $fileinputname$ target = new $fileinputname$();
            
            double actual = target.SomeMethod(someArgument);
            
            actual.Assert().Equal(expected);
        }
    }
}
