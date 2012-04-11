using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Xunit.Runner.VisualStudio.VS2010
{
    public static class IAHelpers
    {
        public static NewExpression GetNewExpression(Type resultType, params Expression[] parameters)
        {
            Type[] types = Array.ConvertAll(parameters, p => p.Type);
            var ctor = resultType.GetConstructor(types);
            if (ctor == null) throw new MissingMethodException("Unable to find constuctor: " + resultType + "(" + string.Join(", ", types.Select(t => t.FullName)) + ")");
            return Expression.New(ctor, parameters);
        }
        public static MethodCallExpression GetSCallExpression(MethodInfo method, params Expression[] parameters)
        {
            return Expression.Call(method, parameters);
        }
        public static MethodCallExpression GetCallExpression(Expression instance, MethodInfo method, params Expression[] parameters)
        {
            return Expression.Call(instance, method, parameters);
        }

        public static void InitNewLambda<TDelegate>(out TDelegate dmethod, Type resultType, params Type[] types)
        {
            var parameters = Array.ConvertAll(types, t => Expression.Parameter(t));
            var ctor = resultType.GetConstructor(types);
            CreateMethod(Expression.New(ctor, parameters), out dmethod, parameters);
        }
        public static void InitSCallLambda<TDelegate>(out TDelegate dmethod, MethodInfo method, params Type[] types)
        {
            var parameters = Array.ConvertAll(types, t => Expression.Parameter(t));
            CreateMethod(Expression.Call(method, parameters), out dmethod, parameters);
        }
        public static void InitCallLambda<TDelegate>(out TDelegate dmethod, MethodInfo method, params Type[] types)
        {
            var subpars = Array.ConvertAll(types, t => Expression.Parameter(t));
            ParameterExpression[] arr = new ParameterExpression[subpars.Length + 1];
            arr[0] = Expression.Parameter(typeof(object));
            Array.Copy(subpars, 0, arr, 1, subpars.Length);
            CreateMethod(Expression.Call(Expression.Convert(arr[0], method.ReflectedType), method, subpars), out dmethod, arr);
        }

        public static void CreateMethod<TDelegate>(Expression body, out TDelegate method, params ParameterExpression[] parameters)
        {
            Type rettype = typeof(TDelegate).GetMethod("Invoke").ReturnType;
            if (body.Type != rettype && rettype.IsAssignableFrom(body.Type)) body = Expression.Convert(body, rettype);

            var lambda = Expression.Lambda<TDelegate>(body, parameters);
            method = lambda.Compile();
        }
    }
}
