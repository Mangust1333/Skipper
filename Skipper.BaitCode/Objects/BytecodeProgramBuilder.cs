namespace Skipper.BaitCode.Objects;

public class BytecodeProgramBuilder(BytecodeProgram program)
{
    private readonly BytecodeProgram _program = program;
    
    public int RegisterFunction(string name)
    {
        var isExisting = _program.Functions.FirstOrDefault(f => f.Name == name);
        if (isExisting != null)
        {
            return isExisting.FunctionId;
        }

        var id = _program.Functions.Count;
        var fn = new BytecodeFunction(id, name);

        _program.Functions.Add(fn);
        return id;
    }
    
    public int RegisterClass(string name)
    {
        var isExisting = _program.Classes.FirstOrDefault(c => c.Name == name);
        if (isExisting != null)
        {
            return isExisting.ClassId;
        }

        var id = _program.Classes.Count;
        var cls = new BytecodeClass(id, name);

        _program.Classes.Add(cls);
        return id;
    }
    
    public static int RegisterField(BytecodeClass cls, string fieldName)
    {
        if (cls.Fields.TryGetValue(fieldName, out var index))
        {
            return index;
        }

        index = cls.Fields.Count;
        cls.Fields[fieldName] = index;
        return index;
    }
    
    public static int RegisterMethod(BytecodeClass cls, string methodName, int functionId)
    {
        cls.Methods[methodName] = functionId;
        return functionId;
    }
}