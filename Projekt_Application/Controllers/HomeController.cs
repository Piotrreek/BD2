using System.Net.Mime;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Projekt;

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
    
    public IActionResult GetErrorPage([FromQuery]string error)
    {
        return View("GetErrorPage", error);
    }
}