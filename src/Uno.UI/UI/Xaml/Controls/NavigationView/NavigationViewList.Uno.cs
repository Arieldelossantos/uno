// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

//
// This file is a C# translation of the NavigationViewList.cpp file from WinUI controls.
//

using System;
using Windows.System;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Uno.UI;

#if XAMARIN_IOS_UNIFIED
using UIKit;
#elif __MACOS__
using AppKit;
#endif

namespace Windows.UI.Xaml.Controls
{
	public  partial class NavigationViewList : ListView
	{
		// Workaround for https://github.com/unoplatform/uno/issues/2477
		//
		// In case a container is hosted in another container (if
		// materialized through a DataTemplate), forward some of the properties
		// to the original container.

		private bool _isTemplateOwnContainer;

		protected override void OnItemTemplateChanged(DataTemplate oldItemTemplate, DataTemplate newItemTemplate)
		{
			base.OnItemTemplateChanged(oldItemTemplate, newItemTemplate);

			_isTemplateOwnContainer = ItemTemplate?.LoadContent() is NavigationViewItemBase;
		}

		private bool IsTemplateOwnContainer => _isTemplateOwnContainer;

		private DependencyObject CreateOwnContainer()
		{
			return new ListViewItem()
			{
				Padding = Thickness.Empty,
				VerticalContentAlignment = VerticalAlignment.Stretch,
				HorizontalContentAlignment = HorizontalAlignment.Stretch,
			};
		}

		internal override int IndexFromContainerInner(DependencyObject container)
		{
			var index = base.IndexFromContainerInner(container);

			if(index == -1)
			{
				if(
					container is FrameworkElement fe
					&& fe.FindFirstParent<SelectorItem>() is SelectorItem parentItem
				)
				{
					index = base.IndexFromContainerInner(parentItem);
				}
			}

			return index;
		}
	}
}
