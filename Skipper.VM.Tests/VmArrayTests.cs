using Skipper.BaitCode.Objects;
using Skipper.BaitCode.Objects.Instructions;
using Skipper.Runtime;
using Skipper.Runtime.Values;
using Xunit;

namespace Skipper.VM.Tests;

public class VmArrayTests
{
    [Fact]
    public void Run_ArrayOperations_ReadWrite()
    {
        // Логика теста (упрощенная без Length):
        // int size = 5;
        // int[] arr = new int[size];
        // arr[1] = 42;
        // return arr[1]; // 42

        BytecodeProgram program = new();
        program.ConstantPool.Add(5);  // idx 0: size
        program.ConstantPool.Add(1);  // idx 1: index
        program.ConstantPool.Add(42); // idx 2: value

        BytecodeFunction func = new(0, "main", null!, [])
        {
            Code =
            [
                // 1. Создаем массив
                new Instruction(OpCode.PUSH, 0),           // Stack: [5]
                new Instruction(OpCode.NEW_ARRAY),         // Stack: [ArrRef]
                new Instruction(OpCode.STORE_LOCAL, 0, 0), // Locals[0] = ArrRef

                // 2. arr[1] = 42
                // Порядок стека для SET_ELEMENT: ArrRef, Index, Value
                new Instruction(OpCode.LOAD_LOCAL, 0, 0),  // Stack: [ArrRef]
                new Instruction(OpCode.PUSH, 1),           // Stack: [ArrRef, 1]
                new Instruction(OpCode.PUSH, 2),           // Stack: [ArrRef, 1, 42]
                new Instruction(OpCode.SET_ELEMENT),       // Stack: []

                // 3. Читаем arr[1]
                new Instruction(OpCode.LOAD_LOCAL, 0, 0),  // Stack: [ArrRef]
                new Instruction(OpCode.PUSH, 1),           // Stack: [ArrRef, 1]
                new Instruction(OpCode.GET_ELEMENT),       // Stack: [42]

                new Instruction(OpCode.RETURN)
            ]
        };
        program.Functions.Add(func);

        VirtualMachine vm = new(program, new RuntimeContext());
        Value result = vm.Run("main");

        Assert.Equal(42, result.AsInt());
    }
}
