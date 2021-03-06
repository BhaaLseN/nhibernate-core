﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by AsyncGenerator.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------


using System.Collections.Generic;
using NHibernate.Dialect;
using NHibernate.Driver;
using NHibernate.Engine;
using NUnit.Framework;

namespace NHibernate.Test.NHSpecificTest.NH1756
{
	using System.Threading.Tasks;
	[TestFixture]
	public class FixtureAsync : BugTestCase
	{
		protected override bool AppliesTo(Dialect.Dialect dialect)
		{
			return dialect is MsSql2000Dialect;
		}

		protected override bool AppliesTo(ISessionFactoryImplementor factory)
		{
			// ODBC driver DateTime handling with SQL Server 2008+ Client is broken and forbids using it as a time stamp
			// generated on db-side.
			// Due to NH-3895, we have to force the scale on date-time parameters to 3 (3 significant fractional seconds digits)
			// when using ODBC + SQL Server 2008+, otherwise DateTime values having milliseconds will be rejected. But the SQL
			// Server DateTime does not have actually a one millisecond resolution (it has 3.333), causing ODBC to convert the
			// parameter to DateTime2. A DateTime value ending by 3ms (indeed 3.333) or 7ms (indeed 6.666) is
			// to be transmitted as having 3ms or 7ms and will match if transmitted as a DateTime. But when transmitted as
			// DateTime2, it will no more be considered equal, causing the test to be flaky and failing two thirds of tries.
			// Example failing update captured with profiler:
			// exec sp_executesql N'UPDATE book SET name_column = @P1 WHERE id = @P2 AND version_column = @P3',
			//     N'@P1 nvarchar(18),@P2 int,@P3 datetime2',N'modified test book',1,'2017-08-02 16:37:16.0630000'
			// Setting the scale to 2 still causes failure for two thirds of tries, due to 3ms/7ms being truncated in such case
			// with ODBC and SQL Server 2008+ Client, which is rejected bu ODBC.
			// (Affects DbVersionFixture too.)
			return !(factory.ConnectionProvider.Driver is OdbcDriver);
		}

		[Test]
		public async Task SaveTransient_Then_Update_OkAsync()
		{
			using (ISession session = OpenSession())
			{
				using (ITransaction transaction = session.BeginTransaction())
				{
					var book = new BookNotGenerated {Name = "test book", Pages = new List<Page>(),};
					await (session.SaveAsync(book));
					book.Name = "modified test book";
					await (transaction.CommitAsync());
				}
			}
			using (ISession session = OpenSession())
			{
				using (ITransaction transaction = session.BeginTransaction())
				{
					await (session.CreateQuery("delete from BookNotGenerated").ExecuteUpdateAsync());
					await (transaction.CommitAsync());
				}
			}
		}

		[Test]
		[Description("Work with AutoFlush on commit")]
		public async Task SaveTransient_Then_UpdateAsync()
		{
			using (ISession session = OpenSession())
			{
				using (ITransaction transaction = session.BeginTransaction())
				{
					var book = new Book { Name = "test book", Pages = new List<Page>(), };
					await (session.SaveAsync(book));
					book.Name = "modified test book";
					await (transaction.CommitAsync());
				}
			}
			using (ISession session = OpenSession())
			{
				using (ITransaction transaction = session.BeginTransaction())
				{
					await (session.CreateQuery("delete from Book").ExecuteUpdateAsync());
					await (transaction.CommitAsync());
				}
			}
		}

		[Test]
		public async Task SaveTransient_Then_Update_BugAsync()
		{
			using (ISession session = OpenSession())
			{
				using (ITransaction transaction = session.BeginTransaction())
				{
					var book = new Book {Name = "test book", Pages = new List<Page>(),};
					await (session.SaveAsync(book));
					book.Name = "modified test book";
					await (session.FlushAsync());
					await (transaction.CommitAsync());
				}
			}
			using (ISession session = OpenSession())
			{
				using (ITransaction transaction = session.BeginTransaction())
				{
					await (session.CreateQuery("delete from Book").ExecuteUpdateAsync());
					await (transaction.CommitAsync());
				}
			}
		}
	}
}