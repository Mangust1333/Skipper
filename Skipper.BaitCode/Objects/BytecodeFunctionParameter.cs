using Skipper.BaitCode.Types;

namespace Skipper.BaitCode.Objects;

public sealed class BytecodeFunctionParameter
{
    public string Name { get; }
    public BytecodeType Type { get; }

    public BytecodeFunctionParameter(string name, BytecodeType type)
    {
        Name = name;
        Type = type;
    }
}