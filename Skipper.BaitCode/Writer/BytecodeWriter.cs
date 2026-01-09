using System.Text.Json;
using System.Text.Json.Serialization;
using Skipper.BaitCode.Objects;

namespace Skipper.BaitCode.Writer;

public class BytecodeWriter
{
    private readonly BytecodeProgram _program;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        IncludeFields = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public BytecodeWriter(BytecodeProgram program)
    {
        _program = program;
    }

    public void SaveToFile(string path)
    {
        var json = JsonSerializer.Serialize(_program, JsonOptions);
        File.WriteAllText(path, json);
    }

    public static BytecodeProgram LoadFromFile(string path)
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<BytecodeProgram>(json, JsonOptions)
               ?? throw new InvalidOperationException("Не удалось десериализовать BytecodeProgram");
    }
}