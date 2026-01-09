namespace Skipper.BaitCode.Types;

public sealed class ClassType : BytecodeType
{
    public int ClassId { get; }
    public string Name { get; }

    public ClassType(int classId, string name)
    {
        ClassId = classId;
        Name = name;
    }
}