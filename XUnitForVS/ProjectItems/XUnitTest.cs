using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Extensions;

namespace $rootnamespace$
{
    public class $itemname$
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
