using System.Text.Json.Serialization;

namespace Skipper.BaitCode.Types;

[JsonDerivedType(typeof(PrimitiveType), typeDiscriminator: "primitive")]
[JsonDerivedType(typeof(ClassType), typeDiscriminator: "class")]
[JsonDerivedType(typeof(ArrayType), typeDiscriminator: "array")]
public abstract class BytecodeType
{
    public int TypeId { get; internal set; }
}
