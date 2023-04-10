using Projekt.Models;

namespace Projekt;

public interface IXmlService
{
     Task<Result> CreateDatabase();
     Task<Result<string>> SaveXmlDocument(string xml, string documentName);
     Task<Result<string>> ReadXmlDocument(Guid documentId);
     Task<Result> DeleteXmlDocument(Guid documentId);
     Task<Result> UpdateXmlElement(Guid elementId, string value);
     Task<Result> UpdateXmlAttribute(Guid attributeId, string value);
     Task<Result<XmlEditDocumentModel>> GetXmlDocumentModelForEditing(Guid documentId);
     Task<Result<IEnumerable<XmlDocumentModel>>> GetDocuments();
}