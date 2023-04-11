using System.Collections;
using System.Data.Common;
using System.Data.SqlClient;
using System.Text;
using System.Xml;
using Dapper;
using Projekt.Helpers;
using Projekt.Models;
using XmlAttribute = System.Xml.XmlAttribute;
using XmlDocument = System.Xml.XmlDocument;
using XmlElement = System.Xml.XmlElement;

namespace Projekt;

/// <summary>
/// This is class to make some simple operations on XML data
/// Remember to use at the beginning CreateDatabase method
/// Without it, you won't be able to use other functions
/// </summary>
public class XmlService : IXmlService
{
    private readonly string _connectionString;

    /// <summary>
    /// Creates instance of XmlService
    /// </summary>
    /// <param name="connectionString">Connection string to Microsoft SQL Server database</param>
    public XmlService(string connectionString)
    {
        _connectionString = connectionString;
    }

    /// <summary>
    /// Creates database and tables if not exist
    /// <exception cref="DbException">An error occurred while executing the command</exception>
    /// </summary>
    public async Task<Result>CreateDatabase()
    {
        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = SqlStatements.CreateDatabaseQuery;
            command.ExecuteNonQuery();
            command.CommandText = SqlStatements.CreateTablesQuery;
            await command.ExecuteNonQueryAsync();
            await connection.CloseAsync();
        }
        catch (ArgumentException argumentException)
        {
            return Result.Failure(argumentException);
        }
        catch (Exception exception)
        {
            var errorBuilder = new StringBuilder();
            errorBuilder.AppendLine("Error occured while executing script. Details:");
            errorBuilder.AppendLine(exception.Message);
            
            return Result.Failure(errorBuilder.ToString());
        }

