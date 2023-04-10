namespace Projekt.Models;

public class XmlElementModel
{
    public Guid Id { get; }
    public int Order { get; }
    public string Value { get; }
    public XmlElementTypeEnum Type { get; }
    public List<XmlElementModel> Children { get; internal set; } = new();
    public List<XmlAttributeModel> Attributes { get; internal set; } = new();
    
    public XmlElementModel(Guid id, int order, string value, XmlElementTypeEnum type)
    {
        Id = id;
        Order = order;
        Value = value;
        Type = type;
    }
}