using Skipper.BaitCode.Objects;
using Skipper.Runtime.Values;

namespace Skipper.VM;

public sealed class StackFrame
{
    public BytecodeFunction Function { get; }
    public int ReturnAddress { get; }
    public Value[] Locals { get; }

    public StackFrame(BytecodeFunction function, int returnAddress)
    {
        Function = function;
        ReturnAddress = returnAddress;

        // Считаем размер массива: Количество Аргументов + Количество Локальных переменных
        var totalCount = function.ParameterTypes.Count + function.Locals.Count;

        // ЗАЩИТА ОТ ОШИБОК ГЕНЕРАТОРА:
        // Если генератор байткода еще не заполняет список Locals, а инструкции LOAD/STORE 
        // используют большие индексы, программа упадет.
        // Поэтому для MVP мы выделяем минимум 64 слота (с запасом).
        var safeSize = Math.Max(totalCount, 64);

        Locals = new Value[safeSize];
    }
}
