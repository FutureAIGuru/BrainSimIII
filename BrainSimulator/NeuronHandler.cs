using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace BrainSimulator
{

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class NeuronPartial
    {
        public int id;
        public bool inUse;
        public float lastCharge;
        public float currentCharge;
        public float leakRate;
        public int axonDelay;
        public Neuron.modelType model;
        public int dummy; //TODO :Don't know why this is here, it is not required for alignment
        public long lastFired;
    };

    public class NeuronHandler
    {
    }
}
