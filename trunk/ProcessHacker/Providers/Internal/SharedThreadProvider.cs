﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace ProcessHacker
{
    public class SharedThreadProvider : IDisposable
    {
        private object _disposeLock = new object();
        private bool _disposed;
        private List<IProvider> _providers = new List<IProvider>();
        private Thread _thread;
        private int _interval;

        public SharedThreadProvider(int interval)
        {
            _interval = interval;
            _thread = new Thread(new ThreadStart(this.Update));
            _thread.SetApartmentState(ApartmentState.STA);
            _thread.Start();
            _thread.Priority = ThreadPriority.Lowest;
        }

        ~SharedThreadProvider()
        {
            this.Dispose();
        }

        public void Dispose()
        {
            lock (_disposeLock)
            {
                if (!_disposed)
                {
                    _disposed = true;

                    _thread.Abort();
                    _thread = null;

                    foreach (IProvider provider in _providers)
                        this.Remove(provider);

                    GC.SuppressFinalize(this);
                }
            }
        }

        public int Interval
        {
            get { return _interval; }
            set { _interval = value; }
        }

        public void Add(IProvider provider)
        {
            provider.CreateThread = false;

            lock (_providers)
                _providers.Add(provider);
        }

        public void Remove(IProvider provider)
        {
            lock (_providers)
                _providers.Remove(provider);

            provider.CreateThread = true;
        }

        private void Update()
        {
            while (true)
            {
                IProvider[] providers;

                lock (_providers)
                    providers = _providers.ToArray();

                foreach (var provider in providers)
                    if (provider.Enabled)
                        provider.RunOnce();

                Thread.Sleep(_interval);
            }
        }
    }
}
