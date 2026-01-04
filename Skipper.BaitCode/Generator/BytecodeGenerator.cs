using Skipper.Lexer.Tokens;
using Skipper.Parser.AST;
using Skipper.Parser.AST.Declarations;
using Skipper.Parser.AST.Expressions;
using Skipper.Parser.AST.Statements;
using Skipper.Parser.Visitor;
using Skipper.BaitCode.IdManager;
using Skipper.BaitCode.Objects;
using Skipper.BaitCode.Objects.Instructions;
using Skipper.BaitCode.Types;

namespace Skipper.BaitCode.Generator;

public class BytecodeGenerator : IAstVisitor<BytecodeGenerator>
{
    private readonly BytecodeProgram _program = new();

    private BytecodeFunction? _currentFunction;
    private readonly Stack<LocalSlotManager> _locals = new();
    private readonly Dictionary<string, BytecodeType> _resolvedTypes = new();
    private readonly Dictionary<string, PrimitiveType> _primitiveTypes = new();

    public BytecodeProgram Generate(ProgramNode program)
    {
        VisitProgram(program);

        return _program;
    }

    // Добавляет операцию в текущую обрабатываемую функцию
    private void Emit(OpCode opCode, params object[] operands)
    {
        if (_currentFunction == null)
            throw new InvalidOperationException("No function declared in scope");

        _currentFunction.Code.Add(new Instruction(opCode, operands));
    }

    // Обход начиная с результата работы парсера AST (корневой узел)
    public BytecodeGenerator VisitProgram(ProgramNode node)
    {
        foreach (var decl in node.Declarations)
        {
            decl.Accept(this);
        }
        return this;
    }

    private LocalSlotManager Locals => _locals.Peek();
    private void EnterScope() => Locals.EnterScope();
    private void ExitScope() => Locals.ExitScope();

    // --- Declarations ---
    public BytecodeGenerator VisitFunctionDeclaration(FunctionDeclaration node)
    {
        var function = new BytecodeFunction(
            id: _program.Functions.Count,
            name: node.Name,
            returnType: ResolveType(node.ReturnType),
            parameters: node.Parameters
                .Select(p => (p.Name, ResolveType(p.TypeName)))
                .ToList()
        );
        
        _program.Functions.Add(function);
        _currentFunction = function;
        
        _locals.Push(new LocalSlotManager());
        EnterScope();
        
        foreach (var param in node.Parameters)
            param.Accept(this);

        node.Body.Accept(this);

        ExitScope();
        _locals.Pop();

        _currentFunction = null;

        return this;
    }

    public BytecodeGenerator VisitParameterDeclaration(ParameterDeclaration node)
    {
        Locals.Declare(node.Name);
        return this;
    }

    public BytecodeGenerator VisitVariableDeclaration(VariableDeclaration node) // TODO: посмотреть на ResolveType
    {
        var slot = Locals.Declare(node.Name);

        if (node.Initializer != null)
        {
            node.Initializer.Accept(this);
            Emit(OpCode.STORE, slot);
        }

        return this;
    }

    public BytecodeGenerator VisitClassDeclaration(ClassDeclaration node) // TODO
    {
        return this;
    }

    // --- Statements ---
    public BytecodeGenerator VisitBlockStatement(BlockStatement node)
    {
        EnterScope();

        foreach (var stmt in node.Statements)
            stmt.Accept(this);

        ExitScope();
        return this;
    }

    public BytecodeGenerator VisitExpressionStatement(ExpressionStatement node)
    {
        node.Expression.Accept(this);
        Emit(OpCode.POP);
        return this;
    }

    public BytecodeGenerator VisitReturnStatement(ReturnStatement node)
    {
        node.Value?.Accept(this);
        Emit(OpCode.RETURN);
        return this;
    }

    public BytecodeGenerator VisitIfStatement(IfStatement node) // TODO: EmitPlaceholder + Patch
    {
        node.Condition.Accept(this);

        var jumpFalse = EmitPlaceholder(OpCode.JUMP_IF_FALSE);

        node.ThenBranch.Accept(this);

        if (node.ElseBranch != null)
        {
            var jumpEnd = EmitPlaceholder(OpCode.JUMP);
            Patch(jumpFalse);
            node.ElseBranch.Accept(this);
            Patch(jumpEnd);
        }
        else
        {
            Patch(jumpFalse);
        }

        return this;
    }

    private int EmitPlaceholder(OpCode opCode)
    {
        if (_currentFunction == null)
            throw new NullReferenceException("No function declared in scope");

        var placeholderIndex = _currentFunction.Code.Count;
        Emit(opCode, 0); // 0 будет заменено позже
        return placeholderIndex;
    }

    private void Patch(int instructionIndex)
    {
        if (_currentFunction == null)
            throw new NullReferenceException("No function declared in scope");
        
        var instr = _currentFunction.Code[instructionIndex];
        _currentFunction.Code[instructionIndex] = new Instruction(
            instr.OpCode,
            _currentFunction.Code.Count
        );
    }

    public BytecodeGenerator VisitWhileStatement(WhileStatement node)
    {
        return this;
    }

    public BytecodeGenerator VisitForStatement(ForStatement node) // TODO
    {
        return this;
    }

