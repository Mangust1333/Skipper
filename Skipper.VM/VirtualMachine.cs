using Skipper.BaitCode.Objects;
using Skipper.BaitCode.Objects.Instructions;
using Skipper.Runtime;
using Skipper.Runtime.Abstractions;
using Skipper.Runtime.Values;

namespace Skipper.VM;

public sealed class VirtualMachine : IRootProvider
{
    private readonly RuntimeContext _runtime;
    private readonly BytecodeProgram _program;

    // Стек вызовов функций
    private readonly Stack<StackFrame> _callStack = new();

    // Глобальный стек операндов
    private readonly Stack<Value> _evalStack = new();

    // Регистры виртуальной машины
    private int _ip; // Instruction Pointer
    private BytecodeFunction? _currentFunc;
    private Value[]? _currentLocals;

    public VirtualMachine(BytecodeProgram program, RuntimeContext runtime)
    {
        _program = program;
        _runtime = runtime;
    }

    /// <summary>
    /// Запускает выполнение программы с указанной точки входа.
    /// </summary>
    public Value Run(string entryPointName)
    {
        BytecodeFunction? mainFunc = _program.Functions.FirstOrDefault(f => f.Name == entryPointName);
        if (mainFunc == null)
        {
            throw new InvalidOperationException($"Function '{entryPointName}' not found.");
        }

        // Инициализируем первый фрейм
        PushFrame(mainFunc, -1);

        try
        {
            // Главный цикл интерпретации
            while (_currentFunc != null && _ip < _currentFunc.Code.Count)
            {
                Instruction instr = _currentFunc.Code[_ip];
                Execute(instr);

                // Если стек вызовов пуст, значит вышли из Main
                if (_callStack.Count == 0)
                {
                    break;
                }
            }
        } catch (Exception)
        {
            Console.WriteLine($"[VM Runtime Error] Func: {_currentFunc?.Name}, IP: {_ip}, Op: {_currentFunc?.Code[_ip].OpCode}");
            throw; // Пробрасываем ошибку дальше для тестов/отладки
        }

        // Возвращаем результат (если есть)
        return _evalStack.Count > 0 ? _evalStack.Pop() : Value.Null();
    }

