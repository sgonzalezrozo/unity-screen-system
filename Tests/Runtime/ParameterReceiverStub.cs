using System.Collections.Generic;
using UnityEngine;

namespace ScreenSystem.Tests
{
    /// <summary>Records the values Screen System delivers to it. In a runtime assembly, because Unity can't attach MonoBehaviours from Editor assemblies.</summary>
    public sealed class ParameterReceiverStub : MonoBehaviour, IScreenParameter<string>, IScreenParameter<int>
    {
        public readonly List<object> Received = new();

        public void OnScreenParameter(string value) => Received.Add(value);
        public void OnScreenParameter(int value) => Received.Add(value);
    }
}
