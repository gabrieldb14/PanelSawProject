using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwinCAT.Ads;

namespace HMI_PanelSaw.Service
{
    public class AdsService : IDisposable
    {
        private AdsClient _adsClient;
        private Dictionary<string, uint> _handles;
        private bool _disposed = false;

        public AdsService()
        {
            _adsClient = new AdsClient();
            _handles = new Dictionary<string, uint>();
        }

        public void Connect(string amsNetId, int port)
        {
            CheckDisposed();
            if (!_adsClient.IsConnected)
            {
                _adsClient.Connect(amsNetId, port);
            }
        }

        public void Disconnect()
        {
            if (_adsClient.IsConnected)
            {
                foreach(var handle in _handles.Values)
                {
                    _adsClient.DeleteVariableHandle(handle);
                }
                _handles.Clear();
                _adsClient.Dispose();
                _adsClient = null;
            }
        }

        public void AddVariable(string variableName)
        {
            if(_adsClient.IsConnected && !_handles.ContainsKey(variableName))
            {
                try
                {
                    uint handle = _adsClient.CreateVariableHandle(variableName);
                    _handles.Add(variableName, handle);
                }
                catch(Exception ex)
                {
                    throw new Exception($"Error creating handle for {variableName}: {ex.Message}");
                }
            }
        }

        public T Read<T>(string variableName)
        {
            try
            {
                if (_handles.ContainsKey(variableName))
                {
                    uint handle = _handles[variableName];
                    if (typeof(T) == typeof(string))
                    {
                        int stringLength = 256;
                        byte[] buffer = new byte[stringLength];
                        Array.Clear(buffer, 0, buffer.Length);
                        
                        _adsClient.Read(handle, buffer.AsMemory());
                        
                        int nullIndex = Array.IndexOf(buffer, (byte)0);
                        if (nullIndex >= 0)
                        {
                            string result = Encoding.Default.GetString(buffer, 0, nullIndex);
                            return (T)(object)result;
                        }
                        else
                        {
                            string result = Encoding.Default.GetString(buffer).TrimEnd('\0');
                            return (T)(object)result;
                        }
                    }
                    else
                    {
                        return (T)_adsClient.ReadAny(handle, typeof(T));
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error reading {variableName}: {ex.Message}", ex);
            }
            return default(T);
        }
        public void Write(string variableName, object value)
        {
            if (_handles.ContainsKey(variableName))
            {
                uint handle = _handles[variableName];
                _adsClient.WriteAny(handle, value);
            }
        }
        public bool IsConnected => _adsClient.IsConnected;

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(AdsService));
            }
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Disconnect();
                    if(_adsClient != null)
                    {
                        _adsClient.Dispose();
                        _adsClient = null;
                    }
                    if(_handles != null)
                    {
                        _handles.Clear();
                        _handles = null;
                    }
                }
                _disposed = true;
            }
        }
        ~AdsService()
        {
            Dispose(false);
        }


    }
}
