using System;
using System.Linq;
using System.Reflection;

#nullable enable

namespace Couchbase.Core.DI
{
    /// <summary>
    /// Implementation of <see cref="IServiceFactory"/> which creates a transient
    /// service for each request.
    /// </summary>
    internal class TransientServiceFactory : IServiceFactory
    {
        private readonly Func<IServiceProvider, object?> _factory;

        private IServiceProvider? _serviceProvider;

        /// <summary>
        /// Creates a new TransientServiceFactory which uses a lambda to create the service.
        /// </summary>
        /// <param name="factory">Lambda to invoke on each call to <see cref="CreateService"/>.</param>
        public TransientServiceFactory(Func<IServiceProvider, object?> factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        /// <summary>
        /// Creates a new TransientServiceFactory which uses a type's constructor on each call to <see cref="CreateService"/>.
        /// </summary>
        /// <param name="type">Type to create on each call to <seealso cref="CreateService"/>.</param>
        public TransientServiceFactory(Type type)
            : this(CreateFactory(type))
        {
        }

        /// <inheritdoc />
        public void Initialize(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /// <inheritdoc />
        public object? CreateService(Type requestedType)
        {
            if (_serviceProvider == null)
            {
                throw new InvalidOperationException("Not initialized.");
            }

            return _factory(_serviceProvider);
        }

        private static Func<IServiceProvider, object> CreateFactory(Type implementationType)
        {
            var constructor = implementationType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .OrderByDescending(p => p.GetParameters().Length)
                .First();

            object Factory(IServiceProvider serviceProvider)
            {
                var constructorArgs = constructor.GetParameters()
                    .Select(p => serviceProvider.GetRequiredService(p.ParameterType))
                    .ToArray();

                return constructor.Invoke(constructorArgs);
            }

            return Factory;
        }
    }
}
