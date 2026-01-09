namespace Skipper.BaitCode.Types;

public sealed class ArrayType : BytecodeType
{
    public BytecodeType ElementType { get; set; } = default!;

    public ArrayType() { }

    public ArrayType(BytecodeType element)
    {
        ElementType = element;
    }
}
