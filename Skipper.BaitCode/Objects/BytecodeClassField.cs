using Skipper.BaitCode.Types;

namespace Skipper.BaitCode.Objects;

public sealed class BytecodeClassField
{
    public int FieldId { get; set; }
    public BytecodeType Type { get; set; }

    public BytecodeClassField(int fieldId, BytecodeType type)
    {
        FieldId = fieldId;
        Type = type;
    }
}