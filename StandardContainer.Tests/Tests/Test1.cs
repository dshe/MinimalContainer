using System;
using System.Linq;
using System.Linq.Expressions;
using Xunit;
using Xunit.Abstractions;

namespace StandardContainer.Tests.Tests
{
    public class Test1
    {
        public interface ISomeClass { }

        public class SomeClass : ISomeClass
        {
            private static int instances;
            public SomeClass()
            {
                instances++;
            }
        }

        public class SomeClass2
        {
            public SomeClass S;
            public SomeClass2(Func<SomeClass> factory)
            {
                S = factory();
            }
        }

        private readonly Action<string> write;
        public Test1(ITestOutputHelper output)
        {
            write = output.WriteLine;
        }



        [Fact]
        public void T01_Getfactory()
        {
            var container = new Container(DefaultLifestyle.Transient);
            //container.RegisterTransient<SomeClass>();
            //container.RegisterFactory(() => new SomeClass());
            //container.RegisterTransient<Func<SomeClass>>();
            //container.RegisterTransient<SomeClass2>();
            var instance2 = container.GetInstance<SomeClass2>();
            ;
        }

        [Fact]
        public void T01_Failure()
        {
            Func<object> factory = () => new SomeClass();
            Assert.Throws<InvalidCastException>(() => (Func<SomeClass>) factory);

            Func<SomeClass> func2 = () => (SomeClass)factory();
            SomeClass sc = func2();
        }

        [Fact]
        public void T02_Concrete()
        {
            //You can not recreate an expression based on a method since an expression needs to know the original statements, not IL.
            Func<object> func = () => new SomeClass();
            Expression<Func<SomeClass>> expression = () => new SomeClass();

            var expression2 = expression;

            var xx = Expression.Lambda(expression2).Compile();
            var yy = xx.DynamicInvoke();
            var tt = yy.GetType();
        }
        [Fact]
        public void T03_Concrete()
        {
            var ctor = typeof(SomeClass).GetConstructors().Single();

            Expression ex = Expression.New(ctor);

            var xx = Expression.Lambda(ex).Compile();
            var yy = Expression.Lambda<Func<object>>(ex).Compile();

            var qq = (SomeClass) xx.DynamicInvoke();
            var ww = (SomeClass)yy();

            var tt = xx.GetType();
            //Expression.Lambda<>().

            //dynamic 
            ;
            //Func<SomeClass> newFunc = expression2.Compile();

            //SomeClass xxx = newFunc();

            //funce.ReturnType

            //Expression<Func<SomeClass>> funcee = Expression.Lambda<Func<SomeClass>>(func);


            //var expression = Expression.Lambda<Func<SomeClass>>(funce).Compile()();


        }



    }
}
