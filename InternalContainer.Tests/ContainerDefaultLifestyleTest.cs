using System.Linq;
using Xunit;

namespace InternalContainer.Tests
{
    public class ContainerDefaultLifestyleTest
    {
        public interface ISomeClass { }
        public class SomeClass : ISomeClass { }

        [Fact]
        public void Test_Singleton()
        {
            var c = new Container(Lifestyle.Singleton);
            var instance1 = c.GetInstance<SomeClass>();
            var map = c.Maps().Single();
            Assert.Equal(Lifestyle.Singleton, map.Lifestyle);
            Assert.Equal(1, map.Instances);
            var instance2 = c.GetInstance<SomeClass>();
            Assert.Equal(instance1, instance2);
            Assert.Equal(1, map.Instances);
        }
        [Fact]
        public void Test_Transient()
        {
            var c = new Container(Lifestyle.Transient);
            var instance1 = c.GetInstance<SomeClass>();
            var map = c.Maps().Single();
            Assert.Equal(Lifestyle.Transient, map.Lifestyle);
            Assert.Equal(1, map.Instances);
            var instance2 = c.GetInstance<SomeClass>();
            Assert.NotEqual(instance1, instance2);
            Assert.Equal(2, map.Instances);

        }
    }
}
