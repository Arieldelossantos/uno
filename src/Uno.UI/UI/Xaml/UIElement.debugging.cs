﻿#if DEBUG
using Windows.Foundation;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using System.Collections.Generic;
using Uno.Extensions;
using Uno.Logging;
using Uno.Disposables;
using System.Linq;
using Windows.Devices.Input;
using Windows.System;
using Windows.UI.Xaml.Controls;
using Uno.UI;
using Uno;
using Uno.UI.Controls;
using Uno.UI.Media;
using System;
using System.Collections;
using System.Numerics;
using System.Reflection;
using Windows.UI.Xaml.Markup;
using Microsoft.Extensions.Logging;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Core;
using System.Text;

#if __IOS__
using UIKit;
#endif

namespace Windows.UI.Xaml
{
	public partial class UIElement
	{
		/// <summary>
		/// Debugging helper method to get a list of the set value at each precedence for a DependencyProperty.
		/// </summary>
		/// <param name="propertyName">The property to get values by precedence for.</param>
		internal (object value, DependencyPropertyValuePrecedences precedence)[] GetValuesByPrecedence(string propertyName)
		{

			var dp = GetDPByName(propertyName);
			if (dp == null)
			{
				return null;
			}

			return this.GetValueForEachPrecedences(dp);
		}

		/// <summary>
		/// A helper method while debugging to get the theme resource, if any, assigned to <paramref name="propertyName"/>.
		/// </summary>
		internal string GetThemeSource(string propertyName)
		{
			var dp = GetDPByName(propertyName);
			if (dp == null)
			{
				return "[No such property]";
			}
			var bindings = (this as IDependencyObjectStoreProvider).Store.GetResourceBindingsForProperty(dp);
			if (bindings.Any())
			{
				var output = "";
				foreach (var binding in bindings)
				{
					output += $"{binding.ResourceKey} ({binding.Precedence}), ";
				}

				return output;
			}
			else
			{
				return "[None]";
			}
		}

		private DependencyProperty GetDPByName(string propertyName) => GetDPByName(propertyName, this);

		private static DependencyProperty GetDPByName(string propertyName, DependencyObject propertyOwner)
		{
			if (!propertyName.EndsWith("Property"))
			{
				propertyName += "Property";
			}
			var propInfo = propertyOwner.GetType().GetTypeInfo().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
			var dp = propInfo?.GetValue(null) as DependencyProperty;
			return dp;
		}

		/// <summary>
		/// Lists all resource keys associated with <paramref name="resource"/>, both in local code and the framework. This is spectacularly
		/// inefficient and only useful for providing extra information while debugging.
		/// </summary>
		/// <remarks>
		/// Currently won't work with value-typed resources (eg double, Thickness) since it uses ReferencEquals() and they will be boxed.
		/// </remarks>
		internal object[] GetKeysForResource(object resource)
		{
			return Inner().ToArray();

			IEnumerable<object> Inner()
			{
				var fe = this as FrameworkElement;
				while (fe != null)
				{
					foreach (var key in TryFindResource(fe.Resources))
					{
						yield return key;
					}

					fe = fe.Parent as FrameworkElement;
				}

				foreach (var key in TryFindResource(Application.Current.Resources))
				{
					yield return key;
				}
				foreach (var key in TryFindResource(Uno.UI.GlobalStaticResources.MasterDictionary))
				{
					yield return key;
				}

				IEnumerable<object> TryFindResource(ResourceDictionary resourceDictionary)
				{
					foreach (var kvp in resourceDictionary)
					{
						if (ReferenceEquals(resource, kvp.Value)) // TODO: doesn't work for value types
						{
							yield return kvp.Key;
						}
					}

					foreach (var mergedDict in resourceDictionary.MergedDictionaries)
					{
						foreach (var key in TryFindResource(mergedDict))
						{
							yield return key;
						}
					}

					foreach (var themeDict in resourceDictionary.ThemeDictionaries.Values.OfType<ResourceDictionary>())
					{
						foreach (var key in TryFindResource(themeDict))
						{
							yield return key;
						}
					}
				}
			}
		}

		/// <summary>
		/// Debugging helper method to get the parent element that this element is inheriting <paramref name="propertyName"/> from.
		///
		/// Eg, if you call textBlock.GetPropInheritanceParent("Foreground"), it will return the first parent which has a property called
		/// "Foreground" (most likely the Control whose template contains the TextBlock).
		/// </summary>
		internal FrameworkElement GetPropInheritanceParent(string propertyName)
		{
			var dependencyProp = GetDPByName(propertyName);
			if (!IsInherited(dependencyProp, GetType()))
			{
				return null;
			}

			return Uno.UI.Extensions.DependencyObjectExtensions.FindFirstParent<FrameworkElement>(this, fe => IsInherited(GetDPByName(propertyName, fe), fe.GetType()), includeCurrent: false);

			bool IsInherited(DependencyProperty dp, Type type)
			{
				if (dp.Name == "DataContext")
				{
					return true;
				}
				var metadata = dp?.GetMetadata(type) as FrameworkPropertyMetadata;
				return metadata?.Options.HasFlag(FrameworkPropertyMetadataOptions.Inherits) ?? false;
			}
		}

		/// <summary>
		/// Debugging helper method to get a condensed summary of visual states on controls in this element's hierarchy.
		/// </summary>
		internal string GetVisualStatesSummary()
		{
			var sb = new StringBuilder();
			foreach (var ancestor in Uno.UI.Extensions.DependencyObjectExtensions.GetAllParents(this))
			{
				if (ancestor is Control control && control.GetTemplateRoot() is { } root)
				{
					var groups = VisualStateManager.GetVisualStateGroups(root);
					if (groups != null)
					{
						sb.Append($"Parent: {control}, ");
						sb.Append("States: [");
						foreach (var group in groups)
						{
							sb.Append($"{group}: {group.CurrentState}, ");
						}
						sb.Append(" ]; ");
					}
				}
			}
			return sb.ToString();
		}
	}
}
#endif