        return Result.Success();
    }

    /// <summary>
    /// Saves xml document into database
    /// </summary>
    /// <param name="xml">String value, which contains XML data</param>
    /// <param name="documentName">String value, which contains name of XML document</param>
    /// <returns>Guid value of saved document</returns>
    public async Task<Result<string>> SaveXmlDocument(string xml, string documentName)
    {
        if (string.IsNullOrEmpty(documentName))
            return Result<string>.Failure("Document name must not be empty!");

        try
        {
            // create xml document with passed xml string
            var document = new XmlDocument();
            document.LoadXml(xml);

            // if there is not valid declaration at the beginning, we return result object with failure
            if (document.FirstChild is not XmlDeclaration xmlDecl)
                return Result<string>.Failure("Check document declaration (Version, Encoding)");

            var version = xmlDecl.Version;
            var encoding = xmlDecl.Encoding;

            // create connection and transaction to database
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            // create and return document id
            var documentId = await CreateDocument(connection, transaction, documentName, version, encoding);

            // save nodes to database recursively
            await SaveNodesRecursively(connection, transaction, document.DocumentElement, documentId, 1);

            await transaction.CommitAsync();
            await connection.CloseAsync();

            return Result<string>.Success(documentId.ToString());

        }
        catch (XmlException xmlException)
        {
            return Result<string>.Failure("XML string is invalid!");
        }
        catch (Exception exception)
        {
            return Result<string>.Failure(exception.Message);
        }
    }
    
    /// <summary>
    /// Reads XML document from database
    /// </summary>
    /// <param name="documentId">Id of document stored in the database</param>
    /// <returns>Result object with Message property containing XML or Error containing error message</returns>
    public async Task<Result<string>> ReadXmlDocument(Guid documentId)
    {
        var document = new XmlDocument();

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            var dbDocument = await GetDocumentById(connection, transaction, documentId);
            if (dbDocument == null)
                return Result<string>.Failure("Document with given Id does not exist!");

            var xmlDeclaration = document.CreateXmlDeclaration(dbDocument.Version, dbDocument.Encoding, null);
            document.AppendChild(xmlDeclaration);

            var xmlElements = (await GetElementsByDocumentId(connection, transaction, dbDocument.Id))
                .ToList();
            
            foreach (var xmlElement in xmlElements)
            {
                xmlElement.Attributes.AddRange(await GetAttributesByElementId(connection, transaction, xmlElement.Id));
            }
            
            await transaction.CommitAsync();
            await connection.CloseAsync();
            
            var root = xmlElements.First(e => e.ParentId == Guid.Empty);
            var rootElement = document.CreateElement(root.Value);
            foreach (var attribute in root.Attributes.OrderBy(a => a.Order))
                AppendAttribute(attribute.Name, attribute.Value, rootElement, document);
            
            document.AppendChild(rootElement);
            
            ConstructDocumentRecursively(root, xmlElements, document, rootElement);
            
            var writeSettings = new XmlWriterSettings()
            {
                Indent = true,
                IndentChars = "  ",
                Encoding = Encoding.GetEncoding(dbDocument.Encoding)
            };

            using var stringWriter = new Utf8StringWriter();
            using var xmlWrite = XmlWriter.Create(stringWriter, writeSettings);
            document.Save(xmlWrite);

            return Result<string>.Success(stringWriter.ToString());

        }
        catch (Exception exception)
        {
            return Result<string>.Failure(exception.Message);
        }
    }

    /// <summary>
    /// Get XML document model, which can be used for using editing functions, because this model holds id's of nodes/attributes/text
    /// </summary>
    /// <param name="documentId">Id of XML document</param>
    /// <returns>Result object with error if failed or XmlEditDocumentModel object, which contains whole XML document stored recursively</returns>
    public async Task<Result<XmlEditDocumentModel>> GetXmlDocumentModelForEditing(Guid documentId)
    {
        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            var dbDocument = await GetDocumentById(connection, transaction, documentId);
            if (dbDocument == null)
                return Result<XmlEditDocumentModel>.Failure("Document with given Id does not exist!");

            var xmlElements = (await GetElementsByDocumentId(connection, transaction, dbDocument.Id))
                .ToList();
            
            foreach (var xmlElement in xmlElements)
            {
                xmlElement.Attributes.AddRange(await GetAttributesByElementId(connection, transaction, xmlElement.Id));
            }
            
            await transaction.CommitAsync();
            await connection.CloseAsync();
            
            var root = xmlElements.First(e => e.ParentId == Guid.Empty);
            var xmlElementModel = new XmlElementModel(root.Id, root.Order, root.Value, root.Type);

            xmlElementModel.Attributes
                .AddRange(root.Attributes
                    .OrderBy(a => a.Order)
                    .Select(a =>
                        new XmlAttributeModel(a.Id, a.Name, a.Value, a.Order)));
            
            ConstructXmlElementModelRecursively(xmlElementModel, xmlElements);
            
            return Result<XmlEditDocumentModel>.Success(new XmlEditDocumentModel(documentId, xmlElementModel, dbDocument.Name));

        }
        catch (Exception exception)
        {
            return Result<XmlEditDocumentModel>.Failure(exception.Message);
        }
    }
    
    /// <summary>
    /// Find all nodes with given name in XML document
    /// </summary>
    /// <param name="documentId">Id of XML document</param>
    /// <param name="nodeName">Name of XML node</param>
    /// <returns>IEnumerable of XmlElementModel. Each holds node with given name and its children</returns>
    public async Task<Result<IEnumerable<XmlElementModel>>> FindElementsByNodeName(Guid documentId, string nodeName)
    {
        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            var dbDocument = await GetDocumentById(connection, transaction, documentId);
            if (dbDocument == null)
                return Result<IEnumerable<XmlElementModel>>.Failure("Document with given Id does not exist!");

            var xmlElements = (await GetElementsByDocumentId(connection, transaction, dbDocument.Id))
                .ToList();
            
            foreach (var xmlElement in xmlElements)
            {
                xmlElement.Attributes.AddRange(await GetAttributesByElementId(connection, transaction, xmlElement.Id));
            }

            var xmlElementModelsWithWantedNodeName = xmlElements
                .Where(e => e.Value == nodeName && e.Type == XmlElementTypeEnum.Node)
                .Select(e => new XmlElementModel(e.Id, e.Order, e.Value, e.Type))
                .ToList();

            foreach (var xmlElementModel in xmlElementModelsWithWantedNodeName)
            {
                xmlElementModel.Attributes
                    .AddRange(
                        xmlElements.First(e => e.Id == xmlElementModel.Id)
                            .Attributes
                            .OrderBy(a => a.Order)
                            .Select(a =>
                                new XmlAttributeModel(a.Id, a.Name, a.Value, a.Order)));
                
                ConstructXmlElementModelRecursively(xmlElementModel, xmlElements);
            }

            await transaction.CommitAsync();
            await connection.CloseAsync();
            
            return Result<IEnumerable<XmlElementModel>>.Success(xmlElementModelsWithWantedNodeName);
        }
        catch (Exception e)
        {
            return Result<IEnumerable<XmlElementModel>>.Failure(e);
        }
    }

    /// <summary>
    /// Find all nodes with given attribute (both attributeName and attributeValue create one attribute in XML node)
    /// </summary>
    /// <param name="documentId">Id of XML document</param>
    /// <param name="attributeName">Name of XML attribute</param>
    /// <param name="attributeValue">Value of XML attribute</param>
    /// <returns>IEnumerable of XmlElementModel. Each holds node with given attribute name, value and its children</returns>
    public async Task<Result<IEnumerable<XmlElementModel>>> FindElementsByAttributeNameAndValue(Guid documentId, string attributeName, string attributeValue)
    {
        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            var dbDocument = await GetDocumentById(connection, transaction, documentId);
            if (dbDocument == null)
                return Result<IEnumerable<XmlElementModel>>.Failure("Document with given Id does not exist!");

            var xmlElements = (await GetElementsByDocumentId(connection, transaction, dbDocument.Id))
                .ToList();
            
            foreach (var xmlElement in xmlElements)
            {
                xmlElement.Attributes.AddRange(await GetAttributesByElementId(connection, transaction, xmlElement.Id));
            }

            var xmlElementModelsWithWantedAttribute = xmlElements
                .Where(e => e.Attributes.Any(a => a.Name == attributeName && a.Value == attributeValue))
                .Select(e => new XmlElementModel(e.Id, e.Order, e.Value, e.Type))
                .ToList();

            foreach (var xmlElementModel in xmlElementModelsWithWantedAttribute)
            {
                xmlElementModel.Attributes
                    .AddRange(
                        xmlElements.First(e => e.Id == xmlElementModel.Id)
                            .Attributes
                            .OrderBy(a => a.Order)
                            .Select(a =>
                                new XmlAttributeModel(a.Id, a.Name, a.Value, a.Order)));
                
                ConstructXmlElementModelRecursively(xmlElementModel, xmlElements);
            }

            await transaction.CommitAsync();
            await connection.CloseAsync();

            return Result<IEnumerable<XmlElementModel>>.Success(xmlElementModelsWithWantedAttribute);
        }
        catch (Exception e)
        {
            return Result<IEnumerable<XmlElementModel>>.Failure(e);
        }
    }

    /// <summary>
    /// Deletes XML document with specified Id
    /// </summary>
    /// <param name="documentId">Id of XML document stored in database</param>
    /// <returns>Result object indicating whether operation was successful or not</returns>
    public async Task<Result> DeleteXmlDocument(Guid documentId)
    {
        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            var document = await GetDocumentById(connection, transaction, documentId);
            if (document == null)
                return Result.Failure("Document with this Id does not exist!");

            await connection.ExecuteAsync(
                SqlStatements.DeleteXmlDocumentQuery, 
                new { Id = documentId },
                transaction: transaction
                );
            
            await transaction.CommitAsync();
            await connection.CloseAsync();
        }
        catch (Exception e)
        {
            return Result.Failure(e);
        }

        return Result.Success();
    }

    /// <summary>
    /// Updates XML element with specified Id
    /// </summary>
    /// <param name="elementId">Id of XML element</param>
    /// <param name="value">New value of XML element</param>
    /// <returns>Result object indicating whether operation was successful</returns>
    public async Task<Result> UpdateXmlElement(Guid elementId, string value)
    {
        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            var numberOfModifiedRows = await connection.ExecuteAsync(
                SqlStatements.UpdateXmlElementQuery,
                new { Value = value, Id = elementId },
                transaction: transaction
            );

            if (numberOfModifiedRows == 0)
                return Result.Failure("Element with this Id does not exist!");

            await transaction.CommitAsync();
            await connection.CloseAsync();
        }
        catch (Exception e)
        {
            return Result.Failure(e);
        }

        return Result.Success();
    }

    /// <summary>
    /// Updates XML attribute with specified Id
    /// </summary>
    /// <param name="attributeId">Id of XML attribute</param>
    /// <param name="value">New value of XML attribute</param>
    /// <returns>Result object indicating whether operation was successful</returns>
    public async Task<Result> UpdateXmlAttribute(Guid attributeId, string value)
    {
        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            var numberOfModifiedRows = await connection.ExecuteAsync(
                SqlStatements.UpdateXmlAttributeQuery,
                new { Value = value, Id = attributeId },
                transaction: transaction
            );

            if (numberOfModifiedRows == 0)
                return Result.Failure("Attribute with this Id does not exist!");
            
            await transaction.CommitAsync();
            await connection.CloseAsync();
        }
        catch (Exception e)
        {
            return Result.Failure(e);
        }

        return Result.Success();
    }

    /// <summary>
    /// Get all documents
    /// </summary>
    /// <returns>IEnumerable with ids and names of documents</returns>
    public async Task<Result<IEnumerable<XmlDocumentModel>>> GetDocuments()
    {
        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            var documents = await connection.QueryAsync<XmlDocumentModel>(
                SqlStatements.GetDocumentsQuery,
                transaction: transaction
            );

            await transaction.CommitAsync();
            await connection.CloseAsync();

            return Result<IEnumerable<XmlDocumentModel>>.Success(documents);
        }
        catch (Exception e)
        {
            return Result<IEnumerable<XmlDocumentModel>>.Failure(e);
        }
    }
    
    private static void ConstructDocumentRecursively(Projekt.Models.XmlElement xmlElement, List<Projekt.Models.XmlElement> xmlElements, XmlDocument document, XmlElement element)
    {
        var children = xmlElements
            .Where(e => e.ParentId == xmlElement.Id)
            .OrderBy(e => e.Order)
            .ToList();
        
        if (!children.Any())
            return;

        foreach (var child in children)
        {
            if (child.Type == XmlElementTypeEnum.Text)
            {
                element.InnerText = child.Value;
            }
            else
            {
                var newElement = document.CreateElement(child.Value);
                foreach (var attribute in child.Attributes.OrderBy(a => a.Order))
                    AppendAttribute(attribute.Name, attribute.Value, newElement, document);
                
                element.AppendChild(newElement);
                ConstructDocumentRecursively(
                    child,
                    xmlElements.Where(a => a.ParentId != xmlElement.Id).ToList(),
                    document,
                    newElement
                );
            }
        }
    }

    private static void ConstructXmlElementModelRecursively(XmlElementModel xmlElement,
        List<Projekt.Models.XmlElement> xmlElements)
    {
        var children = xmlElements
            .Where(e => e.ParentId == xmlElement.Id)
            .OrderBy(e => e.Order)
            .ToList();

        if (!children.Any())
            return;

        foreach (var child in children)
        {
            if (child.Type == XmlElementTypeEnum.Text)
                xmlElement.Children.Add(
                    new XmlElementModel(child.Id, child.Order, child.Value, XmlElementTypeEnum.Text));
            else
            {
                var newXmlElementModel =
                    new XmlElementModel(child.Id, child.Order, child.Value, XmlElementTypeEnum.Node);
                
                xmlElement.Children.Add(newXmlElementModel);

                newXmlElementModel.Attributes
                    .AddRange(child.Attributes.OrderBy(a => a.Order)
                        .Select(a => new XmlAttributeModel(a.Id, a.Name, a.Value, a.Order)));

                ConstructXmlElementModelRecursively(newXmlElementModel, xmlElements);
            }
        }
    }

    private static void AppendAttribute(string attributeName, string attributeValue, XmlElement xmlElement, XmlDocument xmlDocument)
    {
        var xmlAttribute = xmlDocument.CreateAttribute(attributeName);
        xmlAttribute.Value = attributeValue;
        xmlElement.Attributes.Append(xmlAttribute);
    }
    
    private static async Task<Projekt.Models.XmlDocument?> GetDocumentById(SqlConnection sqlConnection, DbTransaction dbTransaction, Guid documentId)
    {
        return await sqlConnection.QueryFirstOrDefaultAsync<Projekt.Models.XmlDocument>(
            SqlStatements.GetXmlDocumentQuery,
            new { Id = documentId },
            transaction: dbTransaction
        );
    }

    private static async Task<IEnumerable<Projekt.Models.XmlElement>> GetElementsByDocumentId(SqlConnection sqlConnection, DbTransaction dbTransaction, Guid documentId)
    {
        return await sqlConnection.QueryAsync<Projekt.Models.XmlElement>(
            SqlStatements.GetXmlElementsQuery,
            new { XmlDocumentId = documentId },
            transaction: dbTransaction
        );
    }
    
    private static async Task<IEnumerable<Projekt.Models.XmlAttribute>> GetAttributesByElementId(SqlConnection sqlConnection, DbTransaction dbTransaction, Guid elementId)
    {
        return await sqlConnection.QueryAsync<Projekt.Models.XmlAttribute>(
            SqlStatements.GetXmlAttributesQuery,
            new { XmlElementId = elementId },
            transaction: dbTransaction
        );
    }

    private static async Task SaveNodesRecursively(SqlConnection sqlConnection, DbTransaction dbTransaction, XmlNode? node, Guid documentId, int order, Guid? parentId = null)
    {
        switch (node)
        {
            // if node is pure text, we save it to database and end recursion
            case XmlText:
                await sqlConnection.ExecuteScalarAsync<Guid>(
                    SqlStatements.CreateElementQuery,
                    new { Id = Guid.NewGuid(), XmlDocumentId = documentId, Order = order, ParentId = parentId, Type = XmlElementTypeEnum.Text, Value = node.Value },
                    transaction: dbTransaction);
                break;
            // if node is XmlElement, then we save it to database and recursively invoke function to save child nodes if there are any
            case XmlElement:
                var newOrder = 1;
                var id = await sqlConnection.ExecuteScalarAsync<Guid>(
                    SqlStatements.CreateElementQuery,
                    new { Id = Guid.NewGuid(), XmlDocumentId = documentId, Order = order, ParentId = parentId, Type = XmlElementTypeEnum.Node, Value = node.Name },
                    transaction: dbTransaction);
                
                // save attributes if there are any
                // collection of attributes is not null, because node is of type XmlElement
                var attributeOrder = 1;
                foreach (XmlAttribute attribute in node.Attributes!)
                {
                    await sqlConnection.ExecuteAsync(
                        SqlStatements.CreateAttributeQuery,
                        new { Id = Guid.NewGuid(), XmlElementId = id, Name = attribute.Name, Value = attribute.Value, Order = attributeOrder },
                        transaction: dbTransaction
                    );
                    attributeOrder++;
                }
                
                foreach (XmlNode childNode in node.ChildNodes)
                {
                    await SaveNodesRecursively(sqlConnection, dbTransaction, childNode, documentId, newOrder, id);
                    newOrder++;
                }
                break;
             default:
                return;
        }
    }

    private static async Task<Guid> CreateDocument(SqlConnection sqlConnection, DbTransaction dbTransaction, string documentName, string version, string encoding)
    {
        var documentId = await sqlConnection.ExecuteScalarAsync<Guid>(
            SqlStatements.CreateDocumentQuery,
            new { Id = Guid.NewGuid(), Name = documentName, Version = version, Encoding = encoding },
            transaction: dbTransaction
            );
        
        return documentId;
    }
}
