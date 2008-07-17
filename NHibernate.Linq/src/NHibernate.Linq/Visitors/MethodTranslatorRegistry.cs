using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using NHibernate.Linq.Exceptions;

namespace NHibernate.Linq.Visitors
{
	public class MethodTranslatorRegistry
	{
		static MethodTranslatorRegistry()
		{
			current = new MethodTranslatorRegistry();
		}
		public static MethodTranslatorRegistry Current
		{
			get { return current; }
		}

		private static MethodTranslatorRegistry current;


		#region IMethodTranslatorRegistry Members

		private readonly IDictionary<System.Type, System.Type> methodToTranslator;
		protected MethodTranslatorRegistry()
		{
			this.methodToTranslator = new Dictionary<System.Type, System.Type>();
		}
		public void RegisterTranslator(System.Type typeInfo, System.Type translator)
		{
			if (typeof(IMethodTranslator).IsAssignableFrom(translator))
			{
				if (translator.GetConstructor(new System.Type[] { }) != null)
					this.methodToTranslator[typeInfo] = translator;
				else
					throw new MethodTranslatorDefaultConstructorMissingException(translator);
			}
			else
				throw new TranslatorShouldImplementIMethodTranslatorException(translator);
		}
		public void RegisterTranslator<TSubject, TTranslator>() where TTranslator : new()
		{
			RegisterTranslator(typeof(TSubject), typeof(TTranslator));
		}
		//public void RegisterTranslator<TTranslator>(Expression<System.Action> action) where TTranslator : new()
		//{
		//    MethodCallExpression call = action.Body as MethodCallExpression;
		//    RegisterTranslator(call.Method.DeclaringType, typeof(TTranslator));
		//}
		public void UnregisterTranslator(System.Type typeInfo)
		{
			this.methodToTranslator.Remove(typeInfo);
		}

		public IMethodTranslator GetTranslatorInstanceForMethod(MethodInfo methodInfo)
		{

			return this.GetTranslatorInstanceForType(methodInfo.DeclaringType);
		}
		public IMethodTranslator GetTranslatorInstanceForType(System.Type typeInfo)
		{

			System.Type translatorType;
			if (methodToTranslator.TryGetValue(typeInfo, out translatorType))
			{
				var translator = Activator.CreateInstance(translatorType) as IMethodTranslator;
				return translator;
			}
			else if (typeInfo.IsGenericType)
			{
				return GetTranslatorInstanceForType(typeInfo.GetGenericTypeDefinition());
			}
			else
				throw new NHibernate.Linq.Exceptions.MethodTranslatorNotRegistered(typeInfo);
		}
		#endregion
	}
}
