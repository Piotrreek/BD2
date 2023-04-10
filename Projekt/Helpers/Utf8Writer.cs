using System.Text;

namespace Projekt.Helpers;

public sealed class Utf8StringWriter : StringWriter
{
    public override Encoding Encoding => Encoding.UTF8;
}