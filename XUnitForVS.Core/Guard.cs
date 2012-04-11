using System;
using System.Diagnostics;

namespace Xunit.Runner.VisualStudio.VS2010
{
    /// <summary>
    /// Provides a wrapper for assertions and exception throwning for simplicity
    /// </summary>
    public class Guard
    {
        /// <summary>
        /// Checks if a supplied string variable is empty/null and throws appropriately
        /// </summary>
        /// <exception cref="ArgumentNullException">Throws if "parameter" is null or empty</exception>
        /// <param name="parameter">The parameter to check for Empty/Null-ness</param>
        /// <param name="parameterName">Name of the parameter being checked</param>
        public static void StringNotNullOrEmpty(string parameter, string parameterName)
        {
            Debug.Assert(!String.IsNullOrEmpty(parameter));
            Debug.Assert(!String.IsNullOrEmpty(parameterName));

            if (String.IsNullOrEmpty(parameter))
                throw new ArgumentException("The parameter cannot be null, and cannot be empty.", parameterName);
        }

        ///// <summary>
        ///// Checks if a supplied parameter is valid
        ///// </summary>
        ///// <exception cref="ArgumentNullException">Throws if "parameter" is null</exception>
        ///// <param name="parameter">The paramter to check for Null-ness</param>
        ///// <param name="parameterName">The name of the parameter that is being checked</param>
        //public static void ParameterNotNull(object parameter, string parameterName)
        //{
        //    Debug.Assert(parameter != null);

        //    if (parameter == null)
        //        throw new ArgumentNullException(parameterName);
        //}

        ///// <summary>
        ///// Checks an array for nullness and Zero-Length
        ///// </summary>
        ///// <exception cref="ArgumentNullException">Throws if "parameter" is null</exception>
        ///// <param name="parameter">Array to check for Zero-Length & Null-ness</param>
        ///// <param name="parameterName">Name of the parameter being checked</param>
        //public static void ArrayNotNullOrEmpty(object[] parameter, string parameterName)
        //{
        //    Debug.Assert((parameter != null) && (parameter.Length != 0));
        //    Debug.Assert(!String.IsNullOrEmpty(parameterName));

        //    if ((parameter == null) || (parameter.Length == 0))
        //        throw new ArgumentException("The parameter cannot be null, and cannot be empty.", parameterName);
        //}
    }
}
