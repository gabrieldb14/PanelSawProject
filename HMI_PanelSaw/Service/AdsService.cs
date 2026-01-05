using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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
        private readonly object _lockobject = new object();

        public AdsService()
        {
            _adsClient = new AdsClient();
            _handles = new Dictionary<string, uint>();
        }

        public void Connect(string amsNetId, int port)
        {
            CheckDisposed();
            lock (_lockobject)
            {
                if (_adsClient != null && !_adsClient.IsConnected)
                {
                    try
                    {
                        _adsClient.Connect(amsNetId, port);
                    }
                    catch(Exception ex)
                    {
                        throw new Exception($"Failed to connect to PLC at {amsNetId}:{port}: {ex.Message}", ex);
                    }
                }
            }

            /*
            if (!_adsClient.IsConnected)
            {
                _adsClient.Connect(amsNetId, port);
            }
            */
        }

        public void Disconnect()
        {
            lock (_lockobject)
            {
                if(_adsClient != null && _adsClient.IsConnected)
                {
                    try
                    {
                        foreach (var handle in _handles.Values)
                        {
                            try
                            {
                                _adsClient.DeleteVariableHandle(handle);
                            }
                            catch (Exception)
                            {

                            }
                        }
                        _handles.Clear();
                        _adsClient.Disconnect();
                    }
                    catch (Exception)
                    {

                    }
                }
            }
            /*
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
             */
        }

        public void AddVariable(string variableName)
        {
            CheckDisposed();
            lock (_lockobject)
            {
                if (_adsClient?.IsConnected == true && !_handles.ContainsKey(variableName))
                {
                    try
                    {
                        uint handle = _adsClient.CreateVariableHandle(variableName);
                        _handles.Add(variableName, handle);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Error creating handle for {variableName}: {ex.Message}");
                    }
                }
            }
            
        }

        public T Read<T>(string variableName)
        {
            CheckDisposed();
            lock (_lockobject)
            {
                try
                {
                    if (_adsClient?.IsConnected == true && _handles.ContainsKey(variableName))
                    {
                        uint handle = _handles[variableName];
                        if(typeof(T) == typeof(string))
                        {
                            int stringLength = 256;

                            var result = _adsClient.ReadAsResult(handle, stringLength);
                            if (result.Succeeded)
                            {
                                byte[] buffer = result.Data.ToArray();
                                int nullIndex = Array.IndexOf(buffer, (byte)0);
                                if(nullIndex >= 0)
                                {
                                    string stringResult = Encoding.Default.GetString(buffer, 0, nullIndex);
                                    return (T)(object)stringResult;
                                }
                                else
                                {
                                    string stringResult = Encoding.Default.GetString(buffer).TrimEnd('\0');
                                    return (T)(object)stringResult;
                                }
                            }
                            else
                            {
                                throw new Exception($"Failed to read string from {variableName}: ErrorCode={result.ErrorCode}");
                            }
                        }
                        else
                        {
                            return (T)_adsClient.ReadAny(handle, typeof(T));
                        }
                    }
                }
                catch(Exception ex)
                {
                    throw new Exception($"Error reading {variableName}: {ex.Message}", ex);
                }
                return default(T);
            }
        }

        public void Write(string variableName, object value)
        {
            CheckDisposed();
            lock (_lockobject)
            {
                try
                {
                    if (_adsClient?.IsConnected == true && _handles.ContainsKey(variableName))
                    {
                        uint handle = _handles[variableName];
                        _adsClient.WriteAny(handle, value);
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error Writing to {variableName}", ex);
                }
            }
        }

        public bool IsConnected{
            get
            {
                lock (_lockobject)
                {
                    return _adsClient?.IsConnected == true;
                }
            }
        }

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
                    lock (_lockobject)
                    {
                        Disconnect();
                        if (_adsClient != null)
                        {
                            try
                            {
                                _adsClient.Dispose();
                            }
                            catch (Exception)
                            {

                            }
                            _adsClient = null;
                        }
                        _handles?.Clear();
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
