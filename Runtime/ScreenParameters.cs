using System;
using System.Collections.Generic;
using UnityEngine;

namespace ScreenSystem
{
    [Serializable]
    public class ScreenParameter
    {
        public string Key;
        public object Value;

        public ScreenParameter(string key, object value)
        {
            Key = key;
            Value = value;
        }

        public override string ToString()
            => $"{Key}={Value}";
    }

    public interface IScreenParameters
    {
        void SetParameters(params ScreenParameter[] parameters);
        void SetParameter(string key, object value);
        object GetParameter(string key, object defaultValue = null);
        T GetParameter<T>(string key, T defaultValue = default);
        bool HasParameter(string key);
        void ClearParameters();
        IReadOnlyDictionary<string, object> GetAllParameters();
    }

    public class ScreenParameters : MonoBehaviour, IScreenParameters
    {
        private Dictionary<string, object> parameters = new();

        public void SetParameters(params ScreenParameter[] screenParameters)
        {
            if (screenParameters == null || screenParameters.Length == 0)
                return;

            foreach (ScreenParameter param in screenParameters)
            {
                if (param != null && !string.IsNullOrEmpty(param.Key))
                    parameters[param.Key] = param.Value;
            }
        }

        public void SetParameter(string key, object value)
        {
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogWarning("[ScreenParameters] Cannot set parameter with empty key");
                return;
            }

            parameters[key] = value;
        }

        public object GetParameter(string key, object defaultValue = null)
        {
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogWarning("[ScreenParameters] Cannot get parameter with empty key");
                return defaultValue;
            }

            return parameters.TryGetValue(key, out object value) ? value : defaultValue;
        }

        public T GetParameter<T>(string key, T defaultValue = default)
        {
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogWarning("[ScreenParameters] Cannot get parameter with empty key");
                return defaultValue;
            }

            if (!parameters.TryGetValue(key, out object value))
                return defaultValue;

            if (value is T typedValue)
                return typedValue;

            if (value == null)
                return defaultValue;

            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                Debug.LogWarning($"[ScreenParameters] Cannot convert parameter '{key}' from {value.GetType().Name} to {typeof(T).Name}");
                return defaultValue;
            }
        }

        public bool HasParameter(string key)
        {
            if (string.IsNullOrEmpty(key))
                return false;

            return parameters.ContainsKey(key);
        }

        public void ClearParameters()
        {
            parameters.Clear();
        }

        public IReadOnlyDictionary<string, object> GetAllParameters()
            => new Dictionary<string, object>(parameters);
    }
}

