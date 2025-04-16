using System;
using System.Collections.Generic;
using System.Linq;

namespace VIAVR.Scripts.Core.Singleton
{
    public static class DependencyContainer
    {
        private static Dictionary<Type, Type> _realizators = new Dictionary<Type, Type>();

        public static void Register(Type interfaceType, Type realizatorType)
        {
            var exceptionMessages = GetRegisterExceptions(interfaceType, realizatorType);

            if (string.IsNullOrEmpty(exceptionMessages))
                _realizators.Add(interfaceType, realizatorType);
            else
                throw new Exception(exceptionMessages);
        }

        public static Type Resolve<I>() where I : class
        {
            var interfaceToResolve = typeof(I);
            var exceptionMessages = GetResolveExceptions(interfaceToResolve);

            if (string.IsNullOrEmpty(exceptionMessages))
                return _realizators[interfaceToResolve];
            else
                throw new Exception(exceptionMessages);
        }

        public static Type Resolve(Type interfaceToResolve)
        {
            var exceptionMessages = GetResolveExceptions(interfaceToResolve);
            if (string.IsNullOrEmpty(exceptionMessages))
                return _realizators[interfaceToResolve];
            else
                throw new Exception(exceptionMessages);
        }

        private static string GetRegisterExceptions(Type interfaceType, Type realizatorType)
        {
            string exceptions = "";
            if (interfaceType == null)
                exceptions += ($"[DependencyContainer] interfaceType == null.\n");

            if (realizatorType == null)
                exceptions += ($"[DependencyContainer] realizatorType == null.\n");

            if (_realizators.ContainsKey(interfaceType))
                exceptions += ($"[DependencyContainer] already contains {interfaceType.FullName}.\n");

            if (!interfaceType.IsInterface)
                exceptions += ($"[DependencyContainer] {interfaceType.FullName} is not interface.\n");

            var realizatorInterfaces = realizatorType.GetInterfaces();

            if (realizatorInterfaces.Length == 0 || !realizatorInterfaces.Contains(interfaceType))
                exceptions += ($"[DependencyContainer] {realizatorType.FullName} doesnt implement {interfaceType}\n");

            return exceptions;
        }

        private static string GetResolveExceptions(Type interfaceToResolve)
        {
            string exceptions = "";

            if (!interfaceToResolve.IsInterface)
                exceptions += $"[DependencyContainer] trying to Resolve {interfaceToResolve.FullName} is not interface.";

            if (!_realizators.ContainsKey(interfaceToResolve))
                exceptions += $"[DependencyContainer] trying to Resolve {interfaceToResolve.FullName} not found target realize class.";

            return exceptions;
        }
    }
}