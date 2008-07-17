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
						where db.Methods.left(e.FirstName, 2) == "An"
						select db.Methods.left(e.FirstName, 3);
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
						where db.Methods.Substring(e.FirstName, 1, 2) == "An"
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
			                   		AfterExtension = db.Methods.Replace(e.FirstName, "An", "Zan")
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
			            where db.Methods.CharIndex(e.FirstName, 'A') == 1
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
			            select e.FirstName.IndexOf('A', 3);

			ObjectDumper.Write(query);
		}

		[Test]
		public void TwoFunctionExpression()
		{
			var query = from e in db.Employees
			            where db.Methods.left(e.FirstName, 1) == db.Methods.left(e.LastName, 1)
			            select new {Name = e.FirstName, LastName = e.LastName};

			var results = query.ToList();
			results.Each(x=>Assert.AreEqual(x.Name.Substring(0,1),x.LastName.Substring(0,1)));
		}
	}
}
