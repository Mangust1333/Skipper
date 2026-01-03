using Skipper.Lexer.Tokens;
using Skipper.Parser.AST;
using Skipper.Parser.AST.Declarations;
using Skipper.Parser.AST.Expressions;
using Skipper.Parser.AST.Statements;
using Skipper.Parser.Visitor;
using Skipper.BaitCode.IdManager;
using Skipper.BaitCode.Objects;

namespace Skipper.BaitCode.Generator;

public class BytecodeGenerator : IAstVisitor<BytecodeGenerator>
{
    private readonly BytecodeProgram _program = new();

    private BytecodeFunction? _currentFunction;
    private readonly Stack<LocalSlotManager> _locals = new();

    // ---------- ENTRY ----------
    public BytecodeProgram Generate(ProgramNode program)
    {
        // ПРОХОД 1: регистрация
        RegisterDeclarations(program);

        // ПРОХОД 2: код
        program.Accept(this);

        return _program;
    }

    // ---------- PASS 1 ----------
    private void RegisterDeclarations(ProgramNode program)
    {
        foreach (var decl in program.Declarations)
        {
            if (decl is FunctionDeclaration fn)
            {
                _program.RegisterFunction(fn.Name);
            }
            else if (decl is ClassDeclaration cls)
            {
                _program.RegisterClass(cls.Name);
            }
        }
    }

    private void Emit(OpCode opCode, int operand = 0)
    {
        _currentFunction.Code.Add(new Instruction(opCode, operand));
    }

    // --- Root ---
    public BytecodeGenerator VisitProgram(ProgramNode node)
    {
        foreach (var decl in node.Declarations)
        {
            decl.Accept(this);
        }
        return this;
    }

    // --- Declarations ---
    public BytecodeGenerator VisitFunctionDeclaration(FunctionDeclaration node)
    {
        _locals.Reset();
        _currentFunction = new BytecodeFunction(node.Name);
        _program.Functions.Add(_currentFunction);

        // Регистрируем параметры в слотах
        foreach (var param in node.Parameters)
        {
            param.Accept(this);
        }

        node.Body.Accept(this);

        Emit(OpCode.RETURN);
        return this;
    }

    public BytecodeGenerator VisitParameterDeclaration(ParameterDeclaration node)
    {
        _locals.Declare(node.Name);
        return this;
    }

    public BytecodeGenerator VisitVariableDeclaration(VariableDeclaration node)
    {
        int slot = _locals.Declare(node.Name);
        if (node.Initializer != null)
        {
            node.Initializer.Accept(this);
            Emit(OpCode.STORE_LOCAL, slot);
        }
        return this;
    }

    public BytecodeGenerator VisitClassDeclaration(ClassDeclaration node)
    {
        var classNameIndex = _program.ConstantPool.IndexOf(node.Name);
        if (classNameIndex < 0)
        {
            classNameIndex = _program.ConstantPool.Count;
            _program.ConstantPool.Add(node.Name);
        }

        var bytecodeClass = new BytecodeClass(node.Name, classNameIndex);

        foreach (var member in node.Members)
        {
            switch (member)
            {
                case VariableDeclaration field:
                {
                    var fieldSlot = bytecodeClass.Fields.Count;
                    bytecodeClass.Fields.Add(field.Name, fieldSlot);
                    break;
                }

                case FunctionDeclaration method:
                {
                    var methodId = _program.Functions.Count;

                    bytecodeClass.Methods.Add(method.Name, methodId);

                    VisitFunctionDeclaration(method);
                    break;
                }

                default:
                    throw new NotSupportedException(
                        $"Член класса не поддерживается: {member.GetType().Name}");
            }
        }

        _program.Classes.Add(bytecodeClass);

        return this;
    }

    // --- Statements ---
    public BytecodeGenerator VisitBlockStatement(BlockStatement node)
    {
        foreach (var stmt in node.Statements)
        {
            stmt.Accept(this);
        }
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
        if (node.Value != null)
        {
            node.Value.Accept(this);
        }
        Emit(OpCode.RETURN);
        return this;
    }

