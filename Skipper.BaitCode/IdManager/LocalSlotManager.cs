using Skipper.BaitCode.Objects;
using Skipper.BaitCode.Types;

namespace Skipper.BaitCode.IdManager;

/// <summary>
/// Управление слотами для локальных переменных и параметров
/// </summary>

public sealed class LocalSlotManager(BytecodeFunction function)
{
    private readonly Stack<Dictionary<string, int>> _scopes = new();

    public void EnterScope()
    {
        _scopes.Push(new Dictionary<string, int>());
    }

    public void ExitScope()
    {
        _scopes.Pop();
    }

    public int Declare(string name, BytecodeType type)
    {
        var scope = _scopes.Peek();

        if (scope.ContainsKey(name))
            throw new InvalidOperationException($"Variable '{name}' already declared in this scope");

        var slot = function.Locals.Count;

        function.Locals.Add(
            new BytecodeVariable(slot, name, type)
        );

        scope[name] = slot;
        return slot;
    }

    public bool TryResolve(string name, out int slot)
    {
        foreach (var scope in _scopes)
            if (scope.TryGetValue(name, out slot))
                return true;

        slot = -1;
        return false;
    }
}