    private void Execute(Instruction instr)
    {
        switch (instr.OpCode)
        {
            // ===========================
            // Стек и Константы
            // ===========================
            case OpCode.PUSH:
            {
                var constIdx = Convert.ToInt32(instr.Operands[0]);
                var val = _program.ConstantPool[constIdx];
                _evalStack.Push(ValueFromConst(val));
                _ip++;
            }
            break;

            case OpCode.POP:
                _ = _evalStack.Pop();
                _ip++;
                break;

            case OpCode.DUP:
                _evalStack.Push(_evalStack.Peek());
                _ip++;
                break;

            case OpCode.SWAP:
            {
                Value top = _evalStack.Pop();
                Value below = _evalStack.Pop();
                _evalStack.Push(top);
                _evalStack.Push(below);
                _ip++;
            }
            break;

            // ===========================
            // Локальные переменные
            // ===========================
            case OpCode.LOAD:
            {
                var slot = Convert.ToInt32(instr.Operands[0]);
                _evalStack.Push(_currentLocals![slot]);
                _ip++;
            }
            break;

            case OpCode.STORE:
            {
                var slot = Convert.ToInt32(instr.Operands[0]);
                _currentLocals![slot] = _evalStack.Pop();
                _ip++;
            }
            break;

            // ===========================
            // Арифметика
            // ===========================
            case OpCode.ADD:
            {
                var b = _evalStack.Pop().Raw;
                var a = _evalStack.Pop().Raw;
                _evalStack.Push(Value.FromInt((int)(a + b)));
                _ip++;
            }
            break;

            case OpCode.SUB:
            {
                var b = _evalStack.Pop().Raw;
                var a = _evalStack.Pop().Raw;
                _evalStack.Push(Value.FromInt((int)(a - b)));
                _ip++;
            }
            break;

            case OpCode.MUL:
            {
                var b = _evalStack.Pop().Raw;
                var a = _evalStack.Pop().Raw;
                _evalStack.Push(Value.FromInt((int)(a * b)));
                _ip++;
            }
            break;

            case OpCode.DIV:
            {
                var b = _evalStack.Pop().Raw;
                if (b == 0)
                {
                    throw new DivideByZeroException();
                }

                var a = _evalStack.Pop().Raw;
                _evalStack.Push(Value.FromInt((int)(a / b)));
                _ip++;
            }
            break;

            case OpCode.MOD:
            {
                var b = _evalStack.Pop().Raw;
                if (b == 0)
                {
                    throw new DivideByZeroException();
                }

                var a = _evalStack.Pop().Raw;
                _evalStack.Push(Value.FromInt((int)(a % b)));
                _ip++;
            }
            break;

            case OpCode.NEG:
            {
                var a = _evalStack.Pop().Raw;
                _evalStack.Push(Value.FromInt((int)-a));
                _ip++;
            }
            break;

            // ===========================
            // Сравнения
            // ===========================
            case OpCode.CMP_EQ:
            {
                var b = _evalStack.Pop().Raw;
                var a = _evalStack.Pop().Raw;
                _evalStack.Push(Value.FromBool(a == b));
                _ip++;
            }
            break;
            case OpCode.CMP_NE:
            {
                var b = _evalStack.Pop().Raw;
                var a = _evalStack.Pop().Raw;
                _evalStack.Push(Value.FromBool(a != b));
                _ip++;
            }
            break;
            case OpCode.CMP_LT:
            {
                var b = _evalStack.Pop().Raw;
                var a = _evalStack.Pop().Raw;
                _evalStack.Push(Value.FromBool(a < b));
                _ip++;
            }
            break;
            case OpCode.CMP_GT:
            {
                var b = _evalStack.Pop().Raw;
                var a = _evalStack.Pop().Raw;
                _evalStack.Push(Value.FromBool(a > b));
                _ip++;
            }
            break;
            case OpCode.CMP_LE:
            {
                var b = _evalStack.Pop().Raw;
                var a = _evalStack.Pop().Raw;
                _evalStack.Push(Value.FromBool(a <= b));
                _ip++;
            }
            break;
            case OpCode.CMP_GE:
            {
                var b = _evalStack.Pop().Raw;
                var a = _evalStack.Pop().Raw;
                _evalStack.Push(Value.FromBool(a >= b));
                _ip++;
            }
            break;

            // ===========================
            // Логика
            // ===========================
            case OpCode.AND:
            {
                var b = _evalStack.Pop().AsBool();
                var a = _evalStack.Pop().AsBool();
                _evalStack.Push(Value.FromBool(a && b));
                _ip++;
            }
            break;
            case OpCode.OR:
            {
                var b = _evalStack.Pop().AsBool();
                var a = _evalStack.Pop().AsBool();
                _evalStack.Push(Value.FromBool(a || b));
                _ip++;
            }
            break;
            case OpCode.NOT:
            {
                var a = _evalStack.Pop().AsBool();
                _evalStack.Push(Value.FromBool(!a));
                _ip++;
            }
            break;

            // ===========================
            // Поток управления
            // ===========================
            case OpCode.JUMP:
                _ip = Convert.ToInt32(instr.Operands[0]);
                break;

            case OpCode.JUMP_IF_TRUE:
            {
                Value cond = _evalStack.Pop();
                if (cond.AsBool())
                {
                    _ip = Convert.ToInt32(instr.Operands[0]);
                } else
                {
                    _ip++;
                }
            }
            break;

            case OpCode.JUMP_IF_FALSE:
            {
                Value cond = _evalStack.Pop();
                if (!cond.AsBool())
                {
                    _ip = Convert.ToInt32(instr.Operands[0]);
                } else
                {
                    _ip++;
                }
            }
            break;

            case OpCode.CALL:
            {
                var funcId = Convert.ToInt32(instr.Operands[0]);
                // Operand[1] usually is arg_count, but we can get it from func definition too
                BytecodeFunction? target = _program.Functions.FirstOrDefault(f => f.FunctionId == funcId);
                if (target == null)
                {
                    throw new InvalidOperationException($"Func ID {funcId} not found");
                }

                // Сохраняем возврат на следующую инструкцию
                PushFrame(target, _ip + 1);
            }
            break;

            case OpCode.RETURN:
                PopFrame();
                break;

            // ===========================
            // Объекты
            // ===========================
            case OpCode.NEW_OBJECT:
            {
                var classId = Convert.ToInt32(instr.Operands[0]);
                BytecodeClass? cls = _program.Classes.FirstOrDefault(c => c.ClassId == classId);
                if (cls == null)
                {
                    throw new InvalidOperationException($"Class ID {classId} not found");
                }

                // Размер данных: кол-во полей * 8 байт (Header обрабатывает RuntimeContext)
                var payloadSize = cls.Fields.Count * 8;

                // 1. Пробуем выделить. Если нет места -> GC.
                if (!_runtime.CanAllocate(payloadSize))
                {
                    _runtime.Collect(this);
                    if (!_runtime.CanAllocate(payloadSize))
                    {
                        throw new OutOfMemoryException("Heap full after GC");
                    }
                }

                var ptr = _runtime.AllocateObject(payloadSize, classId);
                _evalStack.Push(Value.FromObject(ptr));
                _ip++;
            }
            break;

            case OpCode.GET_FIELD:
            {
                var fieldIdx = Convert.ToInt32(instr.Operands[0]);
                Value objRef = _evalStack.Pop();
                CheckNull(objRef);

                Value val = _runtime.ReadField(objRef.AsObject(), fieldIdx);
                _evalStack.Push(val);
                _ip++;
            }
            break;

            case OpCode.SET_FIELD:
            {
                var fieldIdx = Convert.ToInt32(instr.Operands[0]);
                Value val = _evalStack.Pop();
                Value objRef = _evalStack.Pop();
                CheckNull(objRef);

                _runtime.WriteField(objRef.AsObject(), fieldIdx, val);
                _ip++;
            }
            break;

            // ===========================
            // Массивы
            // ===========================
            case OpCode.NEW_ARRAY:
            {
                // [array_id] (тип массива) игнорируем для MVP аллокации, нам нужен размер со стека
                var length = _evalStack.Pop().AsInt();
                if (length < 0)
                {
                    throw new InvalidOperationException("Array size cannot be negative");
                }

                // Размер данных: длина * 8 байт
                var payloadSize = length * 8;

                if (!_runtime.CanAllocate(payloadSize))
                {
                    _runtime.Collect(this);
                    if (!_runtime.CanAllocate(payloadSize))
                    {
                        throw new OutOfMemoryException("Heap full after GC (Array)");
                    }
                }

                var ptr = _runtime.AllocateArray(length);
                _evalStack.Push(Value.FromObject(ptr));
                _ip++;
            }
            break;

            case OpCode.GET_ELEMENT:
            {
                var index = _evalStack.Pop().AsInt();
                Value arrRef = _evalStack.Pop();
                CheckNull(arrRef);

                // Runtime сам проверит границы массива
                Value val = _runtime.ReadArrayElement(arrRef.AsObject(), index);
                _evalStack.Push(val);
                _ip++;
            }
            break;

            case OpCode.SET_ELEMENT:
            {
                Value val = _evalStack.Pop();
                var index = _evalStack.Pop().AsInt();
                Value arrRef = _evalStack.Pop();
                CheckNull(arrRef);

                _runtime.WriteArrayElement(arrRef.AsObject(), index, val);
                _ip++;
            }
            break;

            case OpCode.ARRAY_LENGTH:
            {
                Value arrRef = _evalStack.Pop();
                CheckNull(arrRef);
                var len = _runtime.GetArrayLength(arrRef.AsObject());
                _evalStack.Push(Value.FromInt(len));
                _ip++;
            }
            break;

            // ===========================
            // IO / Debug
            // ===========================
            case OpCode.PRINT:
            {
                Value val = _evalStack.Pop();
                // Выводим значение. Если объект - выводим адрес или тип.
                var output = val.Kind == ValueKind.ObjectRef
                        ? $"[Object Ref: 0x{val.Raw:X}]"
                        : val.ToString();

                Console.WriteLine(output);
                _ip++;
            }
            break;

            default:
                throw new NotImplementedException($"OpCode {instr.OpCode} not implemented.");
        }
    }