    public BytecodeGenerator VisitIfStatement(IfStatement node)
    {
        node.Condition.Accept(this);

        var jumpIfFalsePos = _currentFunction.Code.Count;
        Emit(OpCode.JUMP_IF_FALSE, 0);

        node.ThenBranch.Accept(this);

        var jumpEndPos = _currentFunction.Code.Count;
        Emit(OpCode.JUMP, 0);

        _currentFunction.Code[jumpIfFalsePos] = new Instruction(OpCode.JUMP_IF_FALSE, _currentFunction.Code.Count);

        node.ElseBranch?.Accept(this);

        _currentFunction.Code[jumpEndPos] = new Instruction(OpCode.JUMP, _currentFunction.Code.Count);

        return this;
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

    public BytecodeGenerator VisitForStatement(ForStatement node)
    {
        node.Initializer?.Accept(this);

        var loopStart = _currentFunction.Code.Count;

        node.Condition?.Accept(this);

        int exitJump = _currentFunction.Code.Count;
        Emit(OpCode.JUMP_IF_FALSE, 0);

        node.Body.Accept(this);

        if (node.Increment != null)
        {
            node.Increment.Accept(this);
            Emit(OpCode.POP);
        }

        Emit(OpCode.JUMP, loopStart);

        _currentFunction.Code[exitJump] = new Instruction(OpCode.JUMP_IF_FALSE, _currentFunction.Code.Count);

        return this;
    }

    // --- Expressions ---
    public BytecodeGenerator VisitBinaryExpression(BinaryExpression node)
    {
        if (node.Operator.Type == TokenType.ASSIGN)
        {
            // 1. RHS
            node.Right.Accept(this);

            // 2. LHS
            switch (node.Left)
            {
                case IdentifierExpression id:
                {
                    var slot = _locals.GetSlot(id.Name);
                    Emit(OpCode.STORE_LOCAL, slot);
                    break;
                }

                case MemberAccessExpression member:
                {
                    member.Object.Accept(this);

                    int fieldSlot = ResolveFieldSlot(member);
                    Emit(OpCode.STORE_FIELD, fieldSlot);
                    break;
                }

                case ArrayAccessExpression array:
                {
                    array.Target.Accept(this);
                    array.Index.Accept(this);
                    Emit(OpCode.STORE_ELEMENT);
                    break;
                }

                default:
                    throw new InvalidOperationException("Invalid assignment target");
            }

            return this;
        }

        // обычные бинарные выражения
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

    public BytecodeGenerator VisitUnaryExpression(UnaryExpression node)
    {
        node.Operand.Accept(this);

        switch (node.Operator.Type)
        {
            case TokenType.MINUS: Emit(OpCode.NEG_INT); break;
            case TokenType.NOT: Emit(OpCode.NOT_BOOL); break;
            default: throw new NotSupportedException($"Unary operator {node.Operator.Type}");
        }

        return this;
    }

    public BytecodeGenerator VisitLiteralExpression(LiteralExpression node)
    {
        object value;

        if (node.Token != null)
        {
            if (node.Token.IsLiteral)
            {
                if (node.Token.Type == TokenType.BOOL_LITERAL)
                    value = node.Token.GetBoolValue();
                else if (node.Token.Type == TokenType.NUMBER || node.Token.Type == TokenType.FLOAT_LITERAL)
                    value = node.Token.GetNumericValue();
                else
                    value = node.Token.GetStringValue();
            }
            else
            {
                value = node.Value;
            }
        }
        else
        {
            value = node.Value;
        }

        int index = _program.ConstantPool.Count;
        _program.ConstantPool.Add(value);

        Emit(OpCode.PUSH_CONST, index);
        return this;
    }

    public BytecodeGenerator VisitIdentifierExpression(IdentifierExpression node)
    {
        var slot = _locals.GetSlot(node.Name);
        Emit(OpCode.LOAD_LOCAL, slot);
        return this;
    }

    public BytecodeGenerator VisitCallExpression(CallExpression node)
    {
        foreach (var arg in node.Arguments)
        {
            arg.Accept(this);
        }

        var funcIndex = _program.Functions.FindIndex(
            f => f.Name == ((IdentifierExpression)node.Callee).Name);
        Emit(OpCode.CALL, funcIndex);
        return this;
    }

    public BytecodeGenerator VisitTernaryExpression(TernaryExpression node)
    {
        node.Condition.Accept(this);

        var falseJump = _currentFunction.Code.Count;
        Emit(OpCode.JUMP_IF_FALSE, 0);

        node.ThenBranch.Accept(this);

        var endJump = _currentFunction.Code.Count;
        Emit(OpCode.JUMP, 0);

        _currentFunction.Code[falseJump] = new Instruction(OpCode.JUMP_IF_FALSE, _currentFunction.Code.Count);

        node.ElseBranch.Accept(this);

        _currentFunction.Code[endJump] = new Instruction(OpCode.JUMP, _currentFunction.Code.Count);

        return this;
    }

    public BytecodeGenerator VisitArrayAccessExpression(ArrayAccessExpression node)
    {
        node.Target.Accept(this);
        node.Index.Accept(this);
        Emit(OpCode.LOAD_ELEMENT);
        return this;
    }

    public BytecodeGenerator VisitMemberAccessExpression(MemberAccessExpression node)
    {
        node.Object.Accept(this);

        int fieldIndex = _program.ConstantPool.IndexOf(node.MemberName);
        if (fieldIndex < 0)
        {
            fieldIndex = _program.ConstantPool.Count;
            _program.ConstantPool.Add(node.MemberName);
        }

        Emit(OpCode.LOAD_FIELD, fieldIndex);
        return this;
    }

    public BytecodeGenerator VisitNewArrayExpression(NewArrayExpression node)
    {
        node.SizeExpression.Accept(this);

        int typeIndex = _program.ConstantPool.IndexOf(node.ElementType);
        if (typeIndex < 0)
        {
            typeIndex = _program.ConstantPool.Count;
            _program.ConstantPool.Add(node.ElementType);
        }

        Emit(OpCode.NEW_ARRAY, typeIndex);
        return this;
    }

    public BytecodeGenerator VisitNewObjectExpression(NewObjectExpression node)
    {
        foreach (var arg in node.Arguments)
        {
            arg.Accept(this);
        }

        int classIndex = _program.ConstantPool.IndexOf(node.ClassName);
        if (classIndex < 0)
        {
            classIndex = _program.ConstantPool.Count;
            _program.ConstantPool.Add(node.ClassName);
        }

        Emit(OpCode.NEW_OBJECT, classIndex);
        return this;
    }
}
