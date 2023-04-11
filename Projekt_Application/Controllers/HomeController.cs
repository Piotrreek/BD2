using System.Net.Mime;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Projekt;
using Projekt_Application.Models;

namespace Projekt_Application.Controllers;

public class HomeController : Controller
{
    private readonly IXmlService _xmlService;

    public HomeController(IXmlService xmlService)
    {
        _xmlService = xmlService;
    }
    
    public IActionResult Index()
    {
        return View();
    }

    public async Task<IActionResult> GetDocuments()
    {
        var result = await _xmlService.GetDocuments();
        if (!result.IsSuccess)
        {
            return RedirectToAction("GetErrorPage", "Home", new { error = result.Error });
        }
        return View(result.Content);
    }

    public async Task<IActionResult> ViewDocument(Guid documentId, string documentName)
    {
        var result = await _xmlService.ReadXmlDocument(documentId);
        if(!result.IsSuccess)
            return RedirectToAction("GetErrorPage", "Home", new { error = result.Error });
        Response.Headers.Add("Content-Disposition", new ContentDisposition
        {
              Inline = true,
              FileName = documentName + ".xml"
        }.ToString());
        
        return new FileContentResult(Encoding.ASCII.GetBytes(result.Content!), "application/xml");
    }
    
    public async Task<IActionResult> DownloadDocument(Guid documentId, string documentName)
    {
        var result = await _xmlService.ReadXmlDocument(documentId);
        if(!result.IsSuccess)
            return RedirectToAction("GetErrorPage", "Home", new { error = result.Error });
        
        return File(Encoding.ASCII.GetBytes(result.Content!), "application/xml", documentName + ".xml");
    }

    public async Task<IActionResult> GetDocumentForEditing(Guid documentId)
    {
        var result = await _xmlService.GetXmlDocumentModelForEditing(documentId);
        if(!result.IsSuccess)
            return RedirectToAction("GetErrorPage", "Home", new { error = result.Error });
        
        return View("GetDocumentForEditing", result.Content);
    }

    public async Task<IActionResult> EditElement([FromQuery] string value, [FromQuery]Guid id)
    {
        var result = await _xmlService.UpdateXmlElement(id, value);
        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        return Ok("Updated successfully");
    }
    
    public async Task<IActionResult> EditAttribute([FromQuery] string value, [FromQuery]Guid id)
    {
        var result = await _xmlService.UpdateXmlAttribute(id, value);
        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        return Ok("Updated successfully");
    }

    public async Task<IActionResult> GetNodesByName([FromForm] Guid id, [FromForm]string nodeName)
    {
        var result = await _xmlService.FindElementsByNodeName(id, nodeName);
        
        if (!result.IsSuccess)
        {
            return View("GetErrorPage", result.Error);
        }

        return View("XmlNodes", result.Content);
    }
    
    public async Task<IActionResult> GetNodesByAttribute([FromForm] Guid id, [FromForm]string attributeName, [FromForm]string attributeValue)
    {
        var result = await _xmlService.FindElementsByAttributeNameAndValue(id, attributeName, attributeValue);
        
        if (!result.IsSuccess)
        {
            return View("GetErrorPage", result.Error);
        }

        return View("XmlNodes", result.Content);
    }

    public async Task<IActionResult> DeleteDocument(Guid id)
    {
        var result = await _xmlService.DeleteXmlDocument(id);
        
        if (!result.IsSuccess)
        {
            return View("GetErrorPage", result.Error);
        }

        return RedirectToAction("GetDocuments");
    }

    [HttpGet]
    public IActionResult AddDocument()
    {
        return View();
    }
    
    [HttpPost]
    public async Task<IActionResult> AddDocument(AddDocumentModel model)
    {
        var file = model.Document;
        if(file == null)
            return View("GetErrorPage", "Choose file!");

        var stream = file.OpenReadStream();
        using var reader = new StreamReader(stream);
        var fileText = new StringBuilder();
        while (reader.Peek() >= 0)
            fileText.AppendLine(await reader.ReadLineAsync());

        var result = await _xmlService.SaveXmlDocument(fileText.ToString(), file.FileName);
        
        return RedirectToAction("GetDocuments");
    }

    public IActionResult GetErrorPage([FromQuery]string error)
    {
        return View("GetErrorPage", error);
    }
}