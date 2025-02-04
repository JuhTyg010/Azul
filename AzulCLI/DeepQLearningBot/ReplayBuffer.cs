using System;
using System.Collections.Generic;

namespace DeepQLearningBot;

public class ReplayBuffer
{
    private Queue<(double[], int, double, double[], bool)> buffer;
    private int capacity;

    public ReplayBuffer(int capacity)
    {
        this.capacity = capacity;
        buffer = new Queue<(double[], int, double, double[], bool)>();
    }

    public void Add(double[] state, int action, double reward, double[] nextState, bool done)
    {
        if (buffer.Count >= capacity)
            buffer.Dequeue(); // Remove the oldest experience
        buffer.Enqueue((state, action, reward, nextState, done));
    }

    public List<(double[], int, double, double[], bool)> Sample(int batchSize)
    {
        var sample = new List<(double[], int, double, double[], bool)>();
        var bufferArray = buffer.ToArray();
        var random = new Random();

        for (int i = 0; i < batchSize; i++)
        {
            int index = random.Next(bufferArray.Length);
            sample.Add(bufferArray[index]);
        }

        return sample;
    }

    public int Count => buffer.Count;
}
