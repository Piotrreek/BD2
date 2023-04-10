namespace Projekt.Models;

internal sealed class XmlDocument
{
    public Guid Id { get; }
    public string Name { get; }
    public string Version { get; }
    public string Encoding { get; }
    
    public XmlDocument(Guid id, string name, string version, string encoding)
    {
        Id = id;
        Name = name;
        Encoding = encoding;
        Version = version;
    }
}
