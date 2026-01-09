using System.Text.Json.Serialization;
using Skipper.BaitCode.Objects.Instructions;
using Skipper.BaitCode.Types;

namespace Skipper.BaitCode.Objects;

public sealed class BytecodeFunction
{
    // Id, также может содержаться и в классах
    public int FunctionId { get; set; }

    // Название функции
    public string Name { get; set; }

    // Сигнатура
    public BytecodeType ReturnType { get; set; }
    public List<BytecodeFunctionParameter> ParameterTypes { get; set; }

    // Конкретные инструкции
    public List<Instruction> Code { get; set; } = [];

    [JsonInclude]
    public List<BytecodeVariable> Locals { get; private set; } = [];

    public BytecodeFunction(
        int functionId,
        string name,
        BytecodeType returnType,
        List<BytecodeFunctionParameter> parameterTypes)
    {
        FunctionId = functionId;
        Name = name;
        ReturnType = returnType;
        ParameterTypes = parameterTypes;
    }
}