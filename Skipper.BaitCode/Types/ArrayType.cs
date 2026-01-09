namespace Skipper.BaitCode.Types;

public sealed class ArrayType : BytecodeType
{
    public BytecodeType ElementType { get; }

    public ArrayType(BytecodeType elementType)
    {
        ElementType = elementType;
    }
}