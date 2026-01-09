using Skipper.BaitCode.Objects;
using Skipper.BaitCode.Objects.Instructions;
using Skipper.BaitCode.Types;
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

        cls.Fields.Add("x", new FieldInfo { FieldId = 0, Type = new PrimitiveType("int") });
        cls.Fields.Add("y", new FieldInfo { FieldId = 1, Type = new PrimitiveType("int") });

        program.Classes.Add(cls);

        program.ConstantPool.Add(10);
        program.ConstantPool.Add(20);

        BytecodeFunction func = new(0, "main", null!, [])
        {
            Code =
            [
                // p = new Point()
                new Instruction(OpCode.NEW_OBJECT, 0),     // Stack: [ref]
                new Instruction(OpCode.STORE_LOCAL, 0, 0), // Locals[0] = ref

                // p.x = 10
                new Instruction(OpCode.LOAD_LOCAL, 0, 0),  // [ref]
                new Instruction(OpCode.PUSH, 0),           // [ref, 10]
                new Instruction(OpCode.SET_FIELD, 0, 0),   // [classId=0, fieldIdx=0] -> p.x = 10

                // p.y = 20
                new Instruction(OpCode.LOAD_LOCAL, 0, 0),  // [ref]
                new Instruction(OpCode.PUSH, 1),           // [ref, 20]
                new Instruction(OpCode.SET_FIELD, 0, 1),   // [classId=0, fieldIdx=1] -> p.y = 20

                // Calc p.x + p.y
                new Instruction(OpCode.LOAD_LOCAL, 0, 0),  // [ref]
                new Instruction(OpCode.GET_FIELD, 0, 0),   // [10]
                
                new Instruction(OpCode.LOAD_LOCAL, 0, 0),  // [ref] (стек был [10, ref])
                new Instruction(OpCode.GET_FIELD, 0, 1),   // [10, 20]

                new Instruction(OpCode.ADD),               // [30]
                new Instruction(OpCode.RETURN)
            ]
        };
        program.Functions.Add(func);

        VirtualMachine vm = new(program, new RuntimeContext());
        Value result = vm.Run("main");

        Assert.Equal(30, result.AsInt());
    }
}
