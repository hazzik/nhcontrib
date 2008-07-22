using System;
using NHibernate.Criterion;
using NUnit.Framework;
using NHibernate.Linq.Tests.Entities;
using System.Linq;
using System.Linq.Expressions;

namespace NHibernate.Linq.Tests
{
    [TestFixture]
    public class WhereSubqueryTests : BaseTest
    {
        protected override ISession CreateSession()
        {
            return GlobalSetup.CreateSession();
        }

        [Test]
        public void TimesheetsWithNoEntries()
        {
            var query = (from timesheet in session.Linq<Timesheet>()
                         where !timesheet.Entries.Any()
                         select timesheet).ToList();

            Assert.AreEqual(1, query.Count);
        }

        [Test]
        public void TimeSheetsWithCountSubquery()
        {
            var query = (from timesheet in session.Linq<Timesheet>()
                         where timesheet.Entries.Count() >= 1
                         select timesheet).ToList();
            Assert.AreEqual(2, query.Count);
        }

		[Test]
		public void TimeSheetsWithCountWithPredicateSubquery()
		{
			var query = (from timesheet in session.Linq<Timesheet>()
						 where timesheet.Entries.Count(e=>e.NumberOfHours>8) >= 1
						 select timesheet).ToList();
			Assert.AreEqual(2, query.Count);
		}

        [Test]
        public void TimeSheetsWithCountSubqueryReversed()
        {
            var query = (from timesheet in session.Linq<Timesheet>()
                         where 1 <= timesheet.Entries.Count()
                         select timesheet).ToList();

            Assert.AreEqual(2, query.Count);
        }

        [Test]
        public void TimeSheetsWithCountSubqueryComparedToProperty()
        {
            var query = (from timesheet in session.Linq<Timesheet>()
                         where timesheet.Entries.Count() > timesheet.Id
                         select timesheet).ToList();

            Assert.AreEqual(1, query.Count);
        }

        [Test]
        public void TimeSheetsWithCountSubqueryComparedToPropertyReversed()
        {
            var query = (from timesheet in session.Linq<Timesheet>()
                         where timesheet.Id < timesheet.Entries.Count()
                         select timesheet).ToList();

            Assert.AreEqual(1, query.Count);
        }

        [Test]
        public void TimeSheetsWithAverageSubquery()
        {
            var query = (from timesheet in session.Linq<Timesheet>()
                         where timesheet.Entries.Average(e => e.NumberOfHours) > 12
                         select timesheet).ToList();

            Assert.AreEqual(1, query.Count);
        }
		[Test]
		public void TimeSheetsWithAverageWithLambdaSubquery()
		{
			var query = (from timesheet in session.Linq<Timesheet>()
						 where timesheet.Entries.Average(e => e.NumberOfHours+e.Id) > 12
						 select timesheet).ToList();

			Assert.AreEqual(1, query.Count);
		}
        [Test]
        public void TimeSheetsWithAverageSubqueryReversed()
        {
            var query = (from timesheet in session.Linq<Timesheet>()
                         where 12 < timesheet.Entries.Average(e => e.NumberOfHours)
                         select timesheet).ToList();

            Assert.AreEqual(1, query.Count);
        }

        [Test]
        //[Ignore("Need to coalesce the subquery - timesheet with no entries should return average of 0, not null")]
        public void TimeSheetsWithAverageSubqueryComparedToProperty()
        {
            var query = (from timesheet in session.Linq<Timesheet>()
                         where timesheet.Entries.Average(e => e.NumberOfHours) < timesheet.Id
                         select timesheet).ToList();

            Assert.AreEqual(0, query.Count);
        }

        [Test]
		public void TimeSheetsWithAverageSubqueryComparedToPropertyReversed()
        {
            var query = (from timesheet in session.Linq<Timesheet>()
                         where timesheet.Id > timesheet.Entries.Average(e => e.NumberOfHours)
                         select timesheet).ToList();

            Assert.AreEqual(0, query.Count);
        }

		[Test]
		public void TimeSheetsWithMaxWithProjectionSubquery()
		{
			var query = (from timesheet in session.Linq<Timesheet>()
						 where timesheet.Entries.Max(e => e.NumberOfHours*e.Id) >100
						 select timesheet).ToList();

			Assert.AreEqual(1, query.Count);
		}
		[Test]
		public void TimeSheetsWithMinWithProjectionSubquery()
		{
			var query = (from timesheet in session.Linq<Timesheet>()
						 where timesheet.Entries.Min(e => e.NumberOfHours * e.Id)==12
						 select timesheet).ToList();

			Assert.AreEqual(1, query.Count);
		}

