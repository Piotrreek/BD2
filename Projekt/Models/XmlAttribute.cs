namespace Projekt.Models;

internal sealed class XmlAttribute
{
    public Guid Id { get; }
    public Guid XmlElementId { get; }
    public string Name { get; }
    public string Value { get; }
    public int Order { get; }
    
    public XmlAttribute(Guid id, Guid xmlElementId, string name, string value, int order)
    {
        Id = id;
        XmlElementId = xmlElementId;
        Name = name;
        Value = value;
        Order = order;
    }
}
