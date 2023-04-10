namespace Projekt.Models;

internal sealed class XmlElement
{
    public Guid Id { get; }
    public int Order { get; }
    public Guid XmlDocumentId { get; }
    public Guid ParentId { get; }
    public XmlElementTypeEnum Type { get; }
    public string Value { get; }
    public List<XmlAttribute> Attributes { get; internal set; } = new ();

    public XmlElement(Guid id, int order, Guid xmlDocumentId, Guid parentId, byte type, string value)
    {
        Id = id;
        XmlDocumentId = xmlDocumentId;
        ParentId = parentId;
        Order = order;
        Type = (XmlElementTypeEnum)type;
        Value = value;
    }
}