        [Test]
        public void TimeSheetsWithMaxSubquery()
        {
            var query = (from timesheet in session.Linq<Timesheet>()
                         where timesheet.Entries.Max(e => e.NumberOfHours) == 14
                         select timesheet).ToList();

            Assert.AreEqual(1, query.Count);
        }

        [Test]
        public void TimeSheetsWithMaxSubqueryReversed()
        {
            var query = (from timesheet in session.Linq<Timesheet>()
                         where 14 == timesheet.Entries.Max(e => e.NumberOfHours)
                         select timesheet).ToList();

            Assert.AreEqual(1, query.Count);
        }

        [Test]
        public void TimeSheetsWithMaxSubqueryComparedToProperty()
        {
            var query = (from timesheet in session.Linq<Timesheet>()
                         where timesheet.Entries.Max(e => e.NumberOfHours) > timesheet.Id
                         select timesheet).ToList();

            Assert.AreEqual(2, query.Count);
        }

        [Test]
        public void TimeSheetsWithMaxSubqueryComparedToPropertyReversed()
        {
            var query = (from timesheet in session.Linq<Timesheet>()
                         where timesheet.Id < timesheet.Entries.Max(e => e.NumberOfHours)
                         select timesheet).ToList();

            Assert.AreEqual(2, query.Count);
        }


		//TODO: Check here once
        [Test]
        public void TimeSheetsWithMinSubquery()
        {
            var query = (from timesheet in session.Linq<Timesheet>()
                         where timesheet.Entries.Min(e => e.NumberOfHours) < 7
                         select timesheet).ToList();

            Assert.AreEqual(2, query.Count);
        }


		//TODO: Check here once
        [Test]
        public void TimeSheetsWithMinSubqueryReversed()
        {
            var query = (from timesheet in session.Linq<Timesheet>()
                         where 7 > timesheet.Entries.Min(e => e.NumberOfHours)
                         select timesheet).ToList();

            Assert.AreEqual(2, query.Count);
        }

        [Test]
        public void TimeSheetsWithMinSubqueryComparedToProperty()
        {
            var query = (from timesheet in session.Linq<Timesheet>()
                         where timesheet.Entries.Min(e => e.NumberOfHours) > timesheet.Id
                         select timesheet).ToList();

            Assert.AreEqual(2, query.Count);
        }

        [Test]
        public void TimeSheetsWithMinSubqueryComparedToPropertyReversed()
        {
            var query = (from timesheet in session.Linq<Timesheet>()
                         where timesheet.Id < timesheet.Entries.Min(e => e.NumberOfHours)
                         select timesheet).ToList();

            Assert.AreEqual(2, query.Count);
        }

        [Test]
        public void TimeSheetsWithSumSubquery()
        {
            var query = (from timesheet in session.Linq<Timesheet>()
                         where timesheet.Entries.Sum(e => e.NumberOfHours) <= 20
                         select timesheet).ToList();

            Assert.AreEqual(1, query.Count);
        }

        [Test]
        public void TimeSheetsWithSumSubqueryReversed()
        {
            var query = (from timesheet in session.Linq<Timesheet>()
                         where 20 >= timesheet.Entries.Sum(e => e.NumberOfHours)
                         select timesheet).ToList();

            Assert.AreEqual(1, query.Count);
        }

        [Test]
		public void TimeSheetsWithSumSubqueryComparedToProperty()
        {
            var query = (from timesheet in session.Linq<Timesheet>()
                         where timesheet.Entries.Sum(e => e.NumberOfHours) >= timesheet.Id * 13
                         select timesheet).ToList();

            Assert.AreEqual(1, query.Count);
        }

        [Test]
		public void TimeSheetsWithSumSubqueryComparedToPropertyReversed()
        {
            var query = (from timesheet in session.Linq<Timesheet>()
                         where timesheet.Id *15>= timesheet.Entries.Sum(e => e.NumberOfHours)
                         select timesheet).ToList();

            Assert.AreEqual(1, query.Count);
        }
    }
}
