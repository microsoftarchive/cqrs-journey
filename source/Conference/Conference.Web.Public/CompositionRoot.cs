using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web.Mvc;
using System.Web.Routing;

namespace Conference.Web.Public
{
    internal sealed class CompositionRoot : DefaultControllerFactory
    {
        private readonly IDictionary<Type, object> services;

        public CompositionRoot(IDictionary<Type, object> services)
        {
            this.services = services;
        }

        protected override IController GetControllerInstance(RequestContext requestContext, Type controllerType)
        {
            IEnumerable<object> consturctorArguments = controllerType
                .GetModestConstructor().GetParameters().Select(pi => pi.ParameterType).Select(GetService);

            return (IController)Activator.CreateInstance(controllerType, consturctorArguments.ToArray());
        }

        private object GetService(Type serviceType)
        {
            object instance;
            if (!services.TryGetValue(serviceType, out instance))
            {
                return null;
            }

            return instance;
        }
    }

    internal static class ControllerType
    {
        internal static ConstructorInfo GetModestConstructor(this Type type)
        {
            return (from ci in type.GetConstructors(
                     BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance)
                    orderby ci.GetParameters().Length ascending
                    select ci).First();
        }
    }
}