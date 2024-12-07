using System;
using System.Collections.Generic;
using System.Linq;

class EventSystem
{
    // Лічильник логічного часу (Алгоритм Лампорта)
    private int logicalClock = 0;

    private readonly Dictionary<string, List<Action<string, int>>> eventSubscribers = new();

    public void RegisterEvent(string eventName)
    {
        if (!eventSubscribers.ContainsKey(eventName))
        {
            eventSubscribers[eventName] = new List<Action<string, int>>();
            Console.WriteLine($"Подія \"{eventName}\" зареєстрована.");
        }
    }

    public void Subscribe(string eventName, Action<string, int> callback)
    {
        if (eventSubscribers.ContainsKey(eventName))
        {
            eventSubscribers[eventName].Add(callback);
            Console.WriteLine($"Підписано на подію \"{eventName}\".");
        }
        else
        {
            Console.WriteLine($"Помилка: подія \"{eventName}\" не існує.");
        }
    }

    public void Unsubscribe(string eventName, Action<string, int> callback)
    {
        if (eventSubscribers.ContainsKey(eventName))
        {
            eventSubscribers[eventName].Remove(callback);
            Console.WriteLine($"Скасовано підписку на подію \"{eventName}\".");
        }
    }

    public void TriggerEvent(string eventName)
    {
        if (eventSubscribers.ContainsKey(eventName))
        {
            logicalClock++; // Збільшуємо логічний час
            Console.WriteLine($"Подія \"{eventName}\" викликана. Логічний час: {logicalClock}");

            foreach (var callback in eventSubscribers[eventName])
            {
                callback(eventName, logicalClock);
            }
        }
        else
        {
            Console.WriteLine($"Помилка: подія \"{eventName}\" не існує.");
        }
    }
}

class Program
{
    static void Main()
    {
        EventSystem eventSystem = new EventSystem();

        eventSystem.RegisterEvent("EventA");
        eventSystem.RegisterEvent("EventB");

        eventSystem.Subscribe("EventA", (eventName, time) =>
            Console.WriteLine($"Підписник 1 отримав {eventName} з часом {time}"));
        eventSystem.Subscribe("EventA", (eventName, time) =>
            Console.WriteLine($"Підписник 2 отримав {eventName} з часом {time}"));

        eventSystem.Subscribe("EventB", (eventName, time) =>
            Console.WriteLine($"Підписник 3 отримав {eventName} з часом {time}"));

        eventSystem.TriggerEvent("EventA");
        eventSystem.TriggerEvent("EventB");

        eventSystem.Unsubscribe("EventA", (eventName, time) =>
            Console.WriteLine($"Підписник 1 отримав {eventName} з часом {time}"));

        eventSystem.TriggerEvent("EventA");
    }
}
