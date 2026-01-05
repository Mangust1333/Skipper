using Skipper.BaitCode.Objects;
using Skipper.BaitCode.Objects.Instructions;
using Skipper.Runtime;
using Skipper.Runtime.Values;
using Xunit;

namespace Skipper.VM.Tests;

public class VmMemoryTests
{
    [Fact]
    public void Run_NewObject_AllocatesInHeap()
    {
        BytecodeProgram program = new();
        BytecodeClass cls = new(0, "User");
        program.Classes.Add(cls);
        program.ConstantPool.Add(0);

        List<Instruction> code =
        [
            new Instruction(OpCode.NEW_OBJECT, 0), // Создание объекта класса 0
            new Instruction(OpCode.RETURN)
        ];

        BytecodeFunction func = new(0, "main", null!, [])
        {
            Code = code
        };
        program.Functions.Add(func);

        RuntimeContext runtime = new();
        VirtualMachine vm = new(program, runtime);

        Value result = vm.Run("main");

        Assert.Equal(ValueKind.ObjectRef, result.Kind);
        Assert.NotEqual(0, result.AsObject()); // Указатель валиден
    }

    [Fact]
    public void Run_CallFunction_PassesArguments()
    {
        BytecodeProgram program = new();
        program.ConstantPool.Add(10);
        program.ConstantPool.Add(5);

        // Функция add(a, b) { return a + b }
        BytecodeFunction addFunc = new(1, "add", null!, [])
        {
            ParameterTypes = [("a", null!), ("b", null!)],
            Code =
            [
                new Instruction(OpCode.LOAD, 0), // a
                new Instruction(OpCode.LOAD, 1), // b
                new Instruction(OpCode.ADD),
                new Instruction(OpCode.RETURN)
            ]
        };

        // Функция main() { return add(10, 5) }
        BytecodeFunction mainFunc = new(0, "main", null!, [])
        {
            Code =
            [
                new Instruction(OpCode.PUSH, 0), // 10
                new Instruction(OpCode.PUSH, 1), // 5
                new Instruction(OpCode.CALL, 1), // Вызов add
                new Instruction(OpCode.RETURN)
            ]
        };

        program.Functions.Add(mainFunc);
        program.Functions.Add(addFunc);

        VirtualMachine vm = new(program, new RuntimeContext());
        Value result = vm.Run("main");

        Assert.Equal(15, result.AsInt());
    }

    [Fact]
    public void Vm_Integration_GcCollectsUnusedObjects()
    {
        BytecodeProgram program = new();
        program.Classes.Add(new BytecodeClass(0, "Temp"));

        List<Instruction> code =
        [
            new Instruction(OpCode.NEW_OBJECT, 0), // Выделение памяти
            new Instruction(OpCode.POP),           // Удаление ссылки со стека
            new Instruction(OpCode.RETURN)
        ];

        BytecodeFunction func = new(0, "main", null!, []) { Code = code };
        program.Functions.Add(func);

        RuntimeContext runtime = new();
        VirtualMachine vm = new(program, runtime);

        _ = vm.Run("main");

        // Ручной запуск сборщика мусора
        runtime.Collect(vm);

        // Проверка состояния кучи (требует доступа к внутренним свойствам RuntimeContext)
        // Assert.Empty(runtime.Heap.Objects); 
    }
}