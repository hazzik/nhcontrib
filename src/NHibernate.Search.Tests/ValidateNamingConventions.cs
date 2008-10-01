using System;
using System.Reflection;
using NUnit.Framework;

namespace NHibernate.Search.Tests
{
    [TestFixture]
    public class ValidateNamingConventions
    {
        private System.Type[] types;

        [SetUp]
        public void Setup()
        {
            types = typeof(ISearchFactory).Assembly.GetExportedTypes();            
        }

        [Test]
        public void InterfacesStartsWithI()
        {
            foreach (System.Type type in types)
            {
                if(type.IsInterface==false)
                    continue;
                Assert.IsTrue(type.Name.StartsWith("I"),type.Name);
            }
        }

        [Test]
        public void FirstLaterOfMethodIsCapitalized()
        {
            foreach (System.Type type in types)
            {
                foreach (MethodInfo method in type.GetMethods())
                {
                    Assert.IsTrue(char.IsUpper(method.Name[0]), type.FullName + "." + method.Name);
                }
            } 
        }

        [Test]
        public void AttributesEndWithAttribute()
        {
            foreach (System.Type type in types)
            {
                if (type.IsAssignableFrom(typeof(Attribute)))
                    continue;
                Assert.IsTrue(type.Name.EndsWith("Attribute"), type.Name);
         
            }
        }
    }
}