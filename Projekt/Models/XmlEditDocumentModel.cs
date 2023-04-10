namespace Projekt.Models;

public class XmlEditDocumentModel
{
    public Guid Id { get; }
    public string Name { get; }
    public XmlElementModel XmlModel { get; }
    
    internal XmlEditDocumentModel(Guid id, XmlElementModel xmlModel, string name)
    {
        Id = id;
        XmlModel = xmlModel;
        Name = name;
    }
}