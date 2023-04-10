namespace Projekt.Models;

public class XmlDocumentModel
{
    public Guid Id { get; }
    public string Name { get; }
    
    public XmlDocumentModel(Guid id, string name)
    {
        Id = id;
        Name = name;
    }
}