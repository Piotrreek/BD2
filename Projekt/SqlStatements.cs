namespace Projekt;

internal static class SqlStatements
{
	public const string CreateDatabaseQuery = "IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'BD2_XML')CREATE DATABASE BD2_XML;";

	public const string CreateTablesQuery = @"
		USE BD2_XML;                                                                                   
		IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'XmlDocument')
			CREATE TABLE XmlDocument (
				Id UNIQUEIDENTIFIER PRIMARY KEY,
				Name VARCHAR(50) NOT NULL,
				Version VARCHAR(4) NOT NULL,
                Encoding VARCHAR(10) NOT NULL
			);

		IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'XmlElement')
			CREATE TABLE XmlElement (
				Id UNIQUEIDENTIFIER PRIMARY KEY,
				XmlDocumentId UNIQUEIDENTIFIER,
				[Order] INT,
				ParentId UNIQUEIDENTIFIER,
				[Type] TINYINT,
				[Value] VARCHAR(50),
				CONSTRAINT FK_XmlElement_XmlDocument
					FOREIGN KEY (XmlDocumentId)
					REFERENCES XmlDocument(Id)
					 ON DELETE CASCADE,
				CONSTRAINT FK_XmlElement_XmlElement
					FOREIGN KEY (ParentId)
					REFERENCES XmlElement(Id)
			);

		IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'XmlAttribute')
			CREATE TABLE XmlAttribute (
				Id UNIQUEIDENTIFIER PRIMARY KEY,
				XmlElementId UNIQUEIDENTIFIER,
				Name VARCHAR(50),
				Value VARCHAR(50),
				[Order] INT,
				CONSTRAINT FK_XmlAttribute_XmlElement
					FOREIGN KEY (XmlElementId)
					REFERENCES XmlElement(Id)
					ON DELETE CASCADE
		);";

	public const string CreateDocumentQuery = @"
		USE BD2_XML;
		INSERT INTO XmlDocument (Id, Name, Version, Encoding) VALUES (@Id, @Name, @Version, @Encoding);
		SELECT @Id;
		";

	public const string CreateElementQuery = @"
		USE BD2_XML;
		INSERT INTO XmlElement (Id, XmlDocumentId, [Order], ParentId, [Type], [Value]) 
		VALUES (@Id, @XmlDocumentId, @Order, @ParentId, @Type, @Value);
		SELECT @Id;
		";

	public const string CreateAttributeQuery = @"
		USE BD2_XML;
		INSERT INTO XmlAttribute (Id, XmlElementId, Name, [Value], [Order]) 
		VALUES (@Id, @XmlElementId, @Name, @Value, @Order);
		";

	public const string GetXmlDocumentQuery = @"
		USE BD2_XML;
		SELECT Id, Name, Version, Encoding FROM XmlDocument WHERE Id = @Id;
		";

	public const string GetXmlElementsQuery =
		@"
		USE BD2_XML;
		SELECT Id, [Order], XmlDocumentId, ParentId, [Type], [Value] FROM XmlElement WHERE XmlDocumentId = @XmlDocumentId;
		";

	public const string GetXmlAttributesQuery =
		@"
		USE BD2_XML;
		SELECT Id, XmlElementId, Name, [Value], [Order] FROM XmlAttribute WHERE XmlElementId = @XmlElementId;
		";

	public const string DeleteXmlDocumentQuery =
		@"
		USE BD2_XML;
		DELETE FROM XmlDocument WHERE Id = @Id;
		";

	public const string UpdateXmlElementQuery =
		@"
		USE BD2_XML;
		UPDATE XmlElement SET Value = @Value WHERE Id = @Id;
		";
	
	public const string UpdateXmlAttributeQuery =
		@"
		USE BD2_XML;
		UPDATE XmlAttribute SET Value = @Value WHERE Id = @Id;
		";

	public const string GetDocumentsQuery =
		@"
		USE BD2_XML;
		SELECT Id, Name FROM XmlDocument;
		";
}