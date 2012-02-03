using System;
using System.Collections;
using System.Collections.Generic;
using Autofac;
using Autofac.Core;
using System.Linq;
using Microsoft.Practices.ServiceLocation;

namespace Microsoft.Practices.ServiceLocation
{
	public delegate void ContainerAdder(Autofac.ContainerBuilder b);

	public interface IServiceLocator : IServiceProvider
	{

		object GetInstance(Type serviceType);

		object GetInstance(Type serviceType, string key);

		IEnumerable<object> GetAllInstances(Type serviceType);



		TService GetInstance<TService>();

		TService GetInstance<TService>(string key);

		IEnumerable<TService> GetAllInstances<TService>();

		IServiceLocator CreateNewUsing(ContainerAdder adder);
	}
}
namespace WeSay.Project
{
	/// <summary>
	/// From autofac google list, author Justin Rudd... looks like it might show up
	/// in a future version of autofac
	/// </summary>
	public class ServiceLocatorAdapter : IServiceLocator
	{
		readonly ILifetimeScope _container;

		public ServiceLocatorAdapter(ILifetimeScope container)
		{
			if (container == null)
				throw new ArgumentNullException("container");

			this._container = container;
		}

		#region IServiceProvider methods

		public object GetService(Type serviceType)
		{
			return GetInstance(serviceType);
		}

		#endregion

		#region IServiceLocator methods

		public object GetInstance(Type serviceType)
		{
			return GetInstanceWhichWrapsExceptions(() => _container.Resolve(serviceType));
		}

		public object GetInstance(Type serviceType, string key)
		{
			return
				string.IsNullOrEmpty(key)
					? GetInstanceWhichWrapsExceptions(() => _container.Resolve(serviceType))
					: GetInstanceWhichWrapsExceptions(() => _container.ResolveNamed(key, serviceType));
		}

		public TService GetInstance<TService>()
		{
			return GetInstanceWhichWrapsExceptions(() => _container.Resolve<TService>());
		}

		public TService GetInstance<TService>(string key)
		{
			return GetInstanceWhichWrapsExceptions(() => _container.ResolveNamed<TService>(key));
		}

		public IEnumerable<object> GetAllInstances(Type serviceType)
		{
			var enumerableType = typeof (IEnumerable<>).MakeGenericType(serviceType);

			object instance = _container.Resolve(enumerableType);
			return ((IEnumerable) instance).Cast<object>();
/*
			// go through all the registrations and find TypedService instances that
			// equal the serviceType.
			var servicesToActivate
				= from reg in _container.ComponentRegistry.Registrations
				  from svc in reg.Descriptor.Services.OfType<TypedService>()
				  where svc.ServiceType.Equals(serviceType)
				  select svc;

			// where we'll collect them....
			var result = new List<object>();

			// Then we create each one...
			foreach (var service in servicesToActivate)
				result.Add(GetInstanceWhichWrapsExceptions(() => _container.Resolve(service)));

			return result;
*/
		}

		public IEnumerable<TService> GetAllInstances<TService>()
		{
			foreach (var item in GetAllInstances(typeof(TService)))
				yield return (TService)item;
		}


		/// <summary>
		/// Create a new service locator which contains a new service
		/// </summary>
		 /// <returns></returns>
		public IServiceLocator CreateNewUsing(ContainerAdder adder)
		{
			var containerBuilder = new Autofac.ContainerBuilder();
			adder.Invoke(containerBuilder);
			var innerContainer = _container.BeginLifetimeScope();
			containerBuilder.Update(innerContainer.ComponentRegistry); // DG: don't know if this change will work
			return new ServiceLocatorAdapter(innerContainer);
		}

		#endregion

		#region Implementation

		private T GetInstanceWhichWrapsExceptions<T>(System.Func<T> resolve)
		{
			try
			{
				return resolve();
			}
			catch (DependencyResolutionException ex)
			{
				throw new ActivationException(ex.Message, ex);
			}
/*            catch (ComponentNotRegisteredException ex)
			{
				throw new ActivationException(ex.Message, ex);
			}
			*/
		}

		#endregion
	}

	/// <summary>
	/// jh made up this implementation to fit the code above I don't know what the actual
	/// members are
	/// </summary>
	internal class ActivationException : Exception
	{
		public string Message { get; set; }
		public Exception OriginalException { get; set; }

		public ActivationException(string message, Exception ex)
		{
			Message = message;
			OriginalException = ex;
		}
	}
}
