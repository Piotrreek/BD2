using FluentAssertions;
using Projekt;

namespace Projekt_Tests;

public class XmlServiceTests
{
    private readonly IXmlService _xmlService;
    public XmlServiceTests()
    {
        _xmlService = new XmlService("Data Source=(localdb)\\mssqllocaldb;Integrated Security=True");
        _xmlService.CreateDatabase();
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
        readResult.Content.Should().Be(xml);
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
}