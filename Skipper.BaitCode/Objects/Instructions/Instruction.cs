namespace Skipper.BaitCode.Objects.Instructions;

public sealed class Instruction(OpCode opCode, params object[] operands)
{
    private OpCode OpCode { get; } = opCode;
    private IReadOnlyList<object> Operands { get; } = operands;

    public override string ToString()
    {
        return Operands.Count == 0 ? OpCode.ToString() : $"{OpCode} {string.Join(", ", Operands)}";
    }
}