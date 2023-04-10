using Projekt;

var service = new XmlService("Data Source=(localdb)\\mssqllocaldb;Integrated Security=True");
await service.CreateDatabase();
var createResult = await service.SaveXmlDocument(
    @"<?xml version=""1.0"" encoding=""UTF-8""?>
    <words id='5'>
    <word fds='fdsfdf' id='1'>
        <l>aa</l>
        <l>bb</l>
    </word>
    <word key='aaa'>sky</word>
    <word f='fdsf'>bottom</word>
    <word>cup</word>
    <word>book</word>
    <word>rock</word>
    <word a='fdsf'>sand</word>
    <word>river</word>
    </words>
    ", 
    "dssd");


var result = await service.GetXmlDocumentModelForEditing(Guid.Parse(createResult.Content!));
//var result = await service.DeleteXmlDocument(Guid.Parse("E85557FD-7D22-46D9-8165-7EC1F9B2DF58"));

Console.WriteLine(result.IsSuccess ? "Success" : result.Error);