namespace Projekt.Models;

public class XmlAttributeModel
{
    public Guid Id { get; }
    public string Name { get; }
    public string Value { get; }
    public int Order { get; }
    
    public XmlAttributeModel(Guid id, string name, string value, int order)
    {
        Id = id;
        Name = name;
        Value = value;
        Order = order;
    }
}