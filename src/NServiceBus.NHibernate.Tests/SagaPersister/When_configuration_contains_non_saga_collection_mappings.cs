namespace NServiceBus.SagaPersisters.NHibernate.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoPersistence;
using global::NHibernate.Cfg;
using global::NHibernate.Dialect;
using global::NHibernate.Driver;
using global::NHibernate.Mapping.ByCode;
using NServiceBus.NHibernate.Tests;
using Sagas;
using NUnit.Framework;

/// <summary>
/// Reproduces issue #959: SagaModelMapper.AddMappings should not create indexes
/// for non-saga entity collections present in the NHibernate configuration.
/// When two non-saga entity sets map to the same tables, the old code would fail
/// with "Index IDX_XXXX already exists!" because it tried to add indexes for all
/// collections in the configuration, not just saga-related ones.
/// </summary>
[TestFixture]
public class When_configuration_contains_non_saga_collection_mappings
{
    [Test]
    public void Should_not_create_indexes_for_non_saga_collections()
    {
        var cfg = new Configuration()
            .DataBaseIntegration(x =>
            {
                x.Dialect<MsSql2012Dialect>();
                x.Driver<MicrosoftDataSqlClientDriver>();
                x.ConnectionString = Consts.SqlConnectionString;
            });

        // Add non-saga class mappings with collections that map to the same tables.
        // This simulates the scenario from issue #959 where two different namespaces
        // have Order/OrderLine entities mapping to the same database tables.
        var nonSagaMapper = new ModelMapper();

        nonSagaMapper.Class<FirstOrder>(ca =>
        {
            ca.Schema("dbo");
            ca.Table("Orders");
            ca.Id(x => x.Id, m => m.Generator(Generators.GuidComb));
            ca.Property(x => x.CustomerName);
            ca.Bag(x => x.OrderLines, b =>
            {
                b.Key(k => k.Column("Order_id"));
                b.Cascade(Cascade.All | Cascade.DeleteOrphans);
            }, r => r.OneToMany());
        });

        nonSagaMapper.Class<FirstOrderLine>(ca =>
        {
            ca.Schema("dbo");
            ca.Table("OrderLines");
            ca.Id(x => x.Id, m => m.Generator(Generators.GuidComb));
            ca.Property(x => x.ProductName);
            ca.ManyToOne(x => x.Order, m => m.Column("Order_id"));
        });

        nonSagaMapper.Class<SecondOrder>(ca =>
        {
            ca.Schema("dbo");
            ca.Table("Orders");
            ca.Id(x => x.Id, m => m.Generator(Generators.GuidComb));
            ca.Property(x => x.CustomerName);
            ca.Bag(x => x.OrderLines, b =>
            {
                b.Key(k => k.Column("Order_id"));
                b.Cascade(Cascade.All | Cascade.DeleteOrphans);
            }, r => r.OneToMany());
        });

        nonSagaMapper.Class<SecondOrderLine>(ca =>
        {
            ca.Schema("dbo");
            ca.Table("OrderLines");
            ca.Id(x => x.Id, m => m.Generator(Generators.GuidComb));
            ca.Property(x => x.ProductName);
            ca.ManyToOne(x => x.Order, m => m.Column("Order_id"));
        });

        cfg.AddMapping(nonSagaMapper.CompileMappingForAllExplicitlyAddedEntities());

        // Now add saga mappings - this should NOT try to index the non-saga collections.
        // ContainSagaData must be included, matching what NHibernateSagaStorage always does.
        var sagaTypes = new[] { typeof(Issue959Saga), typeof(Issue959SagaData) };
        var typesToScan = sagaTypes.Append(typeof(ContainSagaData));
        var metaModel = new SagaMetadataCollection();
        metaModel.AddRange(SagaMetadata.CreateMany(sagaTypes));

        // Before the fix, this would throw:
        // "Failed to add index! Are your sagas sharing types?"
        // caused by: NHibernate.MappingException: Index IDX_XXXX already exists!
        Assert.DoesNotThrow(() => SagaModelMapper.AddMappings(cfg, metaModel, typesToScan));
    }
}

#region Non-saga entity classes for testing issue #959

public class FirstOrder
{
    public virtual Guid Id { get; set; }
    public virtual string CustomerName { get; set; }
    public virtual IList<FirstOrderLine> OrderLines { get; set; }
}

public class FirstOrderLine
{
    public virtual Guid Id { get; set; }
    public virtual string ProductName { get; set; }
    public virtual FirstOrder Order { get; set; }
}

public class SecondOrder
{
    public virtual Guid Id { get; set; }
    public virtual string CustomerName { get; set; }
    public virtual IList<SecondOrderLine> OrderLines { get; set; }
}

public class SecondOrderLine
{
    public virtual Guid Id { get; set; }
    public virtual string ProductName { get; set; }
    public virtual SecondOrder Order { get; set; }
}

#endregion

#region Saga classes for testing issue #959

public class Issue959SagaData : ContainSagaData
{
    public virtual Guid CorrelationId { get; set; }
}

public class Issue959Saga : Saga<Issue959SagaData>, IAmStartedByMessages<SagaStartMessage>
{
    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<Issue959SagaData> mapper) =>
        mapper.MapSaga(s => s.CorrelationId).ToMessage<SagaStartMessage>(m => m.CorrelationId);

    public Task Handle(SagaStartMessage message, IMessageHandlerContext context) =>
        throw new NotImplementedException();
}

#endregion
