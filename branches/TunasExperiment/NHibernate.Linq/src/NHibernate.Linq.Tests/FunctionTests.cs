using NUnit.Framework;
using NHibernate.Linq.SqlClient;
using System.Linq;

namespace NHibernate.Linq.Tests
{
	[TestFixture]
//	[Ignore("Calling functions doesn't work currently")]
	public class FunctionTests : BaseTest
	{

		[Test]
		public void LeftFunction()
		{
			var query = from e in db.Employees
						where e.FirstName.Left(2) == "An"
						select e.FirstName.Left(3);
			var results = query.ToList();
			foreach (var r in results)
			{
				Assert.LessOrEqual(3,r.Length);
				Assert.AreEqual("An",r.Substring(0,2));
			}
		}

		[Test]
		public void SubstringFunction()
		{
			var query = from e in db.Employees
						where SqlClientExtensions.Substring(e.FirstName,1, 2) == "An"
						select e;
			var results = query.ToList();
			foreach (var r in results)
			{
				Assert.AreEqual("An", r.FirstName.Substring(0, 2));
			}
		}


		[Test]
		public void ReplaceFunction()
		{
			var query = from e in db.Employees
			            where e.FirstName.StartsWith("An")
			            select new
			                   	{
			                   		Before = e.FirstName,
			                   		AfterMethod = e.FirstName.Replace("An", "Zan"),
			                   		AfterExtension = e.FirstName.Replace("An", "Zan")
			                   	};
			var results = query.ToList();
			foreach (var r in results)
			{
				Assert.IsTrue(r.Before.StartsWith("An"));
				Assert.AreEqual(r.Before.Replace("An","Zan"),r.AfterMethod);
				Assert.AreEqual(r.Before.Replace("An", "Zan"), r.AfterMethod);
			}
		}

		[Test]
		public void CharIndexFunction()
		{
			var query = from e in db.Employees
			            where e.FirstName.CharIndex('A') == 1
			            select e.FirstName;

			ObjectDumper.Write(query);
		}

		[Test]
		public void IndexOfFunctionExpression()
		{
			var query = from e in db.Employees
			            where e.FirstName.IndexOf("An") == 1
			            select e.FirstName;

			ObjectDumper.Write(query);
		}

		[Test]
		public void IndexOfFunctionProjection()
		{
			var query = from e in db.Employees
			            where e.FirstName.Contains("a")
			            select e.FirstName.IndexOf('b');

			ObjectDumper.Write(query);
		}

		[Test]
		public void TwoFunctionExpression()
		{
			var query = from e in db.Employees
						where e.FirstName.Substring(1, 1) == e.LastName.Substring(1, 1)
			            select new {Name = e.FirstName, LastName = e.LastName};

			var results = query.ToList();
			Assert.Greater(results.Count, 0);
			results.Each(x=>Assert.AreEqual(x.Name.Substring(1,1),x.LastName.Substring(1,1)));
		}
	}
}
