using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

namespace InternalContainer.Tests.Activator
{
    public class Class1
    {
        public Class2 C2;
        public Class3 C3;
        public Class1(Class2 c2, Class3 c3)
        {
            C2 = c2;
            C3 = c3;
        }
    }
    public class Class2
    {
        public int X;
        public Class2(Class4 c4)
        {
            X++;
        }
    }
    public class Class3 {}
    public class Class4 { }

    public class RecursionTest
    {
        private readonly Container container;

        private readonly ITestOutputHelper Output;
        public RecursionTest(ITestOutputHelper output)
        {
            container = new Container(Lifestyle.Singleton, log: output.WriteLine, assemblies: Assembly.GetExecutingAssembly());
            Output = output;
        }

        public Expression GetInstanceExpression(TypeInfo type)
        {
            var ctor = type.DeclaredConstructors.First();
            var ps = ctor.GetParameters()
                .Select(p => p.HasDefaultValue ? Expression.Constant(p.DefaultValue) : GetInstanceExpression(p.ParameterType.GetTypeInfo()));
            return Expression.New(ctor, ps);
        }
        public Func<object> GetFactory(Type type)
        {
            var exp = GetInstanceExpression(type.GetTypeInfo());
            return Expression.Lambda<Func<object>>(exp).Compile();
        }

        public object GetInstance(TypeInfo type)
        {
            var ctor = type.DeclaredConstructors.First();
            var parameters = ctor.GetParameters();
            var ps = parameters.Select(p => GetInstance(p.ParameterType.GetTypeInfo()));
            return ctor.Invoke(ps.ToArray());
        }

        [Fact]
        public void Test()
        {
            var iterations = 1e6;
            var sw = new Stopwatch();

            sw.Start();
            for (var i = 0; i < iterations; i++)
            {
                var s = new Class1(new Class2(new Class4()), new Class3());
            }
            sw.Stop();
            Output.WriteLine(sw.Elapsed.ToString());

            var f = GetFactory(typeof(Class1));
            sw.Restart();
            for (var i = 0; i < iterations; i++)
            {
                var s = f.Invoke();
            }
            sw.Stop();
            Output.WriteLine(sw.Elapsed.ToString());

            sw.Restart();
            iterations /= 10;
            for (var i = 0; i < iterations; i++)
            {
                var s = GetInstance(typeof(Class1).GetTypeInfo());
            }
            sw.Stop();
            Output.WriteLine(sw.Elapsed.ToString());
        }

    }


}
