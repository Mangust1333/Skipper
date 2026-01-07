using Skipper.BaitCode.Types;
using System.Text.Json.Serialization;

namespace Skipper.BaitCode.Objects;

// Результирующий класс, который после парсинга сериализуется
public sealed class BytecodeProgram
{
    // Таблица типов
    public List<BytecodeType> Types { get; set; } = [];
    // Все переменные (числа, строки, bool и т.д.)
    [JsonInclude]
    public List<BytecodeVariable> Variables { get; private set; } = [];
    // Все функции программы (включая методы классов)
    [JsonInclude]
    public List<BytecodeFunction> Functions { get; private set; } = [];
    // Все классы программы
    [JsonInclude]
    public List<BytecodeClass> Classes { get; private set; } = [];

    // ID функции-точки входа
    public int EntryFunctionId { get; set; }

    [JsonInclude]
    // Общий пул констант (числа, строки, bool, имена классов)
    public List<object> ConstantPool { get; private set; } = [];
}

