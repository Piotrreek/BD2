using FluentAssertions;
using Projekt;
using Projekt.Models;

namespace Projekt_Tests;

public class XmlServiceTests
{
    private readonly IXmlService _xmlService;
    public XmlServiceTests()
    {
        _xmlService = new XmlService("Data Source=(localdb)\\mssqllocaldb;Integrated Security=True");
        //_xmlService.CreateDatabase();
    }
    
    [Fact]
    public async Task SaveDocumentWithInvalidNameReturnsResultWithErrorMessage()
    {
        // arrange
        var xml = 
            @"<?xml version=""1.0"" encoding=""UTF-8""?>
            <words id='5'></words>";
        
        // act
        var result = await _xmlService.SaveXmlDocument(xml, "");
        
        // assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Document name must not be empty!");
    }
    
    [Fact]
    public async Task SaveDocumentWithInvalidXmlDeclarationReturnsResultWithErrorMessage()
    {
        // arrange
        var xml = 
            @"
            <words id='5'></words>";
        
        // act
        var result = await _xmlService.SaveXmlDocument(xml, "test");
        
        // assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Check document declaration (Version, Encoding)");
    }
    
    [Fact]
    public async Task SaveDocumentWithInValidXmlReturnsResultWithErrorMessage()
    {
        // arrange
        var xml = "abcdefjkjdnf";
        
        // act
        var result = await _xmlService.SaveXmlDocument(xml, "test");
        
        // assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("XML string is invalid!");

    }
    
    [Fact]
    public async Task SaveDocumentWithValidXmlReturnsResultWithCorrectId()
    {
        // arrange
        var xml = 
            @"<?xml version=""1.0"" encoding=""UTF-8""?>
            <words id='5'></words>";
        
        // act
        var saveResult = await _xmlService.SaveXmlDocument(xml, "test");
        var getResult = await _xmlService.GetDocuments();

        // assert
        saveResult.IsSuccess.Should().BeTrue();
        getResult.IsSuccess.Should().BeTrue();
        saveResult.Content.Should().BeOneOf(getResult.Content!.Select(a => a.Id.ToString()));
    }

    [Fact]
    public async Task ReadXmlDocumentWithCorrectIdReturnsSameXmlAsPassed()
    {
        // arrange
        var xml = 
            @"<?xml version=""1.0"" encoding=""UTF-8""?><words id=""5"">123</words>";
        
        // act
        var saveResult = await _xmlService.SaveXmlDocument(xml, "test");
        var readResult = await _xmlService.ReadXmlDocument(Guid.Parse(saveResult.Content!));

        // assert
        readResult.IsSuccess.Should().BeTrue();
        readResult.Content.Replace("\n", "").Replace("\r", "").Should().BeEquivalentTo(xml);
    }
    
    [Fact]
    public async Task ReadXmlDocumentWithInvalidIdReturnsResultWithError()
    {
        // act
        var readResult = await _xmlService.ReadXmlDocument(Guid.Empty);

        // assert
        readResult.IsSuccess.Should().BeFalse();
        readResult.Error.Should().Be("Document with given Id does not exist!");
    }

    [Fact]
    public async Task DeleteXmlDocumentWithValidIdReturnSuccess()
    {
        // arrange
        var xml = 
            @"<?xml version=""1.0"" encoding=""UTF-8""?><words id=""5"">123</words>";
        
        // act
        var saveResult = await _xmlService.SaveXmlDocument(xml, "test");
        var delete = await _xmlService.DeleteXmlDocument(Guid.Parse(saveResult.Content!));

        // assert
        delete.IsSuccess.Should().BeTrue();
    }
    
    [Fact]
    public async Task DeleteXmlDocumentWithNotExistingIdReturnsFailure()
    {
        // act
        var delete = await _xmlService.DeleteXmlDocument(Guid.Empty);

        // assert
        delete.IsSuccess.Should().BeFalse();
        delete.Error.Should().Be("Document with this Id does not exist!");
    }

