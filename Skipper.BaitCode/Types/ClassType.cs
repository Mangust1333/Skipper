namespace Skipper.BaitCode.Types;

public sealed class ClassType : BytecodeType
{
    public int ClassId { get; set; }
    public string Name { get; set; } = string.Empty;

    public ClassType() { }

    public ClassType(int classId, string name)
    {
        ClassId = classId;
        Name = name;
    }
}
