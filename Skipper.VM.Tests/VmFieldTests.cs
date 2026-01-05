using Skipper.BaitCode.Objects;
using Skipper.BaitCode.Objects.Instructions;
using Skipper.Runtime;
using Skipper.Runtime.Values;
using Xunit;

namespace Skipper.VM.Tests;

public class VmFieldTests
{
    [Fact]
    public void Run_ObjectFields_ReadWriteMultipleFields()
    {
        // class Point { int x; int y; }

        BytecodeProgram program = new();
        BytecodeClass cls = new(0, "Point");

        cls.Fields.Add("x", (0, null!));
        cls.Fields.Add("y", (1, null!));

        program.Classes.Add(cls);

        program.ConstantPool.Add(10);
        program.ConstantPool.Add(20);

        BytecodeFunction func = new(0, "main", null!, [])
        {
            Code =
            [
                // p = new Point()
                new Instruction(OpCode.NEW_OBJECT, 0),
                new Instruction(OpCode.STORE, 0),

                // p.x = 10 (Используем индекс 0, который мы задали выше для "x")
                new Instruction(OpCode.LOAD, 0),       // Ref
                new Instruction(OpCode.PUSH, 0),       // 10
                new Instruction(OpCode.SET_FIELD, 0),  // Field idx 0

                // p.y = 20 (Используем индекс 1 для "y")
                new Instruction(OpCode.LOAD, 0),       // Ref
                new Instruction(OpCode.PUSH, 1),       // 20
                new Instruction(OpCode.SET_FIELD, 1),  // Field idx 1

                // Calc p.x + p.y
                new Instruction(OpCode.LOAD, 0),
                new Instruction(OpCode.GET_FIELD, 0),  // Читаем idx 0
                
                new Instruction(OpCode.LOAD, 0),
                new Instruction(OpCode.GET_FIELD, 1),  // Читаем idx 1

                new Instruction(OpCode.ADD),
                new Instruction(OpCode.RETURN)
            ]
        };
        program.Functions.Add(func);

        VirtualMachine vm = new(program, new RuntimeContext());
        Value result = vm.Run("main");

        Assert.Equal(30, result.AsInt());
    }
}