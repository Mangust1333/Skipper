using Skipper.BaitCode.Objects;
using Skipper.BaitCode.Objects.Instructions;
using Skipper.Runtime;
using Xunit;

namespace Skipper.VM.Tests;

public class VmErrorTests
{
    [Fact]
    public void Run_DivisionByZero_ThrowsException()
    {
        BytecodeProgram program = new();
        program.ConstantPool.Add(10);
        program.ConstantPool.Add(0);

        BytecodeFunction func = new(0, "main", null!, [])
        {
            Code =
            [
                new Instruction(OpCode.PUSH, 0), // 10
                new Instruction(OpCode.PUSH, 1), // 0
                new Instruction(OpCode.DIV),
                new Instruction(OpCode.RETURN)
            ]
        };
        program.Functions.Add(func);
        VirtualMachine vm = new(program, new RuntimeContext());

        _ = Assert.Throws<DivideByZeroException>(() => vm.Run("main"));
    }

    [Fact]
    public void Run_NullReference_ThrowsException()
    {
        BytecodeProgram program = new();
        program.ConstantPool.Add(null); // null

        BytecodeFunction func = new(0, "main", null!, [])
        {
            Code =
            [
                new Instruction(OpCode.PUSH, 0),      // Загрузка null
                new Instruction(OpCode.GET_FIELD, 0), // Попытка чтения поля у null
                new Instruction(OpCode.RETURN)
            ]
        };
        program.Functions.Add(func);
        VirtualMachine vm = new(program, new RuntimeContext());

        _ = Assert.Throws<NullReferenceException>(() => vm.Run("main"));
    }

    [Fact]
    public void Run_ArrayIndexOutOfBounds_ThrowsException()
    {
        BytecodeProgram program = new();
        program.ConstantPool.Add(2); // size
        program.ConstantPool.Add(5); // bad index

        BytecodeFunction func = new(0, "main", null!, [])
        {
            Code =
            [
                new Instruction(OpCode.PUSH, 0),     // Размер 2
                new Instruction(OpCode.NEW_ARRAY),
                new Instruction(OpCode.PUSH, 1),     // Индекс 5
                new Instruction(OpCode.GET_ELEMENT), // Выход за границы массива
                new Instruction(OpCode.RETURN)
            ]
        };
        program.Functions.Add(func);
        VirtualMachine vm = new(program, new RuntimeContext());

        _ = Assert.Throws<IndexOutOfRangeException>(() => vm.Run("main"));
    }
}