IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'BD2_XML')
       CREATE DATABASE BD2_XML;
GO
USE BD2_XML;
GO                                             
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
            REFERENCES XmlDocument(Id),
            ON DELETE CASCADE
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
);

