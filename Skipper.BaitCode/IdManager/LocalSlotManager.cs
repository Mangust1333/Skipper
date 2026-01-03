namespace Skipper.BaitCode.IdManager;

/// <summary>
/// Управление слотами для локальных переменных и параметров
/// </summary>
public class LocalSlotManager
{
    private readonly Dictionary<string, int> _locals = new();
    private int _nextSlot = 0;

    public int Declare(string name)
    {
        if (_locals.ContainsKey(name))
        {
            throw new InvalidOperationException($"Локальная переменная '{name}' уже объявлена");
        }

        int slot = _nextSlot;
        _locals[name] = slot;
        _nextSlot++;
        return slot;
    }

    public int GetSlot(string name)
    {
        if (!_locals.TryGetValue(name, out var slot))
        {
            throw new InvalidOperationException($"Локальная переменная '{name}' не найдена");
        }
        return slot;
    }

    public void Reset()
    {
        _locals.Clear();
        _nextSlot = 0;
    }
}