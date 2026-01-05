using Skipper.Runtime.Objects;
using Skipper.Runtime.Values;
using Xunit;

namespace Skipper.Runtime.Tests;

public unsafe class GcTests
{
    private readonly TestRootProvider _rootProvider = new();

    [Fact]
    public void SingleObject_IsCollected_WhenNoRoots()
    {
        var rt = new RuntimeContext();

        var desc = new ObjectDescriptor(
            ObjectKind.Class,
            []
        );

        rt.Heap.Allocate(desc, 16);

        rt.Collect(_rootProvider);

        Assert.Empty(rt.Heap.Objects);
    }

    [Fact]
    public void RootObject_IsNotCollected()
    {
        var rt = new RuntimeContext();

        var desc = new ObjectDescriptor(
            ObjectKind.Class,
            []
        );

        var a = rt.Heap.Allocate(desc, 16);
        _rootProvider.Add(Value.FromObject(a));

        rt.Collect(_rootProvider);

        Assert.Single(rt.Heap.Objects);
    }

    [Fact]
    public void ChainReferences_AreKeptAlive()
    {
        var rt = new RuntimeContext();

        var desc = new ObjectDescriptor(
            ObjectKind.Class,
            [0]
        );

        var a = rt.Heap.Allocate(desc, sizeof(nint));
        var b = rt.Heap.Allocate(desc, sizeof(nint));
        var c = rt.Heap.Allocate(desc, sizeof(nint));

        WritePtr(a, b);
        WritePtr(b, c);

        _rootProvider.Add(Value.FromObject(a));
        rt.Collect(_rootProvider);

        Assert.Equal(3, rt.Heap.Objects.Count());
    }

    [Fact]
    public void UnreachableObject_IsCollected()
    {
        var rt = new RuntimeContext();

        var desc = new ObjectDescriptor(
            ObjectKind.Class,
            [0]
        );

        var a = rt.Heap.Allocate(desc, sizeof(nint));
        var b = rt.Heap.Allocate(desc, sizeof(nint));

        WritePtr(a, b);

        _rootProvider.Add(Value.FromObject(a));
        rt.Collect(_rootProvider);

        WritePtr(a, 0);
        rt.Collect(_rootProvider);

        Assert.Single(rt.Heap.Objects);
    }

    [Fact]
    public void CyclicReferences_WithoutRoots_AreCollected()
    {
        var rt = new RuntimeContext();

        var desc = new ObjectDescriptor(
            ObjectKind.Class,
            [0]
        );

        var a = rt.Heap.Allocate(desc, sizeof(nint));
        var b = rt.Heap.Allocate(desc, sizeof(nint));

        WritePtr(a, b);
        WritePtr(b, a);

        rt.Collect(_rootProvider);

        Assert.Empty(rt.Heap.Objects);
    }

    [Fact]
    public void CyclicReferences_WithRoot_AreKeptAlive()
    {
        var rt = new RuntimeContext();

        var desc = new ObjectDescriptor(
            ObjectKind.Class,
            [0]
        );

        var a = rt.Heap.Allocate(desc, sizeof(nint));
        var b = rt.Heap.Allocate(desc, sizeof(nint));

        WritePtr(a, b);
        WritePtr(b, a);

        _rootProvider.Add(Value.FromObject(a));
        rt.Collect(_rootProvider);

        Assert.Equal(2, rt.Heap.Objects.Count());
    }

    [Fact]
    public void PartialGraph_IsCorrectlyCollected()
    {
        var rt = new RuntimeContext();

        var desc = new ObjectDescriptor(
            ObjectKind.Class,
            [0]
        );

        var a = rt.Heap.Allocate(desc, sizeof(nint));
        var b = rt.Heap.Allocate(desc, sizeof(nint));
        rt.Heap.Allocate(desc, sizeof(nint));

        WritePtr(a, b);

        _rootProvider.Add(Value.FromObject(a));
        rt.Collect(_rootProvider);

        Assert.Equal(2, rt.Heap.Objects.Count());
    }

    [Fact]
    public void MultipleRoots_AreAllRespected()
    {
        var rt = new RuntimeContext();

        var desc = new ObjectDescriptor(
            ObjectKind.Class,
            [0]
        );

        var a = rt.Heap.Allocate(desc, sizeof(nint));
        var b = rt.Heap.Allocate(desc, sizeof(nint));
        var c = rt.Heap.Allocate(desc, sizeof(nint));

        WritePtr(a, c);

        _rootProvider.Add(Value.FromObject(a));
        _rootProvider.Add(Value.FromObject(b));

        rt.Collect(_rootProvider);

        Assert.Equal(3, rt.Heap.Objects.Count());
    }

    [Fact]
    public void MultipleCollections_WorkCorrectly()
    {
        var rt = new RuntimeContext();

        var desc = new ObjectDescriptor(
            ObjectKind.Class,
            []
        );

        var a = rt.Heap.Allocate(desc, 16);
        _rootProvider.Add(Value.FromObject(a));

        rt.Collect(_rootProvider);
        rt.Collect(_rootProvider);
        rt.Collect(_rootProvider);

        Assert.Single(rt.Heap.Objects);
    }

    [Fact]
    public void NonObjectValues_AreIgnoredByGC()
    {
        var rt = new RuntimeContext();

        var desc = new ObjectDescriptor(
            ObjectKind.Class,
            []
        );

        rt.Heap.Allocate(desc, 16);

        _rootProvider.Add(Value.FromInt(123));
        _rootProvider.Add(Value.FromBool(true));

        rt.Collect(_rootProvider);

        Assert.Empty(rt.Heap.Objects);
    }

