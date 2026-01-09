namespace Skipper.BaitCode.Objects;

public sealed class BytecodeClass
{
    // Id класса
    public int ClassId { get; }

    // Имя класса
    public string Name { get; }

    // Id полей и тип по названию в классе
    public Dictionary<string, BytecodeClassField> Fields { get; } = [];

    // Id методов по названию в классе
    public Dictionary<string, int> Methods { get; } = new();

    public BytecodeClass(int classId, string name)
    {
        ClassId = classId;
        Name = name;
    }
}