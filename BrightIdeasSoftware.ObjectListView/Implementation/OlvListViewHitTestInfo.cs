﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace BrightIdeasSoftware
{
	/// <summary>
	/// An indication of where a hit was within ObjectListView cell
	/// </summary>
	public enum HitTestLocation
	{
		/// <summary>
		/// Nowhere
		/// </summary>
		Nothing,

		/// <summary>
		/// On the text
		/// </summary>
		Text,

		/// <summary>
		/// On the image
		/// </summary>
		Image,

		/// <summary>
		/// On the checkbox
		/// </summary>
		CheckBox,

		/// <summary>
		/// On the expand button (TreeListView)
		/// </summary>
		ExpandButton,

		/// <summary>
		/// in a button (cell must have ButtonRenderer)
		/// </summary>
		Button,

		/// <summary>
		/// in the cell but not in any more specific location
		/// </summary>
		InCell,

		/// <summary>
		/// UserDefined location1 (used for custom renderers)
		/// </summary>
		UserDefined,

		/// <summary>
		/// On the expand/collapse widget of the group
		/// </summary>
		GroupExpander,

		/// <summary>
		/// Somewhere on a group
		/// </summary>
		Group,

		/// <summary>
		/// Somewhere in a column header
		/// </summary>
		Header,

		/// <summary>
		/// Somewhere in a column header checkbox
		/// </summary>
		HeaderCheckBox,

		/// <summary>
		/// Somewhere in a header divider
		/// </summary>
		HeaderDivider,
	}

	/// <summary>
	/// A collection of ListViewHitTest constants
	/// </summary>
	[Flags]
	public enum HitTestLocationEx
	{
		LVHT_NOWHERE = 0x00000001,
		LVHT_ONITEMICON = 0x00000002,
		LVHT_ONITEMLABEL = 0x00000004,
		LVHT_ONITEMSTATEICON = 0x00000008,
		LVHT_ONITEM = (LVHT_ONITEMICON | LVHT_ONITEMLABEL | LVHT_ONITEMSTATEICON),
		LVHT_ABOVE = 0x00000008,
		LVHT_BELOW = 0x00000010,
		LVHT_TORIGHT = 0x00000020,
		LVHT_TOLEFT = 0x00000040,
		LVHT_EX_GROUP_HEADER = 0x10000000,
		LVHT_EX_GROUP_FOOTER = 0x20000000,
		LVHT_EX_GROUP_COLLAPSE = 0x40000000,
		LVHT_EX_GROUP_BACKGROUND = -2147483648,
		LVHT_EX_GROUP_STATEICON = 0x01000000,
		LVHT_EX_GROUP_SUBSETLINK = 0x02000000,
		LVHT_EX_GROUP = (LVHT_EX_GROUP_BACKGROUND | LVHT_EX_GROUP_COLLAPSE | LVHT_EX_GROUP_FOOTER | LVHT_EX_GROUP_HEADER | LVHT_EX_GROUP_STATEICON | LVHT_EX_GROUP_SUBSETLINK),
		LVHT_EX_GROUP_MINUS_FOOTER_AND_BKGRD = (LVHT_EX_GROUP_COLLAPSE | LVHT_EX_GROUP_HEADER | LVHT_EX_GROUP_STATEICON | LVHT_EX_GROUP_SUBSETLINK),
		LVHT_EX_ONCONTENTS = 0x04000000,
		LVHT_EX_FOOTER = 0x08000000
	}

	/// <summary>
	/// Instances of this class encapsulate the information gathered during a OlvHitTest()
	/// operation.
	/// </summary>
	/// <remarks>Custom renderers can use HitTestLocation.UserDefined and the UserData
	/// object to store more specific locations for use during event handlers.</remarks>
	public class OlvListViewHitTestInfo
	{
		/// <summary>
		/// Create a OlvListViewHitTestInfo
		/// </summary>
		public OlvListViewHitTestInfo(OLVListItem olvListItem, OLVListSubItem subItem, int flags, int iColumn)
		{
			item = olvListItem;
			this.subItem = subItem;
			location = ConvertNativeFlagsToDotNetLocation(olvListItem, flags);
			HitTestLocationEx = (HitTestLocationEx)flags;
			ColumnIndex = iColumn;
			ListView = olvListItem == null ? null : (ObjectListView)olvListItem.ListView;

			switch (location)
			{
				case ListViewHitTestLocations.StateImage:
					HitTestLocation = HitTestLocation.CheckBox;
					break;
				case ListViewHitTestLocations.Image:
					HitTestLocation = HitTestLocation.Image;
					break;
				case ListViewHitTestLocations.Label:
					HitTestLocation = HitTestLocation.Text;
					break;
				default:
					if ((HitTestLocationEx & HitTestLocationEx.LVHT_EX_GROUP_COLLAPSE) == HitTestLocationEx.LVHT_EX_GROUP_COLLAPSE)
						HitTestLocation = HitTestLocation.GroupExpander;
					else if ((HitTestLocationEx & HitTestLocationEx.LVHT_EX_GROUP_MINUS_FOOTER_AND_BKGRD) != 0)
						HitTestLocation = HitTestLocation.Group;
					else
						HitTestLocation = HitTestLocation.Nothing;
					break;
			}
		}

		/// <summary>
		/// Create a OlvListViewHitTestInfo when the header was hit
		/// </summary>
		public OlvListViewHitTestInfo(ObjectListView olv, int iColumn, bool isOverCheckBox, int iDivider)
		{
			ListView = olv;
			ColumnIndex = iColumn;
			HeaderDividerIndex = iDivider;
			HitTestLocation = isOverCheckBox ? HitTestLocation.HeaderCheckBox : (iDivider < 0 ? HitTestLocation.Header : HitTestLocation.HeaderDivider);
		}

		private static ListViewHitTestLocations ConvertNativeFlagsToDotNetLocation(OLVListItem hitItem, int flags)
		{
			// Untangle base .NET behaviour.

			// In Windows SDK, the value 8 can have two meanings here: LVHT_ONITEMSTATEICON or LVHT_ABOVE.
			// .NET changes these to be:
			// - LVHT_ABOVE becomes ListViewHitTestLocations.AboveClientArea (which is 0x100).
			// - LVHT_ONITEMSTATEICON becomes ListViewHitTestLocations.StateImage (which is 0x200).
			// So, if we see the 8 bit set in flags, we change that to either a state image hit
			// (if we hit an item) or to AboveClientAream if nothing was hit.

			if ((8 & flags) == 8)
				return (ListViewHitTestLocations)(0xf7 & flags | (hitItem == null ? 0x100 : 0x200));

			// Mask off the LVHT_EX_XXXX values since ListViewHitTestLocations doesn't have them
			return (ListViewHitTestLocations)(flags & 0xffff);
		}

		#region Public fields

		/// <summary>
		/// Where is the hit location?
		/// </summary>
		public HitTestLocation HitTestLocation;

		/// <summary>
		/// Where is the hit location?
		/// </summary>
		public HitTestLocationEx HitTestLocationEx;

		/// <summary>
		/// Custom renderers can use this information to supply more details about the hit location
		/// </summary>
		public object UserData;

		#endregion

		#region Public read-only properties

		/// <summary>
		/// Gets the item that was hit
		/// </summary>
		public OLVListItem Item
		{
			get { return item; }
		}
		private OLVListItem item;

		/// <summary>
		/// Gets the subitem that was hit
		/// </summary>
		public OLVListSubItem SubItem
		{
			get { return subItem; }
			internal set { subItem = value; }
		}
		private OLVListSubItem subItem;

		/// <summary>
		/// Gets the part of the subitem that was hit
		/// </summary>
		public ListViewHitTestLocations Location
		{
			get { return location; }
			internal set { location = value; }
		}
		private ListViewHitTestLocations location;

		/// <summary>
		/// Gets the ObjectListView that was tested
		/// </summary>
		public ObjectListView ListView
		{
			get { return listView; }
			internal set { listView = value; }
		}
		private ObjectListView listView;

		/// <summary>
		/// Gets the model object that was hit
		/// </summary>
		public object RowObject
		{
			get
			{
				return Item == null ? null : Item.RowObject;
			}
		}

		/// <summary>
		/// Gets the index of the row under the hit point or -1
		/// </summary>
		public int RowIndex
		{
			get { return Item == null ? -1 : Item.Index; }
		}

		/// <summary>
		/// Gets the index of the column under the hit point
		/// </summary>
		public int ColumnIndex
		{
			get { return columnIndex; }
			internal set { columnIndex = value; }
		}
		private int columnIndex;

		/// <summary>
		/// Gets the index of the header divider
		/// </summary>
		public int HeaderDividerIndex
		{
			get { return headerDividerIndex; }
			internal set { headerDividerIndex = value; }
		}
		private int headerDividerIndex = -1;

		/// <summary>
		/// Gets the column that was hit
		/// </summary>
		public OLVColumn Column
		{
			get
			{
				int index = ColumnIndex;
				return index < 0 || ListView == null ? null : ListView.GetColumn(index);
			}
		}

		#endregion

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>
		/// A string that represents the current object.
		/// </returns>
		/// <filterpriority>2</filterpriority>
		public override string ToString()
		{
			return string.Format("HitTestLocation: {0}, HitTestLocationEx: {1}, Item: {2}, SubItem: {3}, Location: {4}, ColumnIndex: {5}",
				HitTestLocation, HitTestLocationEx, item, subItem, location, ColumnIndex);
		}

		internal class HeaderHitTestInfo
		{
			public int ColumnIndex;
			public bool IsOverCheckBox;
			public int OverDividerIndex;
		}
	}
}