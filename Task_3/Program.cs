using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

class Operation
{
    public string Resource { get; }
    public string ThreadId { get; }
    public DateTime Timestamp { get; }

    public Operation(string resource, string threadId)
    {
        Resource = resource;
        ThreadId = threadId;
        Timestamp = DateTime.UtcNow;
    }

    public override string ToString()
    {
        return $"[Resource: {Resource}, Thread: {ThreadId}, Timestamp: {Timestamp:HH:mm:ss.fff}]";
    }
}

class OperationLog
{
    private readonly ConcurrentQueue<Operation> _log = new();
    private readonly Dictionary<string, object> _resourceLocks = new();

    public void AddOperation(Operation operation)
    {
        lock (GetResourceLock(operation.Resource))
        {
            // Імітуємо можливість конфлікту.
            if (_log.Any(op => op.Resource == operation.Resource &&
                               (DateTime.UtcNow - op.Timestamp).TotalMilliseconds < 100))
            {
                Console.WriteLine($"Conflict detected for resource {operation.Resource} by thread {operation.ThreadId}!");
                ResolveConflict(operation);
            }
            else
            {
                _log.Enqueue(operation);
                Console.WriteLine($"Operation logged: {operation}");
            }
        }
    }

    private void ResolveConflict(Operation operation)
    {
        Console.WriteLine($"Resolving conflict for {operation.Resource} by thread {operation.ThreadId}...");
        Thread.Sleep(50); // Імітація часу на розв'язання. Конфлікт вирішується через затримку та повторне додавання операції.
        AddOperation(operation); // Повторна спроба додати операцію після розв'язання.
    }

    private object GetResourceLock(string resource)
    {
        lock (_resourceLocks)
        {
            if (!_resourceLocks.ContainsKey(resource))
            {
                _resourceLocks[resource] = new object();
            }

            return _resourceLocks[resource];
        }
    }

    public void PrintLog()
    {
        Console.WriteLine("\nFinal Operation Log:");
        foreach (var op in _log)
        {
            Console.WriteLine(op);
        }
    }
}

class Program
{
    static async Task SimulateThread(OperationLog log, string threadId, List<string> resources)
    {
        var random = new Random();

        for (int i = 0; i < 5; i++)
        {
            string resource = resources[random.Next(resources.Count)];
            var operation = new Operation(resource, threadId);
            log.AddOperation(operation);
            await Task.Delay(random.Next(50, 150)); // Імітація часу між операціями.
        }
    }

    static async Task Main(string[] args)
    {
        var log = new OperationLog();
        var resources = new List<string> { "FileA", "FileB", "FileC", "Database" };

        // Створюємо потоки.
        var tasks = new List<Task>
        {
            SimulateThread(log, "Thread1", resources),
            SimulateThread(log, "Thread2", resources),
            SimulateThread(log, "Thread3", resources)
        };

        await Task.WhenAll(tasks);

        // Виводимо фінальний журнал.
        log.PrintLog();
    }
}

