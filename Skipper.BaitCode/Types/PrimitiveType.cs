namespace Skipper.BaitCode.Types;

public sealed class PrimitiveType : BytecodeType
{
    public string Name { get; set; } = string.Empty;

    public PrimitiveType() { }

    public PrimitiveType(string name)
    {
        Name = name;
    }
}