    private static void WritePtr(nint target, nint value)
    {
        *(nint*)target = value;
    }

    [Fact]
    public void MixedLayout_GcFollowsOnlyDescriptors()
    {
        // Проверка, что GC не путает обычные числа (Int) с указателями
        var rt = new RuntimeContext();

        // Структура объекта: [Int (8 bytes), Ref (8 bytes)]
        // GC должен смотреть только на смещение 8
        var desc = new ObjectDescriptor(
            ObjectKind.Class,
            [sizeof(long)] // Смещение 8
        );

        var parent = rt.Heap.Allocate(desc, 16);
        var child = rt.Heap.Allocate(desc, 16);

        // Записываем "фейковый" указатель в первое поле (это просто число)
        // Если GC попытается прочитать это как указатель, он пойдет искать объект по адресу 12345 и может упасть или ничего не найти
        *(long*)parent = 12345;

        // Записываем реальную ссылку во второе поле (смещение 8)
        *(nint*)(parent + sizeof(long)) = child;

        _rootProvider.Add(Value.FromObject(parent));
        rt.Collect(_rootProvider);

        // Должны выжить оба: parent (root) и child (по ссылке со смещением 8)
        Assert.Equal(2, rt.Heap.Objects.Count());

        // Проверяем, что число не испортилось
        Assert.Equal(12345, *(long*)parent);
    }

    [Fact]
    public void SelfReference_IsKeptAlive()
    {
        var rt = new RuntimeContext();
        var desc = new ObjectDescriptor(ObjectKind.Class, [0]);

        var obj = rt.Heap.Allocate(desc, sizeof(nint));

        // Объект ссылается сам на себя
        WritePtr(obj, obj);

        _rootProvider.Add(Value.FromObject(obj));
        rt.Collect(_rootProvider);

        Assert.Single(rt.Heap.Objects);
    }

    [Fact]
    public void InvalidPointer_InRoots_DoesNotCrash()
    {
        // Если в руты попал мусор (например, неинициализированная переменная VM)
        var rt = new RuntimeContext();

        // Этот адрес точно не существует в куче
        nint invalidPtr = (nint)0xDEADBEEF;

        _rootProvider.Add(Value.FromObject(invalidPtr));

        // GC должен просто проигнорировать этот адрес, так как Heap.FindObject вернет null
        var exception = Record.Exception(() => rt.Collect(_rootProvider));

        Assert.Null(exception);
    }

    [Fact]
    public void ObjectWithNullReference_DoesNotCrash()
    {
        var rt = new RuntimeContext();
        // Дескриптор говорит, что по смещению 0 лежит ссылка
        var desc = new ObjectDescriptor(ObjectKind.Class, [0]);

        var obj = rt.Heap.Allocate(desc, sizeof(nint));

        // Явно записываем 0 (null) в поле ссылки
        WritePtr(obj, 0);

        _rootProvider.Add(Value.FromObject(obj));

        // GC должен пройти маркировку obj, увидеть ссылку 0, проигнорировать её и завершить работу без ошибок
        var exception = Record.Exception(() => rt.Collect(_rootProvider));

        Assert.Null(exception);
        Assert.Single(rt.Heap.Objects);
    }

    [Fact]
    public void DiamondGraph_IsKeptAlive()
    {
        // Структура:
        //    A
        //   / \
        //  B   C
        //   \ /
        //    D

        var rt = new RuntimeContext();
        var desc = new ObjectDescriptor(ObjectKind.Class, [0, sizeof(nint)]); // Две ссылки

        var a = rt.Heap.Allocate(desc, sizeof(nint) * 2);
        var b = rt.Heap.Allocate(desc, sizeof(nint) * 2);
        var c = rt.Heap.Allocate(desc, sizeof(nint) * 2);
        var d = rt.Heap.Allocate(desc, sizeof(nint) * 2);

        // A -> B, C
        WritePtr(a, b);
        WritePtr(a + sizeof(nint), c);

        // B -> D
        WritePtr(b, d);

        // C -> D
        WritePtr(c, d);

        _rootProvider.Add(Value.FromObject(a));
        rt.Collect(_rootProvider);

        Assert.Equal(4, rt.Heap.Objects.Count());
    }

    [Fact]
    public void DeepLinkedList_DoesNotCauseStackOverflow()
    {
        var rt = new RuntimeContext();
        var desc = new ObjectDescriptor(ObjectKind.Class, [0]); // Ссылка на следующий элемент

        nint head = rt.Heap.Allocate(desc, sizeof(nint));
        nint current = head;

        // Создаем цепочку из 10,000 объектов
        for (int i = 0; i < 10000; i++)
        {
            nint next = rt.Heap.Allocate(desc, sizeof(nint));
            WritePtr(current, next);
            current = next;
        }

        _rootProvider.Add(Value.FromObject(head));

        // Должно отработать без переполнения стека
        rt.Collect(_rootProvider);

        // 10000 + 1 (head)
        Assert.Equal(10001, rt.Heap.Objects.Count());
    }

    [Fact]
    public void MultipleReferencesToSameObject_ProcessedCorrectly()
    {
        var rt = new RuntimeContext();
        var desc = new ObjectDescriptor(ObjectKind.Class, [0, sizeof(nint)]);

        var parent = rt.Heap.Allocate(desc, sizeof(nint) * 2);
        var child = rt.Heap.Allocate(desc, sizeof(nint) * 2);

        // Оба поля указывают на child
        WritePtr(parent, child);
        WritePtr(parent + sizeof(nint), child);

        _rootProvider.Add(Value.FromObject(parent));
        rt.Collect(_rootProvider);

        Assert.Equal(2, rt.Heap.Objects.Count());
    }
}