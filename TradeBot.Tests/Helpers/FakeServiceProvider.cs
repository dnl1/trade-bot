using System;
using System.Collections.Generic;

namespace TradeBot.Tests.Helpers
{
    internal class FakeServiceProvider : IServiceProvider
    {
        private readonly Dictionary<Type, object> _services = new();

        public FakeServiceProvider Register<T>(T service)
        {
            _services[typeof(T)] = service!;
            return this;
        }

        public object? GetService(Type serviceType)
        {
            _services.TryGetValue(serviceType, out var service);
            return service;
        }
    }
}
