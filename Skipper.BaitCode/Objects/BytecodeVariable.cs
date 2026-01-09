using Skipper.BaitCode.Types;

namespace Skipper.BaitCode.Objects;

public class BytecodeVariable(int variableId, string name, BytecodeType type)
{
    // Id переменной
    public int VariableId { get; set; } = variableId;
    // Название переменной
    public string Name { get; set; } = name;
    // Тип переменной
    public BytecodeType Type { get; set; } = type;
}
