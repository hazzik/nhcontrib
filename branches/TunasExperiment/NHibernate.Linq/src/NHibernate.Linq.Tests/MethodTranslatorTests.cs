using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MbUnit.Framework;
using NHibernate.Linq.Visitors;
using Northwind.Entities;
namespace NHibernate.Linq.Tests
{
	[TestFixture]
	public class MethodTranslatorTests : BaseTest
	{

		#region String Methods
		[Test]
		public void CanTranslateEndsWithInWhereClause()
		{
			var q = from s in session.Linq<Customer>()
					where s.CustomerID.EndsWith("a")
					select s;
			var results = q.ToList();
			Assert.AreEqual(6,results.Count);
		}

		[Test]
		public void CanTranslateToUpperWithInSelectClause()
		{
			var q = from s in session.Linq<Customer>()
					select s.ContactName.ToUpper();
			var results = q.ToList();
			results.Each(x=>Assert.AreEqual(x.ToUpper(),x));		
		}

		[Test]
		public void CanTranslateStringStartsWithInWhereClause()
		{

			var q = from s in session.Linq<Customer>()
					where s.CustomerID.StartsWith("A")
					select s;
			var results = q.ToList();
			Assert.AreEqual(4, results.Count);
		}

		[Test]
		public void CanTranslateStringContainsInWhereClause()
		{

			var q = from s in session.Linq<Customer>()
					where s.CustomerID.Contains("A")
					select s;
			var results = q.ToList();
			Assert.AreEqual(39,results.Count);
		}

		[Test]
		public void CanTranslateStringStartsWithInSelectClause()
		{
			var q = from s in session.Linq<Customer>()
			        select s.CustomerID.StartsWith("A");
			var results=q.ToList();
			Assert.AreEqual(4, results.Count(x => x));
		}

		[Test]
		public void CanTranslateStringEndsWithInSelectClause()
		{
			var q = from s in session.Linq<Customer>()
					select s.CustomerID.EndsWith("A");
			var results = q.ToList();
			Assert.AreEqual(6, results.Count(x => x));
		}

		[Test]
		public void CanTranslateStringContainsSelectClause()
		{
			var q = from s in session.Linq<Customer>()
					select s.CustomerID.Contains("A");
			var results = q.ToList();
			Assert.AreEqual(39, results.Count(x=>x));
		}
		#endregion
		#region Enumerable
		[Test]
		public void CanTranslateCountCallInWhereClause()
		{

			var q = from s in this.nwnd.Customers
					where s.Orders.Count()>10
					select s;
			var results = q.ToList();
		}
		#endregion
	}
}