    [Fact]
    public async Task EditXmlAttributeWithCorrectIdReturnsSuccess()
    {
        // arrange
        var xml = 
            @"<?xml version=""1.0"" encoding=""UTF-8""?><words id=""5"">123</words>";
        
        // act
        var saveResult = await _xmlService.SaveXmlDocument(xml, "test");
        var xmlEditDocumentModel = (await _xmlService.GetXmlDocumentModelForEditing(Guid.Parse(saveResult.Content!))).Content;
        var xmlElements = GetFlattenedElements(xmlEditDocumentModel!);
        var attribute = xmlElements
            .First(e => e.Attributes.Any(a => a.Value == "5"))
            .Attributes
            .First(a => a.Value == "5");
        var editResult = await _xmlService.UpdateXmlAttribute(attribute.Id, "10");
        var readResult = await _xmlService.ReadXmlDocument(Guid.Parse(saveResult.Content!));

        // assert
        editResult.IsSuccess.Should().BeTrue();
        readResult.IsSuccess.Should().BeTrue();
        readResult.Content.Should().Contain(@"id=""10""");
    }
    
    [Fact]
    public async Task EditXmlElementWithCorrectIdReturnsSuccess()
    {
        // arrange
        var xml = 
            @"<?xml version=""1.0"" encoding=""UTF-8""?><words id=""5"">123</words>";
        
        // act
        var saveResult = await _xmlService.SaveXmlDocument(xml, "test");
        var xmlEditDocumentModel = (await _xmlService.GetXmlDocumentModelForEditing(Guid.Parse(saveResult.Content!))).Content;
        var xmlElements = GetFlattenedElements(xmlEditDocumentModel!);
        var element = xmlElements.First(e => e.Value == "123");
        var editResult = await _xmlService.UpdateXmlElement(element.Id, "10");
        var readResult = await _xmlService.ReadXmlDocument(Guid.Parse(saveResult.Content!));

        // assert
        editResult.IsSuccess.Should().BeTrue();
        readResult.IsSuccess.Should().BeTrue();
        readResult.Content.Should().Contain("10");
        readResult.Content.Should().NotContain("123");
    }
    
    [Fact]
    public async Task FindElementsByAttributeNameAndValueReturnCorrectResult()
    {
        // arrange
        var xml = 
            @"<?xml version=""1.0"" encoding=""UTF-8""?><words id=""5""><word d=""4"">dsd</word><word d=""4"">fsdfs</word></words>";
        
        // act
        var saveResult = await _xmlService.SaveXmlDocument(xml, "test");
        var findResult = await _xmlService.FindElementsByAttributeNameAndValue(Guid.Parse(saveResult.Content!), "d", "4");

        // assert
        findResult.IsSuccess.Should().BeTrue();
        findResult.Content!.ToList().Count.Should().Be(2);
    }
    
    [Fact]
    public async Task FindElementsByNodeNameReturnCorrectResult()
    {
        // arrange
        var xml = 
            @"<?xml version=""1.0"" encoding=""UTF-8""?><words id=""5""><word d=""4"">dsd</word><word d=""4"">fsdfs</word></words>";
        
        // act
        var saveResult = await _xmlService.SaveXmlDocument(xml, "test");
        var findResult = await _xmlService.FindElementsByNodeName(Guid.Parse(saveResult.Content!), "word");

        // assert
        findResult.IsSuccess.Should().BeTrue();
        findResult.Content!.ToList().Count.Should().Be(2);
    }

    private static IEnumerable<XmlElementModel> GetFlattenedElements(XmlEditDocumentModel editDocumentModel)
    {
        var root = editDocumentModel.XmlModel;
        var list = new List<XmlElementModel>();
        AddRecursively(list, root);
        
        return list;
    }

    private static void AddRecursively(List<XmlElementModel> list, XmlElementModel elementModel)
    {
        list.Add(elementModel);
        foreach (var child in elementModel.Children)
        {
            AddRecursively(list, child);
        }
    }
}