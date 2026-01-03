namespace Skipper.BaitCode;

public readonly struct Instruction(OpCode opCode, int operand)
{
    public OpCode OpCode { get; } = opCode;

    public int Operand { get; } = operand;
}