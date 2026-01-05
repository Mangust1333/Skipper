using Skipper.BaitCode.Objects;
using Skipper.BaitCode.Objects.Instructions;
using Skipper.Runtime;
using Skipper.Runtime.Values;
using Xunit;

namespace Skipper.VM.Tests;

public class VmRecursionTests
{
    [Fact]
    public void Run_Factorial_RecursionWorks()
    {
        // fn fact(n) {
        //    if (n <= 1) return 1;
        //    return n * fact(n - 1);
        // }

        BytecodeProgram program = new();
        program.ConstantPool.Add(1); // Index 0
        program.ConstantPool.Add(5); // Index 1

        BytecodeFunction factFunc = new(0, "fact", null!, [])
        {
            // Аргумент 'n' будет в Locals[0]
            ParameterTypes = [("n", null!)],
            Code =
            [
                new Instruction(OpCode.LOAD, 0),          // 0: Загрузить n
                new Instruction(OpCode.PUSH, 0),          // 1: Загрузить 1
                new Instruction(OpCode.CMP_LE),           // 2: n <= 1 ?
                new Instruction(OpCode.JUMP_IF_FALSE, 6), // 3: Если ложь, прыгаем на индекс 6

                // Блок if: return 1
                new Instruction(OpCode.PUSH, 0),          // 4: 1
                new Instruction(OpCode.RETURN),           // 5: return

                // Блок else: return n * fact(n-1)
                new Instruction(OpCode.LOAD, 0),          // 6: n
                new Instruction(OpCode.LOAD, 0),          // 7: n
                new Instruction(OpCode.PUSH, 0),          // 8: 1
                new Instruction(OpCode.SUB),              // 9: n - 1
                new Instruction(OpCode.CALL, 0),          // 10: fact(n-1)
                new Instruction(OpCode.MUL),              // 11: n * result
                new Instruction(OpCode.RETURN)            // 12: return
            ]
        };

        // Main вызывает fact(5)
        BytecodeFunction mainFunc = new(1, "main", null!, [])
        {
            Code =
            [
                 new Instruction(OpCode.PUSH, 1), // Загрузить 5
                 new Instruction(OpCode.CALL, 0), // Вызов fact
                 new Instruction(OpCode.RETURN)
            ]
        };

        program.Functions.Add(factFunc);
        program.Functions.Add(mainFunc);

        VirtualMachine vm = new(program, new RuntimeContext());
        Value result = vm.Run("main");

        Assert.Equal(120, result.AsInt()); // 5! = 120
    }
}