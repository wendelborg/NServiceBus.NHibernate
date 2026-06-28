namespace SomeOtherNamespace;

using NHibernate.Mapping.ByCode.Conformist;

public class OrderLineMap : ClassMapping<OrderLine>
{
    public OrderLineMap()
    {
        SchemaAction(NHibernate.Mapping.ByCode.SchemaAction.None);
        Schema("dbo");
        Id(x => x.Id);
        ManyToOne(x => x.Order, map =>
        {
            map.Column("OrderId");
            map.NotNullable(true);
        });
    }
}
