using Skipper.BaitCode.Types;
using System.Text.Json.Serialization;

namespace Skipper.BaitCode.Objects;

public sealed class BytecodeClass
{
    // Id класса
    public int ClassId { get; set; }
    // Имя класса
    public string Name { get; set; } = string.Empty;
    // Id полей и тип по названию в классе
    public Dictionary<string, FieldInfo> Fields { get; set; } = new();
    // Id методов по названию в классе
    public Dictionary<string, int> Methods { get; set; } = new();
    // Количество полей в классе (возможно не нужно, ведь количество памяти будет выделяться по другому)
    [JsonIgnore]
    public int ObjectSize => Fields.Count;

    public BytecodeClass() { }

    public BytecodeClass(int classId, string name)
    {
        ClassId = classId;
        Name = name;
    }
}

public class FieldInfo
{
    public int FieldId { get; set; }
    public BytecodeType Type { get; set; } = default!;
}