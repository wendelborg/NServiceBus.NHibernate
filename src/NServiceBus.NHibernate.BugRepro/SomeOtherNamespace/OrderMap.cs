namespace SomeOtherNamespace;

using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

public class OrderMap : ClassMapping<Order>
{
    public OrderMap()
    {
        SchemaAction(NHibernate.Mapping.ByCode.SchemaAction.None);
        Schema("dbo");
        Id(x => x.Id);
        Bag(x => x.OrderLines, map =>
        {
            map.Key(k => k.Column(col => col.Name("OrderId")));
            map.Cascade(Cascade.All | Cascade.DeleteOrphans);
        },
        action => action.OneToMany());
    }
}
