using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

class DistributedSystemNode
{
    private readonly string _nodeId;
    private bool _isActive;
    private readonly ConcurrentQueue<string> _messageQueue = new ConcurrentQueue<string>();
    private readonly Dictionary<string, DistributedSystemNode> _otherNodes;

    public DistributedSystemNode(string nodeId, Dictionary<string, DistributedSystemNode> otherNodes)
    {
        _nodeId = nodeId;
        _isActive = true;
        _otherNodes = otherNodes;
        _otherNodes[_nodeId] = this; // Додаємо вузол до списку вузлів.
    }

    public void SendMessage(string recipientId, string message)
    {
        if (_otherNodes.ContainsKey(recipientId))
        {
            _otherNodes[recipientId].ReceiveMessage($"From {_nodeId}: {message}");
        }
        else
        {
            Console.WriteLine($"Node {recipientId} not found.");
        }
    }

    public void ReceiveMessage(string message)
    {
        _messageQueue.Enqueue(message);
    }

    public async Task ProcessMessagesAsync()
    {
        while (_isActive)
        {
            if (_messageQueue.TryDequeue(out string message))
            {
                Console.WriteLine($"[{_nodeId}] Received: {message}");
            }
            else
            {
                await Task.Delay(100); // Чекаємо, якщо черга порожня.
            }
        }
    }

    public async Task NotifyStatusAsync()
    {
        while (_isActive)
        {
            foreach (var node in _otherNodes.Values)
            {
                if (node != this)
                {
                    node.ReceiveMessage($"Status Update: {_nodeId} is active.");
                }
            }

            await Task.Delay(2000); // Відправляємо оновлення статусу кожні 2 секунди.
        }
    }

    public void Deactivate()
    {
        _isActive = false;
        Console.WriteLine($"[{_nodeId}] Deactivated.");
    }
}

class Program
{
    static async Task Main(string[] args)
    {
        var nodes = new Dictionary<string, DistributedSystemNode>();

        var nodeA = new DistributedSystemNode("NodeA", nodes);
        var nodeB = new DistributedSystemNode("NodeB", nodes);
        var nodeC = new DistributedSystemNode("NodeC", nodes);

        var tasks = new List<Task>
        {
            nodeA.ProcessMessagesAsync(),
            nodeB.ProcessMessagesAsync(),
            nodeC.ProcessMessagesAsync(),
            nodeA.NotifyStatusAsync(),
            nodeB.NotifyStatusAsync(),
            nodeC.NotifyStatusAsync()
        };

        nodeA.SendMessage("NodeB", "Hello, NodeB!");
        nodeB.SendMessage("NodeC", "Hi, NodeC!");
        nodeC.SendMessage("NodeA", "Hey, NodeA!");

        await Task.Delay(5000);

        nodeC.Deactivate();

        await Task.Delay(3000);

        nodeA.Deactivate();
        nodeB.Deactivate();

        await Task.WhenAll(tasks);
    }
}
