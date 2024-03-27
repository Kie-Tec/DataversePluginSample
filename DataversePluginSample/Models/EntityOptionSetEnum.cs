#pragma warning disable CS1591
//------------------------------------------------------------------------------
// <auto-generated>
//     O código foi gerado por uma ferramenta.
//     Versão de Tempo de Execução:4.0.30319.42000
//
//     As alterações ao arquivo poderão causar comportamento incorreto e serão perdidas se
//     o código for gerado novamente.
// </auto-generated>
//------------------------------------------------------------------------------

namespace DataversePluginSample.Model
{
	
	
	internal sealed class EntityOptionSetEnum
	{
		
		/// <summary>
		/// Returns the integer version of an OptionSetValue
		/// </summary>
		public static System.Nullable<int> GetEnum(Microsoft.Xrm.Sdk.Entity entity, string attributeLogicalName)
		{
			if (entity.Attributes.ContainsKey(attributeLogicalName))
			{
				Microsoft.Xrm.Sdk.OptionSetValue value = entity.GetAttributeValue<Microsoft.Xrm.Sdk.OptionSetValue>(attributeLogicalName);
				if (value != null)
				{
					return value.Value;
				}
			}
			return null;
		}
		
		/// <summary>
		/// Returns a collection of integer version's of an Multi-Select OptionSetValue for a given attribute on the passed entity
		/// </summary>
		public static System.Collections.Generic.IEnumerable<T> GetMultiEnum<T>(Microsoft.Xrm.Sdk.Entity entity, string attributeLogicalName)
		
		{
			Microsoft.Xrm.Sdk.OptionSetValueCollection value = entity.GetAttributeValue<Microsoft.Xrm.Sdk.OptionSetValueCollection>(attributeLogicalName);
			System.Collections.Generic.List<T> list = new System.Collections.Generic.List<T>();
			if (value == null)
			{
				return list;
			}
			list.AddRange(System.Linq.Enumerable.Select(value, v => (T)(object)v.Value));
			return list;
		}
		
		/// <summary>
		/// Returns a OptionSetValueCollection based on a list of Multi-Select OptionSetValues
		/// </summary>
		public static Microsoft.Xrm.Sdk.OptionSetValueCollection GetMultiEnum<T>(Microsoft.Xrm.Sdk.Entity entity, string attributeLogicalName, System.Collections.Generic.IEnumerable<T> values)
		
		{
			if (values == null)
			{
				return null;
			}
			Microsoft.Xrm.Sdk.OptionSetValueCollection collection = new Microsoft.Xrm.Sdk.OptionSetValueCollection();
			collection.AddRange(System.Linq.Enumerable.Select(values, v => new Microsoft.Xrm.Sdk.OptionSetValue((int)(object)v)));
			return collection;
		}
	}
}
#pragma warning restore CS1591