    // --- Expressions ---
    public BytecodeGenerator VisitBinaryExpression(BinaryExpression node)
    {
        // Присваивание
        if (node.Operator.Type == TokenType.ASSIGN)
        {
            node.Right.Accept(this);
            Emit(OpCode.DUP);
            EmitStore(node.Left);

            return this;
        }
        
        node.Left.Accept(this);
        node.Right.Accept(this);
    
        // Для остальных операций
        switch (node.Operator.Type)
        {
            case TokenType.PLUS: Emit(OpCode.ADD); break;
            case TokenType.MINUS: Emit(OpCode.SUB); break;
            case TokenType.STAR: Emit(OpCode.MUL); break;
            case TokenType.SLASH: Emit(OpCode.DIV); break;
            case TokenType.MODULO: Emit(OpCode.MOD); break;
            case TokenType.EQUAL: Emit(OpCode.CMP_EQ); break;
            case TokenType.NOT_EQUAL: Emit(OpCode.CMP_NE); break;
            case TokenType.LESS: Emit(OpCode.CMP_LT); break;
            case TokenType.GREATER: Emit(OpCode.CMP_GT); break;
            case TokenType.LESS_EQUAL: Emit(OpCode.CMP_LE); break;
            case TokenType.GREATER_EQUAL: Emit(OpCode.CMP_GE); break;
            case TokenType.AND: Emit(OpCode.AND); break;
            case TokenType.OR: Emit(OpCode.OR); break;
            default:
                throw new NotSupportedException($"Operator {node.Operator.Type} not supported");
        }
    
        return this;
    }
    
    private void EmitStore(Expression target)
    {
        switch (target)
        {
            // x = value
            case IdentifierExpression id:
            {
                var slot = Locals.Resolve(id.Name);
                Emit(OpCode.STORE, slot);
                break;
            }

            // obj.field = value
            case MemberAccessExpression ma:
            {
                // stack: value
                // нужно: object, value

                // вычисляем объект
                ma.Object.Accept(this);

                // stack: value, object
                Emit(OpCode.SWAP);
                // stack: object, value

                var field = ResolveField(ma);
                Emit(OpCode.SET_FIELD, field.FieldId);
                break;
            }

            // arr[index] = value
            case ArrayAccessExpression aa:
            {
                // stack: value

                aa.Target.Accept(this); // array
                aa.Index.Accept(this);  // index

                // stack: value, array, index
                Emit(OpCode.SWAP);      // value <-> index
                // stack: index, array, value
                Emit(OpCode.SWAP);      // index <-> array
                // stack: array, index, value

                Emit(OpCode.SET_ELEMENT);
                break;
            }

            default:
                throw new InvalidOperationException(
                    $"Expression '{target.NodeType}' cannot be assigned to");
        }
    }

    public BytecodeGenerator VisitUnaryExpression(UnaryExpression node) // TODO
    {
        return this;
    }

    public BytecodeGenerator VisitLiteralExpression(LiteralExpression node)
    {
        int constId = AddConstant(node.Value);
        Emit(OpCode.PUSH, constId);
        return this;
    }

    public BytecodeGenerator VisitIdentifierExpression(IdentifierExpression node) // TODO: проверить взаимосвязь с Visit Variable Declaration
    {
        var slot = Locals.Resolve(node.Name);
        Emit(OpCode.LOAD, slot);
        return this;
    }

    public BytecodeGenerator VisitCallExpression(CallExpression node)
    {
        foreach (var arg in node.Arguments)
            arg.Accept(this);

        if (node.Callee is IdentifierExpression id)
        {
            var functionId = ResolveFunction(id.Name);
            Emit(OpCode.CALL, functionId);
        }

        return this;
    }

    public BytecodeGenerator VisitTernaryExpression(TernaryExpression node) // TODO
    {
        return this;
    }

    public BytecodeGenerator VisitArrayAccessExpression(ArrayAccessExpression node) // TODO
    {
        return this;
    }

    public BytecodeGenerator VisitMemberAccessExpression(MemberAccessExpression node) // TODO
    {
        return this;
    }

    public BytecodeGenerator VisitNewArrayExpression(NewArrayExpression node) // TODO
    {
        return this;
    }

    public BytecodeGenerator VisitNewObjectExpression(NewObjectExpression node) // TODO
    {
        return this;
    }

    
    
    // Разрешение типа
    private BytecodeType ResolveType(string typeName)
    {
        if (_resolvedTypes.TryGetValue(typeName, out var cached))
            return cached;

        BytecodeType result;

        // Массив
        if (typeName.EndsWith("[]"))
        {
            var elementName = typeName[..^2];
            var elementType = ResolveType(elementName);

            result = new ArrayType(elementType);
        }
        // Другие примитивы
        else
        {
            result = typeName switch
            {
                "int"    => GetOrCreatePrimitive("int"),
                "double" => GetOrCreatePrimitive("double"),
                "bool"   => GetOrCreatePrimitive("bool"),
                "char"   => GetOrCreatePrimitive("char"),
                "string" => GetOrCreatePrimitive("string"),
                "void"   => GetOrCreatePrimitive("void"),
                _        => ResolveClassType(typeName)
            };
        }

        // Регистрация типа в Program
        result.TypeId = _program.Types.Count;
        _program.Types.Add(result);
        _resolvedTypes[typeName] = result;

        return result;
    }
    
    private PrimitiveType GetOrCreatePrimitive(string name)
    {
        if (_primitiveTypes.TryGetValue(name, out var t))
            return t;

        var type = new PrimitiveType(name);
        _primitiveTypes[name] = type;
        return type;
    }

    private BytecodeType ResolveClassType(string name)
    {
        var cls = _program.Classes.FirstOrDefault(c => c.Name == name);
        return cls == null ?
            throw new InvalidOperationException($"Unknown class type '{name}'")
            : new ClassType(cls.ClassId, cls.Name);
    }

    private int ResolveFunction(string name)
    {
        var func = _program.Functions.FirstOrDefault(f => f.Name == name);
        if (func == null)
            throw new InvalidOperationException($"Function '{name}' not found");
        return func.FunctionId;
    }

    private int AddConstant(object value)
    {
        _program.ConstantPool.Add(value);
        return _program.ConstantPool.Count - 1;
    }
}
