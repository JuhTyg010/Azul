using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DeepQLearningBot;

public class ReplayBuffer
{
    [JsonInclude] private List<Replay> buffer;
    [JsonInclude]private int capacity;
    private Random random = new Random();

    public ReplayBuffer(int capacity) {
        this.capacity = capacity;
        buffer = new List<Replay>();
    }

    public void Add(double[] state, int action, double reward, double[] nextState, bool done)
    {
        if (buffer.Count >= capacity) {
            buffer.RemoveAt(0);
        }
        buffer.Add(new Replay(state, action, reward, nextState, done));
    }

    public List<Replay> Sample(int batchSize) {
        var sample = new List<Replay>();
        var bufferArray = buffer.ToArray();

        for (int i = 0; i < batchSize; i++) {
            int index = random.Next(bufferArray.Length);
            sample.Add(bufferArray[index]);
            //buffer.Remove(bufferArray[index]);
        }

        return sample;
    }

    public int Count => buffer.Count;

    public void UpdateRewards(double reward) {
        foreach (var replay in buffer) {
            replay.AddReward(reward);
        }
    }

    public void Marge(ReplayBuffer replayBuffer) {    
        buffer.AddRange(replayBuffer.buffer);
    }
}