    // --- Управление стеком фреймов ---

    private void PushFrame(BytecodeFunction func, int returnAddress)
    {
        StackFrame frame = new(func, returnAddress);

        // Аргументы лежат на стеке в обратном порядке (последний аргумент на вершине)
        var argCount = func.ParameterTypes.Count;
        for (var i = argCount - 1; i >= 0; i--)
        {
            if (_evalStack.Count == 0)
            {
                throw new InvalidOperationException("Not enough arguments on stack");
            }

            frame.Locals[i] = _evalStack.Pop();
        }

        _callStack.Push(frame);
        _currentFunc = func;
        _currentLocals = frame.Locals;
        _ip = 0; // В новой функции начинаем с 0
    }

    private void PopFrame()
    {
        StackFrame endingFrame = _callStack.Pop();

        if (_callStack.Count > 0)
        {
            StackFrame parent = _callStack.Peek();
            _currentFunc = parent.Function;
            _currentLocals = parent.Locals;
            _ip = endingFrame.ReturnAddress;
        } else
        {
            // Программа завершилась
            _currentFunc = null;
            _currentLocals = null;
        }
    }

    // --- Вспомогательные методы ---

    private void CheckNull(Value refVal)
    {
        if (refVal.Kind == ValueKind.Null || (refVal.Kind == ValueKind.ObjectRef && refVal.Raw == 0))
        {
            throw new NullReferenceException("Null pointer exception");
        }
    }

    private Value ValueFromConst(object c)
    {
        return c switch
        {
            int i => Value.FromInt(i),
            long l => Value.FromInt((int)l),
            double d => Value.FromDouble(d),
            bool b => Value.FromBool(b),
            char ch => Value.FromChar(ch),
            string => throw new NotImplementedException("String constants require intern pool implementation"),
            _ => throw new NotImplementedException($"Const type {c.GetType()} not supported")
        };
    }

    // --- GC Interface (IRootProvider) ---

    public IEnumerable<nint> EnumerateRoots()
    {
        // 1. Корни со стека операндов
        foreach (Value val in _evalStack)
        {
            if (val.Kind == ValueKind.ObjectRef && val.Raw != 0)
            {
                yield return (nint)val.Raw;
            }
        }

        // 2. Корни из локальных переменных всех активных фреймов
        foreach (StackFrame frame in _callStack)
        {
            foreach (Value local in frame.Locals)
            {
                // Проверяем, является ли локальная переменная ссылкой
                if (local.Kind == ValueKind.ObjectRef && local.Raw != 0)
                {
                    yield return (nint)local.Raw;
                }
            }
        }
    }
}