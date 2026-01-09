namespace Skipper.BaitCode.Types;

public sealed class PrimitiveType : BytecodeType
{
    public string Name { get; }

    public PrimitiveType(string name)
    {
        Name = name;
    }
}