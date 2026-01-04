using Skipper.Runtime.Abstractions;
using Skipper.Runtime.GC;
using Skipper.Runtime.Memory;

namespace Skipper.Runtime;

public sealed class RuntimeContext
{
    public Heap Heap { get; }

    // ReSharper disable once InconsistentNaming
    public IGarbageCollector GC { get; }

    public RootSet Roots { get; }

    public RuntimeContext()
    {
        Heap = new Heap();
        GC = new MarkSweepGC(Heap);
        Roots = new RootSet();
    }

    public void Collect()
    {
        GC.Collect(Roots);
    }
}