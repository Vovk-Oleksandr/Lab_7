using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

class ResourceManager
{
    private readonly SemaphoreSlim _cpuSemaphore;
    private readonly SemaphoreSlim _ramSemaphore;
    private readonly SemaphoreSlim _diskSemaphore;
    private readonly object _lock = new object();
    private readonly Queue<TaskCompletionSource<bool>> _highPriorityQueue = new();
    private readonly Queue<TaskCompletionSource<bool>> _lowPriorityQueue = new();

    public ResourceManager(int cpuCount, int ramCount, int diskCount)
    {
        _cpuSemaphore = new SemaphoreSlim(cpuCount);
        _ramSemaphore = new SemaphoreSlim(ramCount);
        _diskSemaphore = new SemaphoreSlim(diskCount);
    }

    public async Task<bool> RequestResourcesAsync(int cpu, int ram, int disk, bool highPriority)
    {
        var tcs = new TaskCompletionSource<bool>();
        lock (_lock)
        {
            if (highPriority)
                _highPriorityQueue.Enqueue(tcs);
            else
                _lowPriorityQueue.Enqueue(tcs);

            TryGrantAccess();
        }

        // Очікуємо завершення доступу до ресурсів.
        return await tcs.Task;
    }

    private void TryGrantAccess()
    {
        lock (_lock)
        {
            // Перевіряємо чергу з високим пріоритетом.
            if (_highPriorityQueue.TryPeek(out var highPriorityRequest) &&
                CanAllocateResources())
            {
                _highPriorityQueue.Dequeue();
                AllocateResources();
                highPriorityRequest.SetResult(true);
                return;
            }

            // Перевіряємо чергу з низьким пріоритетом.
            if (_lowPriorityQueue.TryPeek(out var lowPriorityRequest) &&
                CanAllocateResources())
            {
                _lowPriorityQueue.Dequeue();
                AllocateResources();
                lowPriorityRequest.SetResult(true);
            }
        }
    }

    private bool CanAllocateResources()
    {
        return _cpuSemaphore.CurrentCount > 0 &&
               _ramSemaphore.CurrentCount > 0 &&
               _diskSemaphore.CurrentCount > 0;
    }

    private void AllocateResources()
    {
        _cpuSemaphore.Wait();
        _ramSemaphore.Wait();
        _diskSemaphore.Wait();
    }

    public void ReleaseResources()
    {
        lock (_lock)
        {
            _cpuSemaphore.Release();
            _ramSemaphore.Release();
            _diskSemaphore.Release();
            TryGrantAccess();
        }
    }
}

class Program
{
    static async Task SimulateTask(string taskName, ResourceManager resourceManager, int cpu, int ram, int disk, bool highPriority)
    {
        Console.WriteLine($"{taskName}: Requesting resources (CPU: {cpu}, RAM: {ram}, Disk: {disk}, HighPriority: {highPriority})");
        var success = await resourceManager.RequestResourcesAsync(cpu, ram, disk, highPriority);

        if (success)
        {
            Console.WriteLine($"{taskName}: Resources allocated. Working...");
            await Task.Delay(2000); // Імітація роботи.
            Console.WriteLine($"{taskName}: Releasing resources.");
            resourceManager.ReleaseResources();
        }
        else
        {
            Console.WriteLine($"{taskName}: Failed to allocate resources.");
        }
    }

    static async Task Main(string[] args)
    {
        var resourceManager = new ResourceManager(cpuCount: 2, ramCount: 2, diskCount: 2);

        var tasks = new List<Task>
        {
            SimulateTask("Task1", resourceManager, 1, 1, 1, highPriority: false),
            SimulateTask("Task2", resourceManager, 1, 1, 1, highPriority: true),
            SimulateTask("Task3", resourceManager, 1, 1, 1, highPriority: false),
            SimulateTask("Task4", resourceManager, 1, 1, 1, highPriority: true),
            SimulateTask("Task5", resourceManager, 1, 1, 1, highPriority: false)
        };

        await Task.WhenAll(tasks);
    }
}
