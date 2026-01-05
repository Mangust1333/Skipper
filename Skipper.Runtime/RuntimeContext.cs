using Skipper.Runtime.Abstractions;
using Skipper.Runtime.GC;
using Skipper.Runtime.Memory;
using Skipper.Runtime.Objects;
using Skipper.Runtime.Values;

namespace Skipper.Runtime;

public sealed class RuntimeContext
{
    private readonly Heap _heap;
    private readonly IGarbageCollector _gc;

    // Размер заголовка 
    // Размер заголовка объекта/массива в байтах (метаданные или длина)
    private const int HeaderSize = sizeof(long);

    // Размер одного слота значения (8 байт)
    private const int SlotSize = sizeof(long);

    public RuntimeContext()
    {
        _heap = new Heap(1024 * 1024); // 1 MB для тестов
        _gc = new MarkSweepGc(_heap);
    }

    // --- Управление памятью ---

    public bool CanAllocate(int bytes)
    {
        return _heap.HasSpace(bytes + HeaderSize);
    }

    public void Collect(IRootProvider roots)
    {
        _gc.Collect(roots);
    }

    // --- Аллокация ---

    public nint AllocateObject(int payloadSize, int classId)
    {
        ObjectDescriptor desc = new(ObjectKind.Class, null);

        // Выделяем память: Заголовок + Поля
        var ptr = _heap.Allocate(desc, HeaderSize + payloadSize);

        // В заголовок записываем ClassId
        _heap.WriteInt64(ptr, 0, classId);

        return ptr;
    }

    public nint AllocateArray(int length)
    {
        var totalSize = HeaderSize + (length * SlotSize);
        ObjectDescriptor desc = new(ObjectKind.Array, null);

        var ptr = _heap.Allocate(desc, totalSize);

        // В заголовок записываем длину массива
        _heap.WriteInt64(ptr, 0, length);

        return ptr;
    }

    // --- Доступ к полям объектов ---

    public Value ReadField(nint objPtr, int fieldIndex)
    {
        var offset = HeaderSize + (fieldIndex * SlotSize);
        var raw = _heap.ReadInt64(objPtr, offset);
        return new Value(raw);
    }

    public void WriteField(nint objPtr, int fieldIndex, Value val)
    {
        var offset = HeaderSize + (fieldIndex * SlotSize);
        _heap.WriteInt64(objPtr, offset, val.Raw);
    }

    // --- Доступ к массивам ---

    public int GetArrayLength(nint arrPtr)
    {
        return (int)_heap.ReadInt64(arrPtr, 0);
    }

    public Value ReadArrayElement(nint arrPtr, int index)
    {
        var length = GetArrayLength(arrPtr);

        if (index < 0 || index >= length)
        {
            throw new IndexOutOfRangeException($"Array index {index} is out of bounds (Length: {length})");
        }

        var offset = HeaderSize + (index * SlotSize);
        var raw = _heap.ReadInt64(arrPtr, offset);
        return new Value(raw);
    }

    public void WriteArrayElement(nint arrPtr, int index, Value val)
    {
        var length = GetArrayLength(arrPtr);

        if (index < 0 || index >= length)
        {
            throw new IndexOutOfRangeException($"Array index {index} is out of bounds (Length: {length})");
        }

        var offset = HeaderSize + (index * SlotSize);
        _heap.WriteInt64(arrPtr, offset, val.Raw);
    }
}