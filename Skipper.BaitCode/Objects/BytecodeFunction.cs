using System.Text.Json.Serialization;
using Skipper.BaitCode.Objects.Instructions;
using Skipper.BaitCode.Types;

namespace Skipper.BaitCode.Objects;

public class BytecodeFunction
{
    // Id, также может содержаться и в классах
    public int FunctionId { get; set; }
    // Название функции
    public string Name { get; set; } = string.Empty;
    // Сигнатура
    public BytecodeType ReturnType { get; set; } = default!;
    public List<FuncParam> ParameterTypes { get; set; } = new();
    // Конкретные инструкции
    public List<Instruction> Code { get; set; } = new();
    [JsonInclude]
    public List<BytecodeVariable> Locals { get; private set; } = new();

    public BytecodeFunction() { }

    public BytecodeFunction(
        int id,
        string name,
        BytecodeType returnType,
        List<FuncParam> parameters)
    {
        FunctionId = id;
        Name = name;
        ReturnType = returnType;
        ParameterTypes = parameters;
    }
}

public class FuncParam
{
    public string Name { get; set; } = string.Empty;
    public BytecodeType Type { get; set; } = default!;

    public FuncParam() { }

    public FuncParam(string name, BytecodeType type)
    {
        Name = name;
        Type = type;
    }
}