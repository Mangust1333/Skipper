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
            Emit(OpCode.STORE_LOCAL, slot);
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

        int jumpFalse = EmitPlaceholder(OpCode.JUMP_IF_FALSE);

        node.ThenBranch.Accept(this);

        if (node.ElseBranch != null)
        {
            int jumpEnd = EmitPlaceholder(OpCode.JUMP);
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

    private int EmitPlaceholder(OpCode opCode) // TODO
    {
        return 0;
    }

    private void Patch(int jumpPos) // TODO;
    {
        return;
    }

    public BytecodeGenerator VisitWhileStatement(WhileStatement node)
    {
        var loopStart = _currentFunction.Code.Count;

        node.Condition.Accept(this);

        var exitJump = _currentFunction.Code.Count;
        Emit(OpCode.JUMP_IF_FALSE, 0);

        node.Body.Accept(this);

        Emit(OpCode.JUMP, loopStart);

        _currentFunction.Code[exitJump] = new Instruction(OpCode.JUMP_IF_FALSE, _currentFunction.Code.Count);

        return this;
    }

    public BytecodeGenerator VisitForStatement(ForStatement node) // TODO
    {
        return this;
    }

    // --- Expressions ---
    public BytecodeGenerator VisitBinaryExpression(BinaryExpression node) // TODO: Складываются не только инты, что делать с массивами, double и т.д.
    {
        node.Left.Accept(this);
        node.Right.Accept(this);

        switch (node.Operator.Type)
        {
            case TokenType.PLUS: Emit(OpCode.ADD_INT); break;
            case TokenType.MINUS: Emit(OpCode.SUB_INT); break;
            case TokenType.STAR: Emit(OpCode.MUL_INT); break;
            case TokenType.SLASH: Emit(OpCode.DIV_INT); break;
            case TokenType.EQUAL: Emit(OpCode.CMP_EQ); break;
            case TokenType.NOT_EQUAL: Emit(OpCode.CMP_NE); break;
            case TokenType.LESS: Emit(OpCode.CMP_LT); break;
            case TokenType.GREATER: Emit(OpCode.CMP_GT); break;
            default:
                throw new NotSupportedException(node.Operator.Type.ToString());
        }

        return this;
    }

    public BytecodeGenerator VisitUnaryExpression(UnaryExpression node) // TODO
    {
        return this;
    }

    public BytecodeGenerator VisitLiteralExpression(LiteralExpression node) // TODO: AddConstant
    {
        int constId = AddConstant(node.Value);
        Emit(OpCode.PUSH_CONST, constId);
        return this;
    }

    public BytecodeGenerator VisitIdentifierExpression(IdentifierExpression node) // TODO: проверить взаимосвязь с Visit Variable Declaration
    {
        var slot = Locals.Resolve(node.Name);
        Emit(OpCode.LOAD_LOCAL, slot);
        return this;
    }

    public BytecodeGenerator VisitCallExpression(CallExpression node) // TODO: ResolveFunction
    {
        foreach (var arg in node.Arguments)
            arg.Accept(this);

        if (node.Callee is IdentifierExpression id)
        {
            int fnId = ResolveFunction(id.Name);
            Emit(OpCode.CALL_FUNCTION, fnId);
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

    private int ResolveFunction(string name) // TODO
    {
        return 0;
    }

    private int AddConstant(object name) // TODO
    {
        return 0;
    }
}
