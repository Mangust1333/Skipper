using Skipper.BaitCode.Types;

namespace Skipper.BaitCode.Objects;

public sealed class BytecodeVariable
{
    public int VariableId { get; }
    public string Name { get; }
    public BytecodeType Type { get; }

    public BytecodeVariable(int variableId, string name, BytecodeType type)
    {
        VariableId = variableId;
        Name = name;
        Type = type;
    }
}