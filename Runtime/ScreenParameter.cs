using System;
using System.Collections.Generic;

namespace ScreenSystem
{
    /// <summary>
    /// A value passed with GoTo, carried to the screen's layers without boxing. Created by ScreenManager.GoTo;
    /// it can't be subclassed outside Screen System.
    /// </summary>
    public abstract class ScreenParameter
    {
        internal ScreenParameter()
        {
        }

        public abstract Type ValueType { get; }

        /// <summary>Delivers the value to every receiver on the layers. Returns how many components got it.</summary>
        internal abstract int DeliverTo(IReadOnlyList<LayerComponent> layers);

        /// <summary>Delivers the value and warns when no component on the screen takes this type.</summary>
        internal void DeliverAndReport(string screenId, IReadOnlyList<LayerComponent> layers)
        {
            if (DeliverTo(layers) == 0)
                UnityEngine.Debug.LogWarning($"[ScreenSystem] GoTo({screenId}) passed a {ValueType.Name}, but no component on its layers implements IScreenParameter<{ValueType.Name}>.");
        }

        internal static ScreenParameter Create<T>(T value)
            => new ScreenParameter<T>(value);
    }

    internal sealed class ScreenParameter<T> : ScreenParameter
    {
        private readonly T value;

        public ScreenParameter(T value)
            => this.value = value;

        public override Type ValueType
            => typeof(T);

        internal override int DeliverTo(IReadOnlyList<LayerComponent> layers)
        {
            int received = 0;
            if (layers == null)
                return received;

            for (int i = 0; i < layers.Count; i++)
            {
                if (layers[i] != null)
                    received += layers[i].DeliverParameter(value);
            }

            return received;
        }
    }
}
