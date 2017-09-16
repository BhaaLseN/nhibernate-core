using System;
using System.Collections.Generic;
using System.Linq;
using NHibernate.Cfg.MappingSchema;
using NHibernate.Mapping.ByCode;
using NUnit.Framework;

namespace NHibernate.Test.NHSpecificTest.NH3114
{
	[TestFixture]
	public class ImplicitByCodeFixture : TestCaseMappingByCode
	{
		protected override HbmMapping GetMappings()
		{
			var mapper = new ModelMapper();
			mapper.Class<Entity>(rc =>
			{
				rc.Id(i => i.Id, m => m.Generator(Generators.GuidComb));
				rc.Component(p => p.FirstComponent,
					m =>
					{
						m.Set(c => c.ComponentCollection,
							// table name omitted, expecting a reasonable default
							c => { },
							c => c.Element());
						// not specifying a column name here (and below) causes an insert failure during session.Flush
						m.Property(p => p.ComponentProperty);
					});
				rc.Component(p => p.SecondComponent,
					m =>
					{
						m.Set(c => c.ComponentCollection,
							// table name omitted, expecting a reasonable default
							c => { },
							c => c.Element());
						// not specifying a column name here (and above) causes an insert failure during session.Flush
						m.Property(p => p.ComponentProperty);
					});
			});

			return mapper.CompileMappingForAllExplicitlyAddedEntities();
		}

		protected override void OnTearDown()
		{
			using (ISession session = OpenSession())
			using (ITransaction transaction = session.BeginTransaction())
			{
				session.Delete("from Entity");

				session.Flush();
				transaction.Commit();
			}
		}

		[Test, Ignore("This test currently has no useful asserts due to how the mapping is generated. A different approach is required to validate the result")]
		public void Component_WithSameType_ButDifferentTables_ShouldBeMappedAccordingly()
		{
			// the generated schema looks like this (missing one value column and a full collection table):
			// - Table "Entity" (Id, ComponentProperty)
			// - Table "ComponentCollection" (component_key, id)
			//
			// a sane schema (sh|w|c)ould probably be (even though the names are ridiculously long now and may need truncation on some DBMS):
			// - Table "Entity" (Id, FirstComponentComponentProperty, SecondComponentComponentProperty)
			// - Table "FirstComponentComponentCollection" (component_key, id)
			// - Table "SecondComponentComponentCollection" (component_key, id)

			// FIXME: it is not possible to write asserts based on Hbm* (or the generated schema XML); they do not contain column/table names
			//        an option could be calling cfg.GenerateSchemaCreationScript and looking at the strings, but that sounds a little too brittle...
			var mappings = GetMappings();
			var modelMapping = mappings.Items.OfType<HbmClass>().FirstOrDefault();
			Assert.IsNotNull(modelMapping);
			var lists = modelMapping.Items.OfType<HbmComponent>();
			Assert.AreEqual(2, lists.Count());
			var firstMapping = lists.FirstOrDefault(l => l.Name == nameof(Entity.FirstComponent));
			Assert.IsNotNull(firstMapping);
			var firstMember = firstMapping.Properties.OfType<HbmProperty>().FirstOrDefault(p => p.Name == nameof(Component.ComponentProperty));
			Assert.IsNotNull(firstMember);
			//Assert.AreEqual("FirstComponentComponentProperty", firstMember.column);
			var firstCollection = firstMapping.Items.OfType<HbmSet>().FirstOrDefault();
			Assert.IsNotNull(firstCollection);
			//Assert.AreEqual("FirstComponentComponentCollection", firstCollection.Table);
			var secondMapping = lists.FirstOrDefault(l => l.Name == nameof(Entity.SecondComponent));
			Assert.IsNotNull(secondMapping);
			var secondMember = secondMapping.Properties.OfType<HbmProperty>().FirstOrDefault(p => p.Name == nameof(Component.ComponentProperty));
			Assert.IsNotNull(secondMember);
			//Assert.AreEqual("SecondComponentComponentProperty", secondMember.column);
			var secondCollection = secondMapping.Items.OfType<HbmSet>().FirstOrDefault();
			Assert.IsNotNull(secondCollection);
			//Assert.AreEqual("SecondComponentComponentCollection", secondCollection.Table);
		}

		[Test]
		public void Component_WithSameType_ButDifferentTables_IsStoredInTheCorrectTableAndCollection()
		{
			Guid previouslySavedId;
			using (var session = OpenSession())
			{
				var entity = new Entity();
				entity.FirstComponent = new Component();
				entity.FirstComponent.ComponentProperty = "First";
				entity.FirstComponent.ComponentCollection = new List<string> { "FirstOne", "FirstTwo", "FirstThree" };
				entity.SecondComponent = new Component();
				entity.SecondComponent.ComponentProperty = "Second";
				entity.SecondComponent.ComponentCollection = new List<string> { "SecondOne", "SecondTwo", "SecondThree" };
				session.SaveOrUpdate(entity);
				// flushing fails due to ComponentProperty of both components being mapped to the same column,
				// as the AbstractEntityPersister expects 3 parameters but only gets 2.
				session.Flush();
				previouslySavedId = entity.Id;
			}

			using (var session = OpenSession())
			{
				var entity = session.Get<Entity>(previouslySavedId);
				Assert.IsNotNull(entity);
				Assert.IsNotNull(entity.FirstComponent);
				Assert.AreEqual("First", entity.FirstComponent.ComponentProperty);
				CollectionAssert.AreEquivalent(new[] { "FirstOne", "FirstTwo", "FirstThree" }, entity.FirstComponent.ComponentCollection);
				Assert.IsNotNull(entity.SecondComponent);
				Assert.AreEqual("Second", entity.SecondComponent.ComponentProperty);
				CollectionAssert.AreEquivalent(new[] { "SecondOne", "SecondTwo", "SecondThree" }, entity.SecondComponent.ComponentCollection);
			}
		}
	}
}
