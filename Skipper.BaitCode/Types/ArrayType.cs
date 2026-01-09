namespace Skipper.BaitCode.Types;

public sealed class ArrayType : BytecodeType
{
    public BytecodeType ElementType { get; set; } = null!;

    public ArrayType() { }

    public ArrayType(BytecodeType element)
    {
        ElementType = element;
    }
}
