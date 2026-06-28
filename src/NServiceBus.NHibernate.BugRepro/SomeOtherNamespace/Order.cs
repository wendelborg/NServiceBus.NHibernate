namespace SomeOtherNamespace;

public class Order
{
    public virtual int Id { get; set; }
    public virtual IList<OrderLine> OrderLines { get; set; }
}
