using Skipper.BaitCode.Objects;
using Skipper.BaitCode.Objects.Instructions;
using Skipper.Runtime;
using Skipper.Runtime.Values;
using Xunit;

namespace Skipper.VM.Tests;

public class VmArithmeticTests
{
    private VirtualMachine CreateVm(List<Instruction> code, List<object>? constants = null)
    {
        BytecodeProgram program = new();
        if (constants != null)
        {
            program.ConstantPool.AddRange(constants);
        }

        // Функция main с ID 0
        BytecodeFunction func = new(0, "main", null!, [])
        {
            Code = code
        };
        program.Functions.Add(func);

        return new VirtualMachine(program, new RuntimeContext());
    }

    [Fact]
    public void Run_Add_TwoNumbers_ReturnsSum()
    {
        // 10 + 20 = 30
        List<Instruction> code =
        [
            new Instruction(OpCode.PUSH, 0), // 10
            new Instruction(OpCode.PUSH, 1), // 20
            new Instruction(OpCode.ADD),
            new Instruction(OpCode.RETURN)
        ];
        VirtualMachine vm = CreateVm(code, [10, 20]);

        Value result = vm.Run("main");

        Assert.Equal(30, result.AsInt());
    }

    [Fact]
    public void Run_ComplexMath_RespectsStackOrder()
    {
        // (10 * 2) - 5 = 15
        List<Instruction> code =
        [
            new Instruction(OpCode.PUSH, 0), // 10
            new Instruction(OpCode.PUSH, 1), // 2
            new Instruction(OpCode.MUL),     // 20
            new Instruction(OpCode.PUSH, 2), // 5
            new Instruction(OpCode.SUB),     // 20 - 5 = 15
            new Instruction(OpCode.RETURN)
        ];
        VirtualMachine vm = CreateVm(code, [10, 2, 5]);

        Value result = vm.Run("main");

        Assert.Equal(15, result.AsInt());
    }

    [Fact]
    public void Run_Comparison_ReturnsBool()
    {
        // 10 > 5 -> true
        List<Instruction> code =
        [
            new Instruction(OpCode.PUSH, 0), // 10
            new Instruction(OpCode.PUSH, 1), // 5
            new Instruction(OpCode.CMP_GT),
            new Instruction(OpCode.RETURN)
        ];
        VirtualMachine vm = CreateVm(code, [10, 5]);

        Value result = vm.Run("main");

        Assert.True(result.AsBool());
    }

    [Fact]
    public void Run_Variables_StoreAndLoad()
    {
        // x = 42; return x;
        List<Instruction> code =
        [
            new Instruction(OpCode.PUSH, 0),           // [42]
            new Instruction(OpCode.STORE_LOCAL, 0, 0), // Locals[0] = 42 (funcId 0, slot 0)
            new Instruction(OpCode.PUSH, 1),           // Мусор
            new Instruction(OpCode.POP),               // Очистка
            new Instruction(OpCode.LOAD_LOCAL, 0, 0),  // Загрузка Locals[0]
            new Instruction(OpCode.RETURN)
        ];
        VirtualMachine vm = CreateVm(code, [42, 99]);

        Value result = vm.Run("main");

        Assert.Equal(42, result.AsInt());
    }

    [Fact]
    public void Run_JumpIfFalse_Branching()
    {
        // if (false) return 100 else return 200
        List<Instruction> code =
        [
            new Instruction(OpCode.PUSH, 0),          // false
            new Instruction(OpCode.JUMP_IF_FALSE, 4), // Прыжок на 4 (если false)
            new Instruction(OpCode.PUSH, 1),          // 100
            new Instruction(OpCode.RETURN),           // (3)
            new Instruction(OpCode.PUSH, 2),          // 200 (индекс 4)
            new Instruction(OpCode.RETURN)            // (5)
        ];
        VirtualMachine vm = CreateVm(code, [false, 100, 200]);

        Value result = vm.Run("main");

        Assert.Equal(200, result.AsInt());
    }
}