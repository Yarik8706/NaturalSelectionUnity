using System;
using UnityEngine;

public class NeuralNetwork
{
    public readonly Layer[] layers;
    public NeuralNetwork(params int[] layerCount)
    {
        layers = new Layer[layerCount.Length];
        for (int i = 0; i < layerCount.Length; i++)
        {
            int nextSize = 0;
            if(i < layerCount.Length - 1) nextSize = layerCount[i + 1];
            layers[i] = new Layer(layerCount[i], nextSize);
            for (int j = 0; j < layerCount[i]; j++)
            {
                for (int k = 0; k < nextSize; k++)
                {
                    layers[i].weights[j, k] = UnityEngine.Random.Range(-1f, 1f);
                }
            }
        }
    }

    public float[] FeedForward(float[] inputs)
    {
        Array.Copy(inputs, 0, layers[0].neurons, 0, inputs.Length);
        for (var item = 1; item < layers.Length; item++) 
        {
            float min = 0f;
            if(item == layers.Length - 1) min = -1f;
            Layer l = layers[item - 1];
            Layer l1 = layers[item];
            for (int j = 0; j < l1.size; j++)
            {
                l1.neurons[j] = 0;
                for (int k = 0; k < l.size; k++)
                {
                    l1.neurons[j] += l.neurons[k] * l.weights[k, j];
                }
                l1.neurons[j] = Mathf.Min(1f, Mathf.Max(min, l1.neurons[j]));
            }
        }
        return layers[^1].neurons;
    }

}