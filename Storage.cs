// Copyright (c) 2018 cloudcrate solutions UG (haftungsbeschraenkt)

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.JSInterop;

namespace Cloudcrate.AspNetCore.Blazor.Browser.Storage
{
    public abstract class StorageBase
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly string _fullTypeName;

        private EventHandler<StorageEventArgs> _storageChanged;

        protected abstract string StorageTypeName { get; }


        protected internal StorageBase(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
            _fullTypeName = GetType().FullName.Replace('.', '_');
        }

        public void Clear()
        {
            _jsRuntime.InvokeAsync<object>($"{_fullTypeName}.Clear");
        }

        public async Task<string> GetItem(string key)
        {
            return await _jsRuntime.InvokeAsync<string>($"{_fullTypeName}.GetItem", key);
        }

        public async Task<T> GetItem<T>(string key)
        {
            var json = await GetItem(key);
            return string.IsNullOrEmpty(json) ? default(T) : Json.Deserialize<T>(json);
        }

        public async Task<string> Key(int index)
        {
            return await _jsRuntime.InvokeAsync<string>($"{_fullTypeName}.Key", index);
        }

        public int Length
        {
            get
            {
                var t=Task.Run(() => _jsRuntime.InvokeAsync<int>($"{_fullTypeName}.Length"));
                t.Wait();
                return t.Result;
            }
        }

        public void RemoveItem(string key)
        {
            _jsRuntime.InvokeAsync<object>($"{_fullTypeName}.RemoveItem", key);
        }

        public void SetItem(string key, string data)
        {
            _jsRuntime.InvokeAsync<object>($"{_fullTypeName}.SetItem", key, data);
        }

        public void SetItem(string key, object data)
        {
            SetItem(key, Json.Serialize(data));
        }

        public string this[string key]
        {
            get { var t = _jsRuntime.InvokeAsync<string>($"{_fullTypeName}.GetItemString", key);t.RunSynchronously(); return t.Result; }
            set => _jsRuntime.InvokeAsync<object>($"{_fullTypeName}.SetItemString", key, value);
        }

        //public string this[int index]
        //{
        //    get => _jsRuntime.InvokeAsync<string>($"{_fullTypeName}.GetItemNumber", index);
        //    set => _jsRuntime.InvokeAsync<object>($"{_fullTypeName}.SetItemNumber", index, value);
        //}

        public event EventHandler<StorageEventArgs> StorageChanged
        {
            add
            {
                if (_storageChanged == null)
                {
                    this._jsRuntime.InvokeAsync<object>(
                        $"{_fullTypeName}.AddEventListener",
                        new DotNetObjectRef(this)
                    );
                }
                _storageChanged += value;
            }
            remove
            {
                _storageChanged -= value;
                if (_storageChanged == null)
                {
                    this._jsRuntime.InvokeAsync<object>($"{_fullTypeName}.RemoveEventListener");
                }
            }
        }

        [JSInvokable]
        public virtual void OnStorageChanged(string key, object oldValue, object newValue)
        {
            EventHandler<StorageEventArgs> handler = _storageChanged;
            if (handler != null)
            {
                handler(this, new StorageEventArgs
                {
                    Key = key,
                    OldValue = oldValue,
                    NewValue = newValue,
                });
            }
        }
    }

    public sealed class LocalStorage : StorageBase
    {
        protected override string StorageTypeName => nameof(LocalStorage);

        public LocalStorage(IJSRuntime jsRuntime) : base(jsRuntime)
        {
        }
    }

    public sealed class SessionStorage : StorageBase
    {
        protected override string StorageTypeName => nameof(SessionStorage);

        public SessionStorage(IJSRuntime jsRuntime) : base(jsRuntime)
        {
        }
    }

    public static class ServiceCollectionExtensions
    {
        public static void AddStorage(this IServiceCollection col)
        {
            col.TryAddScoped<LocalStorage>();
            col.TryAddScoped<SessionStorage>();
        }
    }
}
