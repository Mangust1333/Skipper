using System.Text.Json.Serialization;
using Skipper.BaitCode.Types;

namespace Skipper.BaitCode.Objects;

public sealed class BytecodeClass
{
    // Id класса
    public int ClassId { get; set; }
    // Имя класса
    public string Name { get; set; } = string.Empty;
    // Id полей и тип по названию в классе
    public Dictionary<string, FieldInfo> Fields { get; set; } = [];
    // Id методов по названию в классе
    public Dictionary<string, int> Methods { get; set; } = new();
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
