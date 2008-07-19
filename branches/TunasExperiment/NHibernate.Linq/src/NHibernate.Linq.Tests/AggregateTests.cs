using System;
using System.Text;
using NUnit.Framework;
using NHibernate.Linq.SqlClient;
using System.Linq;

namespace NHibernate.Linq.Tests
{
	[TestFixture]
	public class AggregateTests : BaseTest
	{
		[Test]
		public void AggregateWithStartsWith()
		{
			var query = (from c in db.Customers where c.CustomerID.StartsWith("A") select c.CustomerID)
				.Aggregate<String, StringBuilder>(new StringBuilder(),
				                                  ( sb, id ) => sb.Append(id).Append(","));

			Console.WriteLine(query);
			Assert.AreEqual("ALFKI,ANATR,ANTON,AROUT,", query.ToString());
		}

		[Test]
		public void AggregateWithEndsWith()
		{
			var query = (from c in db.Customers where c.CustomerID.EndsWith("TH") select c.CustomerID)
				.Aggregate<String, StringBuilder>(new StringBuilder(),
				                                  ( sb, id ) => sb.Append(id).Append(","));

			Console.WriteLine(query);
			Assert.AreEqual("WARTH,", query.ToString());
		}

		[Test]
		public void AggregateWithContains()
		{
			var query = (from c in db.Customers where c.CustomerID.Contains("CH") select c.CustomerID)
				.Aggregate<String, StringBuilder>(new StringBuilder(),
				                                  ( sb, id ) => sb.Append(id).Append(","));

			Console.WriteLine(query);
			Assert.AreEqual("CHOPS,RANCH,", query.ToString());
		}

		[Test]
		public void AggregateWithEquals()
		{
			var query = (from c in db.Customers
			             where c.CustomerID.Equals("ALFKI") || c.CustomerID.Equals("ANATR") || c.CustomerID.Equals("ANTON")
			             select c.CustomerID)
				.Aggregate(( prev, next ) => (prev + "," + next));

			Console.WriteLine(query);
			Assert.AreEqual("ALFKI,ANATR,ANTON", query);
		}

		[Test]
		public void AggregateWithNotStartsWith()
		{
			var query = (from c in db.Customers
			             where c.CustomerID.StartsWith("A") && !c.CustomerID.StartsWith("AN")
			             select c.CustomerID)
				.Aggregate<String, StringBuilder>(new StringBuilder(),
				                                  ( sb, id ) => sb.Append(id).Append(","));

			Console.WriteLine(query);
			Assert.AreEqual("ALFKI,AROUT,", query.ToString());
		}

		[Test]
		public void AggregateWithMonthFunction()
		{
			var date = new DateTime(2007, 1, 1);

			var query = (from e in db.Employees
			             where e.BirthDate.Month() == date.Month
			             select e.FirstName)
				.Aggregate(new StringBuilder(), ( sb, name ) => sb.Length > 0 ? sb.Append(", ").Append(name) : sb.Append(name));

			Console.WriteLine("{0} Birthdays:", date.ToString("MMMM"));
			Console.WriteLine(query);
		}

		[Test]
		public void AggregateWithBeforeYearFunction()
		{
			var date = new DateTime(1960, 1, 1);

			var query = (from e in db.Employees
			             where e.BirthDate.Year() < date.Year
			             select e.FirstName.Upper())
				.Aggregate(new StringBuilder(), ( sb, name ) => sb.Length > 0 ? sb.Append(", ").Append(name) : sb.Append(name));

			Console.WriteLine("Birthdays before {0}:", date.ToString("yyyy"));
			Console.WriteLine(query);
		}

		[Test]
		public void AggregateWithOnOrAfterYearFunction()
		{
			var date = new DateTime(1960, 1, 1);

			var query = (from e in db.Employees
			             where e.BirthDate.Year() >= date.Year && e.FirstName.Len() > 4
			             select e.FirstName)
				.Aggregate(new StringBuilder(), ( sb, name ) => sb.Length > 0 ? sb.Append(", ").Append(name) : sb.Append(name));

			Console.WriteLine("Birthdays after {0}:", date.ToString("yyyy"));
			Console.WriteLine(query);
		}

		[Test]
		public void AggregateWithUpperAndLowerFunctions()
		{
			var date = new DateTime(2007, 1, 1);

			var query = (from e in db.Employees
			             where e.BirthDate.Month() == date.Month
			             select new {First = e.FirstName.ToUpper(), Last = e.LastName.Lower()})
				.Aggregate(new StringBuilder(), ( sb, name ) => sb.Length > 0 ? sb.Append(", ").Append(name) : sb.Append(name));

			Console.WriteLine("{0} Birthdays:", date.ToString("MMMM"));
			Console.WriteLine(query);
		}

		[Test]
		public void AggregateWithCustomFunction()
		{
			var date = new DateTime(1960, 1, 1);

			var query = (from e in db.Employees
			             where e.BirthDate.Year() < date.Year
			             select e.FirstName.fnEncrypt())
				.Aggregate(new StringBuilder(), ( sb, name ) => sb.AppendLine(BitConverter.ToString(name)));

			Console.WriteLine(query);
		}
	}
}