using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace BrightIdeasSoftware
{
	/// <summary>
	/// An ObjectListView is a much easier to use, and much more powerful, version of the ListView.
	/// </summary>
	/// <remarks>
	/// <para>
	/// An ObjectListView automatically populates a ListView control with information taken 
	/// from a given collection of objects. It can do this because each column is configured
	/// to know which bit of the model object (the "aspect") it should be displaying. Columns similarly
	/// understand how to sort the list based on their aspect, and how to construct groups
	/// using their aspect.
	/// </para>
	/// <para>
	/// Aspects are extracted by giving the name of a method to be called or a
	/// property to be fetched. These names can be simple names or they can be dotted
	/// to chain property access e.g. "Owner.Address.Postcode".
	/// Aspects can also be extracted by installing a delegate.
	/// </para>
	/// <para>
	/// An ObjectListView can show a "this list is empty" message when there is nothing to show in the list, 
	/// so that the user knows the control is supposed to be empty.
	/// </para>
	/// <para>
	/// Right clicking on a column header should present a menu which can contain:
	/// commands (sort, group, ungroup); filtering; and column selection. Whether these
	/// parts of the menu appear is controlled by ShowCommandMenuOnRightClick, 
	/// ShowFilterMenuOnRightClick and SelectColumnsOnRightClick respectively.
	/// </para>
	/// <para>
	/// The groups created by an ObjectListView can be configured to include other formatting
	/// information, including a group icon, subtitle and task button. Using some undocumented
	/// interfaces, these groups can even on virtual lists.
	/// </para>
	/// <para>
	/// For these classes to build correctly, the project must have references to these assemblies:
	/// </para>
	/// <list type="bullet">
	/// <item><description>System</description></item>
	/// <item><description>System.Data</description></item>
	/// <item><description>System.Design</description></item>
	/// <item><description>System.Drawing</description></item>
	/// <item><description>System.Windows.Forms (obviously)</description></item>
	/// </list>
	/// </remarks>
	public partial class ObjectListView : ListView, ISupportInitialize
	{

		#region Life and death

		/// <summary>
		/// Create an ObjectListView
		/// </summary>
		public ObjectListView()
		{
			ColumnClick += new ColumnClickEventHandler(HandleColumnClick);
			Layout += new LayoutEventHandler(HandleLayout);
			ColumnWidthChanging += new ColumnWidthChangingEventHandler(HandleColumnWidthChanging);
			ColumnWidthChanged += new ColumnWidthChangedEventHandler(HandleColumnWidthChanged);

			base.View = View.Details;

			// Turn on owner draw so that we are responsible for our own fates (and isolated from bugs in the underlying ListView)
			OwnerDraw = true;

			DoubleBuffered = true; // kill nasty flickers. hiss... me hates 'em
			ShowSortIndicators = true;
		}

		/// <summary>
		/// Dispose of any resources this instance has been using
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (!disposing)
				return;
		}

		#endregion

		#region Static properties

		/// <summary>
		/// Gets whether or not the left mouse button is down at this very instant
		/// </summary>
		public static bool IsLeftMouseDown
		{
			get { return (Control.MouseButtons & MouseButtons.Left) == MouseButtons.Left; }
		}

		/// <summary>
		/// Gets whether the program running on Vista or later?
		/// </summary>
		static public bool IsVistaOrLater
		{
			get
			{
				if (!ObjectListView.sIsVistaOrLater.HasValue)
					ObjectListView.sIsVistaOrLater = Environment.OSVersion.Version.Major >= 6;
				return ObjectListView.sIsVistaOrLater.Value;
			}
		}
		static private bool? sIsVistaOrLater;

		/// <summary>
		/// Gets or sets how what smoothing mode will be applied to graphic operations.
		/// </summary>
		static public System.Drawing.Drawing2D.SmoothingMode SmoothingMode
		{
			get { return ObjectListView.sSmoothingMode; }
			set { ObjectListView.sSmoothingMode = value; }
		}
		static private System.Drawing.Drawing2D.SmoothingMode sSmoothingMode =
			System.Drawing.Drawing2D.SmoothingMode.HighQuality;

		/// <summary>
		/// Gets or sets how should text be renderered.
		/// </summary>
		static public System.Drawing.Text.TextRenderingHint TextRenderingHint
		{
			get { return ObjectListView.sTextRendereringHint; }
			set { ObjectListView.sTextRendereringHint = value; }
		}
		static private System.Drawing.Text.TextRenderingHint sTextRendereringHint =
			System.Drawing.Text.TextRenderingHint.SystemDefault;

		/// <summary>
		/// Convert the given enumerable into an ArrayList as efficiently as possible
		/// </summary>
		/// <param name="collection">The source collection</param>
		/// <param name="alwaysCreate">If true, this method will always create a new
		/// collection.</param>
		/// <returns>An ArrayList with the same contents as the given collection.</returns>
		/// <remarks>
		/// <para>When we move to .NET 3.5, we can use LINQ and not need this method.</para>
		/// </remarks>
		public static List<object> EnumerableToArray(IEnumerable<object> collection, bool alwaysCreate)
		{
			if (collection == null)
				return new List<object>();

			return collection.ToList();
		}


		/// <summary>
		/// Return the count of items in the given enumerable
		/// </summary>
		/// <param name="collection"></param>
		/// <returns></returns>
		/// <remarks>When we move to .NET 3.5, we can use LINQ and not need this method.</remarks>
		public static int EnumerableCount(IEnumerable<object> collection)
		{
			if (collection == null)
				return 0;

			return collection.Count();
		}

		/// <summary>
		/// Gets or sets whether the control will draw a rectangle in each cell showing the cell padding.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This can help with debugging display problems from cell padding.
		/// </para>
		/// <para>As with all cell padding, this setting only takes effect when the control is owner drawn.</para>
		/// </remarks>
		public static bool ShowCellPaddingBounds
		{
			get { return sShowCellPaddingBounds; }
			set { sShowCellPaddingBounds = value; }
		}
		private static bool sShowCellPaddingBounds;

		/// <summary>
		/// Gets the style that will be used by default to format disabled rows
		/// </summary>
		public static SimpleItemStyle DefaultDisabledItemStyle
		{
			get
			{
				if (sDefaultDisabledItemStyle == null)
				{
					sDefaultDisabledItemStyle = new SimpleItemStyle();
					sDefaultDisabledItemStyle.ForeColor = Color.DarkGray;
				}
				return sDefaultDisabledItemStyle;
			}
		}
		private static SimpleItemStyle sDefaultDisabledItemStyle;

		#endregion

		#region Public properties

		/// <summary>
		/// Get or set all the columns that this control knows about.
		/// Only those columns where IsVisible is true will be seen by the user.
		/// </summary>
		/// <remarks>
		/// <para>
		/// If you want to add new columns programmatically, add them to
		/// AllColumns and then call RebuildColumns(). Normally, you do not have to
		/// deal with this property directly. Just use the IDE.
		/// </para>
		/// <para>If you do add or remove columns from the AllColumns collection,
		/// you have to call RebuildColumns() to make those changes take effect.</para>
		/// </remarks>
		[Browsable(false),
		DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
		public virtual List<OLVColumn> AllColumns
		{
			get { return allColumns; }
			set { allColumns = value ?? new List<OLVColumn>(); }
		}
		private List<OLVColumn> allColumns = new List<OLVColumn>();

		/// <summary>
		/// This property forces the ObjectListView to always group items by the given column.
		/// </summary>
		[Browsable(false),
		DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public virtual OLVColumn AlwaysGroupByColumn
		{
			get { return alwaysGroupByColumn; }
			set { alwaysGroupByColumn = value; }
		}
		private OLVColumn alwaysGroupByColumn;

		/// <summary>
		/// If AlwaysGroupByColumn is not null, this property will be used to decide how
		/// those groups are sorted. If this property has the value SortOrder.None, then
		/// the sort order will toggle according to the users last header click.
		/// </summary>
		[Browsable(false),
		DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public virtual SortOrder AlwaysGroupBySortOrder
		{
			get { return alwaysGroupBySortOrder; }
			set { alwaysGroupBySortOrder = value; }
		}
		private SortOrder alwaysGroupBySortOrder = SortOrder.None;

		/// <summary>
		/// Give access to the image list that is actually being used by the control
		/// </summary>
		/// <remarks>
		/// Normally, it is preferable to use SmallImageList. Only use this property
		/// if you know exactly what you are doing.
		/// </remarks>
		[Browsable(false),
		DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public virtual ImageList BaseSmallImageList
		{
			get { return base.SmallImageList; }
			set { base.SmallImageList = value; }
		}

		/// <summary>
		/// How does the user indicate that they want to edit a cell?
		/// None means that the listview cannot be edited.
		/// </summary>
		/// <remarks>Columns can also be marked as editable.</remarks>
		[Category("ObjectListView"),
		 Description("How does the user indicate that they want to edit a cell?"),
		 DefaultValue(CellEditActivateMode.None)]
		public virtual CellEditActivateMode CellEditActivation
		{
			get { return cellEditActivation; }
			set
			{
				cellEditActivation = value;
				if (Created)
					Invalidate();
			}
		}
		private CellEditActivateMode cellEditActivation = CellEditActivateMode.None;

		/// <summary>
		/// When a cell is edited, should the whole cell be used (minus any space used by checkbox or image)?
		/// Defaults to true.
		/// </summary>
		/// <remarks>
		/// <para>This is always treated as true when the control is NOT owner drawn.</para>
		/// <para>
		/// When this is false and the control is owner drawn, 
		/// ObjectListView will try to calculate the width of the cell's
		/// actual contents, and then size the editing control to be just the right width. If this is true,
		/// the whole width of the cell will be used, regardless of the cell's contents.
		/// </para>
		/// <para>Each column can have a different value for property. This value from the control is only
		/// used when a column is not specified one way or another.</para>
		/// <para>Regardless of this setting, developers can specify the exact size of the editing control
		/// by listening for the CellEditStarting event.</para>
		/// </remarks>
		[Category("ObjectListView"),
		 Description("When a cell is edited, should the whole cell be used?"),
		 DefaultValue(true)]
		public virtual bool CellEditUseWholeCell
		{
			get { return cellEditUseWholeCell; }
			set { cellEditUseWholeCell = value; }
		}
		private bool cellEditUseWholeCell;

		/// <summary>
		/// Gets the tool tip control that shows tips for the cells
		/// </summary>
		[Browsable(false)]
		public ToolTipControl CellToolTip
		{
			get
			{
				if (cellToolTip == null)
				{
					CreateCellToolTip();
				}
				return cellToolTip;
			}
		}
		private ToolTipControl cellToolTip;

		/// <summary>
		/// Gets or sets how many pixels will be left blank around each cell of this item.
		/// Cell contents are aligned after padding has been taken into account.
		/// </summary>
		/// <remarks>
		/// <para>Each value of the given rectangle will be treated as an inset from
		/// the corresponding side. The width of the rectangle is the padding for the
		/// right cell edge. The height of the rectangle is the padding for the bottom
		/// cell edge.
		/// </para>
		/// <para>
		/// So, this.olv1.CellPadding = new Rectangle(1, 2, 3, 4); will leave one pixel
		/// of space to the left of the cell, 2 pixels at the top, 3 pixels of space
		/// on the right edge, and 4 pixels of space at the bottom of each cell.
		/// </para>
		/// <para>
		/// This setting only takes effect when the control is owner drawn.
		/// </para>
		/// <para>This setting only affects the contents of the cell. The background is
		/// not affected.</para>
		/// <para>If you set this to a foolish value, your control will appear to be empty.</para>
		/// </remarks>
		[Category("ObjectListView"),
		 Description("How much padding will be applied to each cell in this control?"),
		 DefaultValue(null)]
		public Rectangle? CellPadding
		{
			get { return cellPadding; }
			set { cellPadding = value; }
		}
		private Rectangle? cellPadding;

		/// <summary>
		/// Gets or sets how cells will be vertically aligned by default.
		/// </summary>
		/// <remarks>This setting only takes effect when the control is owner drawn. It will only be noticable
		/// when RowHeight has been set such that there is some vertical space in each row.</remarks>
		[Category("ObjectListView"),
		 Description("How will cell values be vertically aligned?"),
		 DefaultValue(StringAlignment.Center)]
		public virtual StringAlignment CellVerticalAlignment
		{
			get { return cellVerticalAlignment; }
			set { cellVerticalAlignment = value; }
		}
		private StringAlignment cellVerticalAlignment = StringAlignment.Center;

		/// <summary>
		/// Should this list show checkboxes?
		/// </summary>
		public new bool CheckBoxes
		{
			get { return base.CheckBoxes; }
			set
			{
				// Due to code in the base ListView class, turning off CheckBoxes on a virtual
				// list always throws an InvalidOperationException. We have to do some major hacking
				// to get around that
				if (VirtualMode)
				{
					// Leave virtual mode
					StateImageList = null;
					VirtualListSize = 0;
					VirtualMode = false;

					// Change the CheckBox setting while not in virtual mode
					base.CheckBoxes = value;

					// Reinstate virtual mode
					VirtualMode = true;

					// Re-enact the bits that we lost by switching to virtual mode
					ShowGroups = ShowGroups;
					BuildList(true);
				}
				else
				{
					base.CheckBoxes = value;
					// Initialize the state image list so we can display indetermined values.
					InitializeStateImageList();
				}
			}
		}

		/// <summary>
		/// Get or set the collection of model objects that are checked.
		/// When setting this property, any row whose model object isn't
		/// in the given collection will be unchecked. Setting to null is
		/// equivilent to unchecking all.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This property returns a simple collection. Changes made to the returned
		/// collection do NOT affect the list. This is different to the behaviour of
		/// CheckedIndicies collection.
		/// </para>
		/// <para>
		/// .NET's CheckedItems property is not helpful. It is just a short-hand for
		/// iterating through the list looking for items that are checked.
		/// </para>
		/// <para>
		/// The performance of the get method is O(n), where n is the number of items
		/// in the control. The performance of the set method is
		/// O(n + m) where m is the number of objects being checked. Be careful on long lists.
		/// </para>
		/// </remarks>
		[Browsable(false),
		 DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public virtual IList<object> CheckedObjects
		{
			get
			{
				var list = new List<object>();
				if (CheckBoxes)
				{
					for (int i = 0; i < GetItemCount(); i++)
					{
						OLVListItem olvi = GetItem(i);
						if (olvi.CheckState == CheckState.Checked)
							list.Add(olvi.RowObject);
					}
				}
				return list;
			}
			set
			{
				if (!CheckBoxes)
					return;

				// Set up an efficient way of testing for the presence of a particular model
				var table = new Dictionary<object, bool>(GetItemCount());
				if (value != null)
				{
					foreach (object x in value)
						table[x] = true;
				}

				BeginUpdate();
				foreach (var x in Objects)
				{
					SetObjectCheckedness(x, table.ContainsKey(x) ? CheckState.Checked : CheckState.Unchecked);
				}
				EndUpdate();
			}
		}

		/// <summary>
		/// Gets Columns for this list. We hide the original so we can associate
		/// a specialised editor with it.
		/// </summary>
		[Editor("BrightIdeasSoftware.Design.OLVColumnCollectionEditor", "System.Drawing.Design.UITypeEditor")]
		new public ListView.ColumnHeaderCollection Columns
		{
			get
			{
				return base.Columns;
			}
		}

		/// <summary>
		/// Return the visible columns in the order they are displayed to the user
		/// </summary>
		[Browsable(false)]
		public virtual List<OLVColumn> ColumnsInDisplayOrder
		{
			get
			{
				OLVColumn[] columnsInDisplayOrder = new OLVColumn[Columns.Count];
				foreach (OLVColumn col in Columns)
				{
					columnsInDisplayOrder[col.DisplayIndex] = col;
				}
				return new List<OLVColumn>(columnsInDisplayOrder);
			}
		}

		/// <summary>
		/// When owner drawing, this renderer will draw columns that do not have specific renderer
		/// given to them
		/// </summary>
		/// <remarks>If you try to set this to null, it will revert to a HighlightTextRenderer</remarks>
		[Browsable(false),
		 DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public IRenderer DefaultRenderer
		{
			get { return defaultRenderer; }
			set { defaultRenderer = value ?? new HighlightTextRenderer(); }
		}
		private IRenderer defaultRenderer = new HighlightTextRenderer();

		/// <summary>
		/// Get the renderer to be used to draw the given cell.
		/// </summary>
		/// <param name="model">The row model for the row</param>
		/// <param name="column">The column to be drawn</param>
		/// <returns>The renderer used for drawing a cell. Must not return null.</returns>
		public IRenderer GetCellRenderer(object model, OLVColumn column)
		{
			return column.Renderer ?? DefaultRenderer;
		}

		/// <summary>
		/// Gets or sets the style that will be applied to disabled items.
		/// </summary>
		/// <remarks>If this is not set explicitly, <see cref="ObjectListView.DefaultDisabledItemStyle"/>  will be used.</remarks>
		[Category("ObjectListView"),
		Description("The style that will be applied to disabled items"),
		DefaultValue(null)]
		public SimpleItemStyle DisabledItemStyle
		{
			get { return disabledItemStyle; }
			set { disabledItemStyle = value; }
		}
		private SimpleItemStyle disabledItemStyle;

		/// <summary>
		/// Gets or sets the list of model objects that are disabled.
		/// Disabled objects cannot be selected or activated.
		/// </summary>
		[Browsable(false),
		DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public virtual IEnumerable<object> DisabledObjects
		{
			get
			{
				return disabledObjects;
			}
			set
			{
				disabledObjects.Clear();
				DisableObjects(value);
			}
		}
		private readonly HashSet<object> disabledObjects = new HashSet<object>();

		/// <summary>
		/// Is this given model object disabled?
		/// </summary>
		/// <param name="model"></param>
		/// <returns></returns>
		public bool IsDisabled(object model)
		{
			return model != null && disabledObjects.Contains(model);
		}

		/// <summary>
		/// Disable all the given model objects
		/// </summary>
		/// <param name="models"></param>
		public void DisableObjects(IEnumerable<object> models)
		{
			if (models == null)
				return;

			foreach (object model in models)
			{
				if (model == null)
					continue;

				disabledObjects.Add(model);
				int modelIndex = IndexOf(model);
				if (modelIndex >= 0)
					NativeMethods.DeselectOneItem(this, modelIndex);
			}
			RefreshObjects(models.ToList());
		}

		/// <summary>
		/// Enable all the given model objects
		/// </summary>
		/// <param name="models"></param>
		public void EnableObjects(IEnumerable<object> models)
		{
			if (models == null)
				return;
			var list = EnumerableToArray(models, false);
			foreach (object model in list)
			{
				if (model != null)
					disabledObjects.Remove(model);
			}
			RefreshObjects(list);
		}

		/// <summary>
		/// Forget all disabled objects. This does not trigger a redraw or rebuild
		/// </summary>
		protected void ClearDisabledObjects()
		{
			disabledObjects.Clear();
		}

		/// <summary>
		/// Gets the header control for the ListView
		/// </summary>
		[Browsable(false)]
		public HeaderControl HeaderControl
		{
			get { return headerControl ?? (headerControl = new HeaderControl(this)); }
		}
		private HeaderControl headerControl;

		/// <summary>
		/// Gets or sets the style that will be used to draw the columm headers of the listview
		/// </summary>
		/// <remarks>
		/// <para>
		/// This is only used when HeaderUsesThemes is false.
		/// </para>
		/// <para>
		/// Individual columns can override this through their HeaderFormatStyle property.
		/// </para>
		/// </remarks>
		[Category("ObjectListView"),
		 Description("What style will be used to draw the control's header"),
		 DefaultValue(null)]
		public HeaderFormatStyle HeaderFormatStyle
		{
			get { return headerFormatStyle; }
			set { headerFormatStyle = value; }
		}
		private HeaderFormatStyle headerFormatStyle;

		/// <summary>
		/// Gets or sets the maximum height of the header. -1 means no maximum.
		/// </summary>
		[Category("ObjectListView"),
		 Description("What is the maximum height of the header? -1 means no maximum"),
		 DefaultValue(-1)]
		public int HeaderMaximumHeight
		{
			get { return headerMaximumHeight; }
			set { headerMaximumHeight = value; }
		}
		private int headerMaximumHeight = -1;

		/// <summary>
		/// Gets or sets the minimum height of the header. -1 means no minimum.
		/// </summary>
		[Category("ObjectListView"),
		 Description("What is the minimum height of the header? -1 means no minimum"),
		 DefaultValue(-1)]
		public int HeaderMinimumHeight
		{
			get { return headerMinimumHeight; }
			set { headerMinimumHeight = value; }
		}
		private int headerMinimumHeight = -1;

		/// <summary>
		/// Gets or sets whether the header will be drawn strictly according to the OS's theme. 
		/// </summary>
		/// <remarks>
		/// <para>
		/// If this is set to true, the header will be rendered completely by the system, without
		/// any of ObjectListViews fancy processing -- no images in header, no filter indicators,
		/// no word wrapping, no header styling, no checkboxes.
		/// </para>
		/// <para>If this is set to false, ObjectListView will render the header as it thinks best.
		/// If no special features are required, then ObjectListView will delegate rendering to the OS.
		/// Otherwise, ObjectListView will draw the header according to the configuration settings.
		/// </para>
		/// <para>
		/// The effect of not being themed will be different from OS to OS. At
		/// very least, the sort indicator will not be standard. 
		/// </para>
		/// </remarks>
		[Category("ObjectListView"),
		 Description("Will the column headers be drawn strictly according to OS theme?"),
		 DefaultValue(false)]
		public bool HeaderUsesThemes
		{
			get { return headerUsesThemes; }
			set { headerUsesThemes = value; }
		}
		private bool headerUsesThemes;

		/// <summary>
		/// Gets or sets the whether the text in the header will be word wrapped.
		/// </summary>
		/// <remarks>
		/// <para>Line breaks will be applied between words. Words that are too long
		/// will still be ellipsed.</para>
		/// <para>
		/// As with all settings that make the header look different, HeaderUsesThemes must be set to false, otherwise
		/// the OS will be responsible for drawing the header, and it does not allow word wrapped text.
		/// </para>
		/// </remarks>
		[Category("ObjectListView"),
		 Description("Will the text of the column headers be word wrapped?"),
		 DefaultValue(false)]
		public bool HeaderWordWrap
		{
			get { return headerWordWrap; }
			set
			{
				headerWordWrap = value;
				if (headerControl != null)
					headerControl.WordWrap = value;
			}
		}
		private bool headerWordWrap;

		/// <summary>
		/// What color should be used for the background of selected rows?
		/// </summary>
		[Category("ObjectListView"),
		 Description("The background of selected rows when the control is owner drawn"),
		 DefaultValue(typeof(Color), "")]
		public virtual Color SelectedBackColor
		{
			get { return selectedBackColor; }
			set { selectedBackColor = value; }
		}
		private Color selectedBackColor = Color.Empty;

		/// <summary>
		/// Return the color should be used for the background of selected rows or a reasonable default
		/// </summary>
		[Browsable(false)]
		public virtual Color SelectedBackColorOrDefault
		{
			get
			{
				return SelectedBackColor.IsEmpty ? SystemColors.Highlight : SelectedBackColor;
			}
		}

		/// <summary>
		/// What color should be used for the foreground of selected rows?
		/// </summary>
		[Category("ObjectListView"),
		 Description("The foreground color of selected rows (when the control is owner drawn)"),
		 DefaultValue(typeof(Color), "")]
		public virtual Color SelectedForeColor
		{
			get { return selectedForeColor; }
			set { selectedForeColor = value; }
		}
		private Color selectedForeColor = Color.Empty;

		/// <summary>
		/// Return the color should be used for the foreground of selected rows or a reasonable default
		/// </summary>
		[Browsable(false)]
		public virtual Color SelectedForeColorOrDefault
		{
			get
			{
				return SelectedForeColor.IsEmpty ? SystemColors.HighlightText : SelectedForeColor;
			}
		}

		/// <summary>
		/// Return true if a cell edit operation is currently happening
		/// </summary>
		[Browsable(false)]
		public virtual bool IsCellEditing
		{
			get { return cellEditor != null; }
		}

		/// <summary>
		/// Return true if the ObjectListView is being used within the development environment.
		/// </summary>
		[Browsable(false)]
		public virtual bool IsDesignMode
		{
			get { return DesignMode; }
		}

		/// <summary>
		/// When the user types into a list, should the values in the current sort column be searched to find a match?
		/// If this is false, the primary column will always be used regardless of the sort column.
		/// </summary>
		/// <remarks>When this is true, the behavior is like that of ITunes.</remarks>
		[Category("ObjectListView"),
		Description("When the user types into a list, should the values in the current sort column be searched to find a match?"),
		DefaultValue(true)]
		public virtual bool IsSearchOnSortColumn
		{
			get { return isSearchOnSortColumn; }
			set { isSearchOnSortColumn = value; }
		}
		private bool isSearchOnSortColumn = true;

		/// <summary>
		/// Hide the Items collection so it's not visible in the Properties grid.
		/// </summary>
		[Browsable(false),
		 DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		new public ListViewItemCollection Items
		{
			get { return base.Items; }
		}

		/// <summary>
		/// This renderer draws the items when in the list is in non-details view.
		/// In details view, the renderers for the individuals columns are responsible.
		/// </summary>
		[Category("ObjectListView"),
		Description("The owner drawn renderer that draws items when the list is in non-Details view."),
		DefaultValue(null)]
		public IRenderer ItemRenderer
		{
			get { return itemRenderer; }
			set { itemRenderer = value; }
		}
		private IRenderer itemRenderer;

		/// <summary>
		/// Which column did we last sort by
		/// </summary>
		/// <remarks>This is an alias for PrimarySortColumn</remarks>
		[Browsable(false),
		DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public virtual OLVColumn LastSortColumn
		{
			get { return PrimarySortColumn; }
			set { PrimarySortColumn = value; }
		}

		/// <summary>
		/// Which direction did we last sort
		/// </summary>
		/// <remarks>This is an alias for PrimarySortOrder</remarks>
		[Browsable(false),
		DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public virtual SortOrder LastSortOrder
		{
			get { return PrimarySortOrder; }
			set { PrimarySortOrder = value; }
		}

		/// <summary>
		/// Gets the hit test info last time the mouse was moved.
		/// </summary>
		/// <remarks>Useful for hot item processing.</remarks>
		[Browsable(false),
		DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public virtual OlvListViewHitTestInfo MouseMoveHitTest
		{
			get { return mouseMoveHitTest; }
			private set { mouseMoveHitTest = value; }
		}
		private OlvListViewHitTestInfo mouseMoveHitTest;

		/// <summary>
		/// Gets or sets whether the user wants to owner draw the header control
		/// themselves. If this is false (the default), ObjectListView will use
		/// custom drawing to render the header, if needed.
		/// </summary>
		/// <remarks>
		/// If you listen for the DrawColumnHeader event, you need to set this to true,
		/// otherwise your event handler will not be called.
		/// </remarks>
		[Category("ObjectListView"),
		 Description("Should the DrawColumnHeader event be triggered"),
		 DefaultValue(false)]
		public bool OwnerDrawnHeader
		{
			get { return ownerDrawnHeader; }
			set { ownerDrawnHeader = value; }
		}
		private bool ownerDrawnHeader;

		/// <summary>
		/// Get/set the collection of objects that this list will show
		/// </summary>
		/// <remarks>
		/// <para>
		/// The contents of the control will be updated immediately after setting this property.
		/// </para>
		/// <para>The property DOES work on virtual lists: setting is problem-free, but if you try to get it
		/// and the list has 10 million objects, it may take some time to return.</para>
		/// </remarks>
		[Browsable(false),
		 DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public virtual IEnumerable<object> Objects
		{
			get { return objects; }
			set { SetObjects(value, true); }
		}
		private IEnumerable<object> objects;

		/// <summary>
		/// Gets or sets whether the ObjectListView will be owner drawn. Defaults to true.
		/// </summary>
		/// <remarks>
		/// <para>
		/// When this is true, all of ObjectListView's neat features are available.
		/// </para>
		/// <para>We have to reimplement this property, even though we just call the base
		/// property, in order to change the [DefaultValue] to true.
		/// </para>
		/// </remarks>
		[Category("Appearance"),
		 Description("Should the ListView do its own rendering"),
		 DefaultValue(true)]
		public new bool OwnerDraw
		{
			get { return base.OwnerDraw; }
			set { base.OwnerDraw = value; }
		}

		/// <summary>
		/// Gets or sets a dictionary that remembers the check state of model objects
		/// </summary>
		/// <remarks>This is used when PersistentCheckBoxes is true and for virtual lists.</remarks>
		protected Dictionary<object, CheckState> CheckStateMap
		{
			get { return checkStateMap ?? (checkStateMap = new Dictionary<object, CheckState>()); }
			set { checkStateMap = value; }
		}
		private Dictionary<object, CheckState> checkStateMap;

		/// <summary>
		/// Which column did we last sort by
		/// </summary>
		[Browsable(false),
		DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public virtual OLVColumn PrimarySortColumn
		{
			get { return primarySortColumn; }
			set
			{
				primarySortColumn = value;
			}
		}
		private OLVColumn primarySortColumn;

		/// <summary>
		/// Which direction did we last sort
		/// </summary>
		[Browsable(false),
		DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public virtual SortOrder PrimarySortOrder
		{
			get { return primarySortOrder; }
			set { primarySortOrder = value; }
		}
		private SortOrder primarySortOrder;

		/// <summary>
		/// Specify the height of each row in the control in pixels.
		/// </summary>
		/// <remarks><para>The row height in a listview is normally determined by the font size and the small image list size.
		/// This setting allows that calculation to be overridden (within reason: you still cannot set the line height to be
		/// less than the line height of the font used in the control). </para>
		/// <para>Setting it to -1 means use the normal calculation method.</para>
		/// <para><bold>This feature is experiemental!</bold> Strange things may happen to your program,
		/// your spouse or your pet if you use it.</para>
		/// </remarks>
		[Category("ObjectListView"),
		 Description("Specify the height of each row in pixels. -1 indicates default height"),
		 DefaultValue(-1)]
		public virtual int RowHeight
		{
			get { return rowHeight; }
			set
			{
				if (value < 1)
					rowHeight = -1;
				else
					rowHeight = value;
				if (DesignMode)
					return;
				SetupBaseImageList();
				if (CheckBoxes)
					InitializeStateImageList();
			}
		}
		private int rowHeight = -1;

		/// <summary>
		/// How many pixels high is each row?
		/// </summary>
		[Browsable(false)]
		public virtual int RowHeightEffective
		{
			get
			{
				switch (View)
				{
					case View.List:
					case View.SmallIcon:
					case View.Details:
						return Math.Max(SmallImageSize.Height, Font.Height);

					case View.Tile:
						return TileSize.Height;

					case View.LargeIcon:
						if (LargeImageList == null)
							return Font.Height;

						return Math.Max(LargeImageList.ImageSize.Height, Font.Height);

					default:
						// This should never happen
						return 0;
				}
			}
		}

		/// <summary>
		/// How many rows appear on each page of this control
		/// </summary>
		[Browsable(false)]
		public virtual int RowsPerPage
		{
			get
			{
				return NativeMethods.GetCountPerPage(this);
			}
		}

		/// <summary>
		/// Get/set the column that will be used to resolve comparisons that are equal when sorting.
		/// </summary>
		/// <remarks>There is no user interface for this setting. It must be set programmatically.</remarks>
		[Browsable(false),
		 DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public virtual OLVColumn SecondarySortColumn
		{
			get { return secondarySortColumn; }
			set { secondarySortColumn = value; }
		}
		private OLVColumn secondarySortColumn;

		/// <summary>
		/// When the SecondarySortColumn is used, in what order will it compare results?
		/// </summary>
		[Browsable(false),
		DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public virtual SortOrder SecondarySortOrder
		{
			get { return secondarySortOrder; }
			set { secondarySortOrder = value; }
		}
		private SortOrder secondarySortOrder = SortOrder.None;

		/// <summary>
		/// Gets or sets the column that is drawn with a slight tint.
		/// </summary>
		/// <remarks>
		/// <para>
		/// If TintSortColumn is true, the sort column will automatically
		/// be made the selected column.
		/// </para>
		/// <para>
		/// The colour of the tint is controlled by SelectedColumnTint.
		/// </para>
		/// </remarks>
		[Browsable(false),
		DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public OLVColumn SelectedColumn
		{
			get { return selectedColumn; }
			set
			{
				selectedColumn = value;
			}
		}
		private OLVColumn selectedColumn;

		/// <summary>
		/// Gets or sets the index of the row that is currently selected. 
		/// When getting the index, if no row is selected,or more than one is selected, return -1.
		/// </summary>
		[Browsable(false),
		 DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public virtual int SelectedIndex
		{
			get { return SelectedIndices.Count == 1 ? SelectedIndices[0] : -1; }
			set
			{
				SelectedIndices.Clear();
				if (value >= 0 && value < Items.Count)
					SelectedIndices.Add(value);
			}
		}

		/// <summary>
		/// Gets or sets the ListViewItem that is currently selected . If no row is selected, or more than one is selected, return null.
		/// </summary>
		[Browsable(false),
		 DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public virtual OLVListItem SelectedItem
		{
			get
			{
				return SelectedIndices.Count == 1 ? GetItem(SelectedIndices[0]) : null;
			}
			set
			{
				SelectedIndices.Clear();
				if (value != null)
					SelectedIndices.Add(value.Index);
			}
		}

		/// <summary>
		/// Gets the model object from the currently selected row, if there is only one row selected. 
		/// If no row is selected, or more than one is selected, returns null.
		/// When setting, this will select the row that is displaying the given model object and focus on it. 
		/// All other rows are deselected.
		/// </summary>
		[Browsable(false),
		 DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public virtual object SelectedObject
		{
			get
			{
				return SelectedIndices.Count == 1 ? GetModelObject(SelectedIndices[0]) : null;
			}
			set
			{
				// If the given model is already selected, don't do anything else (prevents an flicker)
				object selectedObject = SelectedObject;
				if (selectedObject != null && selectedObject.Equals(value))
					return;

				SelectedIndices.Clear();
				SelectObject(value, true);
			}
		}

		/// <summary>
		/// Get the model objects from the currently selected rows. If no row is selected, the returned List will be empty.
		/// When setting this value, select the rows that is displaying the given model objects. All other rows are deselected.
		/// </summary>
		[Browsable(false),
		 DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public virtual IList<object> SelectedObjects
		{
			get
			{
				return SelectedIndices.Cast<int>().Select(i => GetModelObject(i)).ToList();
			}
			set
			{
				SelectedIndices.Clear();
				SelectObjects(value);
			}
		}

		/// <summary>
		/// Should the list view show a bitmap in the column header to show the sort direction?
		/// </summary>
		/// <remarks>
		/// The only reason for not wanting to have sort indicators is that, on pre-XP versions of
		/// Windows, having sort indicators required the ListView to have a small image list, and
		/// as soon as you give a ListView a SmallImageList, the text of column 0 is bumped 16
		/// pixels to the right, even if you never used an image.
		/// </remarks>
		[Category("ObjectListView"),
		 Description("Should the list view show sort indicators in the column headers?"),
		 DefaultValue(true)]
		public virtual bool ShowSortIndicators
		{
			get { return showSortIndicators; }
			set { showSortIndicators = value; }
		}
		private bool showSortIndicators;

		/// <summary>
		/// Should the list view show images on subitems?
		/// </summary>
		/// <remarks>
		/// <para>Virtual lists have to be owner drawn in order to show images on subitems</para>
		/// </remarks>
		[Category("ObjectListView"),
		 Description("Should the list view show images on subitems?"),
		 DefaultValue(false)]
		public virtual bool ShowImagesOnSubItems
		{
			get { return showImagesOnSubItems; }
			set
			{
				showImagesOnSubItems = value;
				if (Created)
					ApplyExtendedStyles();
				if (value && VirtualMode)
					OwnerDraw = true;
			}
		}
		private bool showImagesOnSubItems;

		/// <summary>
		/// Override the SmallImageList property so we can correctly shadow its operations.
		/// </summary>
		/// <remarks><para>If you use the RowHeight property to specify the row height, the SmallImageList
		/// must be fully initialised before setting/changing the RowHeight. If you add new images to the image
		/// list after setting the RowHeight, you must assign the imagelist to the control again. Something as simple
		/// as this will work:
		/// <code>listView1.SmallImageList = listView1.SmallImageList;</code></para>
		/// </remarks>
		new public ImageList SmallImageList
		{
			get { return shadowedImageList; }
			set
			{
				shadowedImageList = value;
				if (UseSubItemCheckBoxes)
					SetupSubItemCheckBoxes();
				SetupBaseImageList();
			}
		}
		private ImageList shadowedImageList;

		/// <summary>
		/// Return the size of the images in the small image list or a reasonable default
		/// </summary>
		[Browsable(false)]
		public virtual Size SmallImageSize
		{
			get
			{
				return BaseSmallImageList == null ? new Size(16, 16) : BaseSmallImageList.ImageSize;
			}
		}

		/// <summary>
		/// Should each row have a tri-state checkbox?
		/// </summary>
		/// <remarks>
		/// If this is true, the user can choose the third state (normally Indeterminate). Otherwise, user clicks
		/// alternate between checked and unchecked. CheckStateGetter can still return Indeterminate when this
		/// setting is false.
		/// </remarks>
		[Category("ObjectListView"),
		 Description("Should the primary column have a checkbox that behaves as a tri-state checkbox?"),
		 DefaultValue(false)]
		public virtual bool TriStateCheckBoxes
		{
			get { return triStateCheckBoxes; }
			set
			{
				triStateCheckBoxes = value;
				if (value && !CheckBoxes)
					CheckBoxes = true;
				InitializeStateImageList();
			}
		}
		private bool triStateCheckBoxes;

		/// <summary>
		/// Get or set the index of the top item of this listview
		/// </summary>
		/// <remarks>
		/// <para>
		/// This property only works when the listview is in Details view and not showing groups.
		/// </para>
		/// <para>
		/// The reason that it does not work when showing groups is that, when groups are enabled,
		/// the Windows msg LVM_GETTOPINDEX always returns 0, regardless of the
		/// scroll position.
		/// </para>
		/// </remarks>
		[Browsable(false),
		DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public virtual int TopItemIndex
		{
			get
			{
				if (View == View.Details && IsHandleCreated)
					return NativeMethods.GetTopIndex(this);

				return -1;
			}
			set
			{
				int newTopIndex = Math.Min(value, GetItemCount() - 1);
				if (View != View.Details || newTopIndex < 0)
					return;

				try
				{
					TopItem = Items[newTopIndex];

					// Setting the TopItem sometimes gives off by one errors,
					// that (bizarrely) are correct on a second attempt
					if (TopItem != null && TopItem.Index != newTopIndex)
						TopItem = GetItem(newTopIndex);
				}
				catch (NullReferenceException)
				{
					// There is a bug in the .NET code where setting the TopItem
					// will sometimes throw null reference exceptions
					// There is nothing we can do to get around it.
				}
			}
		}

		/// <summary>
		/// Gets or sets whether moving the mouse over the header will trigger CellOver events.
		/// Defaults to true.
		/// </summary>
		/// <remarks>
		/// Moving the mouse over the header did not previously trigger CellOver events, since the
		/// header is considered a separate control. 
		/// If this change in behaviour causes your application problems, set this to false.
		/// If you are interested in knowing when the mouse moves over the header, set this property to true (the default).
		/// </remarks>
		[Category("ObjectListView"),
		 Description("Should moving the mouse over the header trigger CellOver events?"),
		 DefaultValue(true)]
		public bool TriggerCellOverEventsWhenOverHeader
		{
			get { return triggerCellOverEventsWhenOverHeader; }
			set { triggerCellOverEventsWhenOverHeader = value; }
		}
		private bool triggerCellOverEventsWhenOverHeader = true;

		/// <summary>
		/// When resizing a column by dragging its divider, should any space filling columns be
		/// resized at each mouse move? If this is false, the filling columns will be
		/// updated when the mouse is released.
		/// </summary>
		/// <remarks>
		/// <para>
		/// If you have a space filling column
		/// is in the left of the column that is being resized, this will look odd: 
		/// the right edge of the column will be dragged, but
		/// its <b>left</b> edge will move since the space filling column is shrinking.
		/// </para>
		/// <para>This is logical behaviour -- it just looks wrong.   
		/// </para>
		/// <para>
		/// Given the above behavior is probably best to turn this property off if your space filling
		/// columns aren't the right-most columns.</para>
		/// </remarks>
		[Category("ObjectListView"),
		Description("When resizing a column by dragging its divider, should any space filling columns be resized at each mouse move?"),
		DefaultValue(true)]
		public virtual bool UpdateSpaceFillingColumnsWhenDraggingColumnDivider
		{
			get { return updateSpaceFillingColumnsWhenDraggingColumnDivider; }
			set { updateSpaceFillingColumnsWhenDraggingColumnDivider = value; }
		}
		private bool updateSpaceFillingColumnsWhenDraggingColumnDivider = true;

		/// <summary>
		/// Should this control be configured to show check boxes on subitems?
		/// </summary>
		/// <remarks>If this is set to True, the control will be given a SmallImageList if it
		/// doesn't already have one. Also, if it is a virtual list, it will be set to owner
		/// drawn, since virtual lists can't draw check boxes without being owner drawn.</remarks>
		[Category("ObjectListView"),
		 Description("Should this control be configured to show check boxes on subitems."),
		 DefaultValue(false)]
		public bool UseSubItemCheckBoxes
		{
			get { return useSubItemCheckBoxes; }
			set
			{
				useSubItemCheckBoxes = value;
				if (value)
					SetupSubItemCheckBoxes();
			}
		}
		private bool useSubItemCheckBoxes;

		/// <summary>
		/// Get/set the style of view that this listview is using
		/// </summary>
		/// <remarks>Switching to tile or details view installs the columns appropriate to that view.
		/// Confusingly, in tile view, every column is shown as a row of information.</remarks>
		[Category("Appearance"),
		 Description("Select the layout of the items within this control)"),
		 DefaultValue(null)]
		new public View View
		{
			get { return base.View; }
			set
			{
				base.View = value;
				SetupBaseImageList();
			}
		}

		#endregion

		#region Callbacks

		/// <summary>
		/// Gets or sets whether ObjectListView can rely on Application.Idle events
		/// being raised.
		/// </summary>
		/// <remarks>In some host environments (e.g. when running as an extension within
		/// VisualStudio and possibly Office), Application.Idle events are never raised.
		/// Set this to false when Idle events will not be raised, and ObjectListView will
		/// raise those events itself.
		/// </remarks>
		[Browsable(false),
		DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public virtual bool CanUseApplicationIdle
		{
			get { return canUseApplicationIdle; }
			set { canUseApplicationIdle = value; }
		}
		private bool canUseApplicationIdle = true;

		/// <summary>
		/// This delegate is called when the list wants to show a tooltip for a particular cell.
		/// The delegate should return the text to display, or null to use the default behavior
		/// (which is to show the full text of truncated cell values).
		/// </summary>
		/// <remarks>
		/// Displaying the full text of truncated cell values only work for FullRowSelect listviews.
		/// This is MS's behavior, not mine. Don't complain to me :)
		/// </remarks>
		[Browsable(false),
		 DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public virtual CellToolTipGetterDelegate CellToolTipGetter
		{
			get { return cellToolTipGetter; }
			set { cellToolTipGetter = value; }
		}
		private CellToolTipGetterDelegate cellToolTipGetter;

		/// <summary>
		/// The name of the property (or field) that holds whether or not a model is checked.
		/// </summary>
		/// <remarks>
		/// <para>The property be modifiable. It must have a return type of bool (or of bool? if
		/// TriStateCheckBoxes is true).</para>
		/// <para>Setting this property replaces any CheckStateGetter or CheckStatePutter that have been installed.
		/// Conversely, later setting the CheckStateGetter or CheckStatePutter properties will take precedence
		/// over the behavior of this property.</para>
		/// </remarks>
		[Category("ObjectListView"),
		 Description("The name of the property or field that holds the 'checkedness' of the model"),
		 DefaultValue(null)]
		public virtual string CheckedAspectName
		{
			get { return checkedAspectName; }
			set
			{
				checkedAspectName = value;
				if (string.IsNullOrEmpty(checkedAspectName))
				{
					checkedAspectMunger = null;
					CheckStateGetter = null;
					CheckStatePutter = null;
				}
				else
				{
					checkedAspectMunger = new Munger(checkedAspectName);
					CheckStateGetter = delegate (object modelObject)
					{
						bool? result = checkedAspectMunger.GetValue(modelObject) as bool?;
						if (result.HasValue)
							return result.Value ? CheckState.Checked : CheckState.Unchecked;
						return TriStateCheckBoxes ? CheckState.Indeterminate : CheckState.Unchecked;
					};
					CheckStatePutter = delegate (object modelObject, CheckState newValue)
					{
						if (TriStateCheckBoxes && newValue == CheckState.Indeterminate)
							checkedAspectMunger.PutValue(modelObject, null);
						else
							checkedAspectMunger.PutValue(modelObject, newValue == CheckState.Checked);
						return CheckStateGetter(modelObject);
					};
				}
			}
		}
		private string checkedAspectName;
		private Munger checkedAspectMunger;

		/// <summary>
		/// This delegate will be called whenever the ObjectListView needs to know the check state
		/// of the row associated with a given model object.
		/// </summary>
		/// <remarks>
		/// <para>.NET has no support for indeterminate values, but as of v2.0, this class allows
		/// indeterminate values.</para>
		/// </remarks>
		[Browsable(false),
		 DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public virtual CheckStateGetterDelegate CheckStateGetter
		{
			get { return checkStateGetter; }
			set { checkStateGetter = value; }
		}
		private CheckStateGetterDelegate checkStateGetter;

		/// <summary>
		/// This delegate will be called whenever the user tries to change the check state of a row.
		/// The delegate should return the state that was actually set, which may be different
		/// to the state given.
		/// </summary>
		[Browsable(false),
		 DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public virtual CheckStatePutterDelegate CheckStatePutter
		{
			get { return checkStatePutter; }
			set { checkStatePutter = value; }
		}
		private CheckStatePutterDelegate checkStatePutter;

		/// <summary>
		/// This delegate can be used to sort the table in a custom fasion.
		/// </summary>
		/// <remarks>
		/// <para>
		/// The delegate must install a ListViewItemSorter on the ObjectListView.
		/// Installing the ItemSorter does the actual work of sorting the ListViewItems.
		/// See ColumnComparer in the code for an example of what an ItemSorter has to do.
		/// </para>
		/// <para>
		/// Do not install a CustomSorter on a VirtualObjectListView. Override the SortObjects()
		/// method of the IVirtualListDataSource instead.
		/// </para>
		/// </remarks>
		[Browsable(false),
		 DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public virtual SortDelegate CustomSorter
		{
			get { return customSorter; }
			set { customSorter = value; }
		}
		private SortDelegate customSorter;

		#endregion

		#region List commands

		/// <summary>
		/// Add the given model object to this control.
		/// </summary>
		/// <param name="modelObject">The model object to be displayed</param>
		/// <remarks>See AddObjects() for more details</remarks>
		public virtual void AddObject(object modelObject)
		{
			if (InvokeRequired)
				Invoke((MethodInvoker)delegate () { AddObject(modelObject); });
			else
				AddObjects(new object[] { modelObject });
		}

		/// <summary>
		/// Add the given collection of model objects to this control.
		/// </summary>
		/// <param name="modelObjects">A collection of model objects</param>
		/// <remarks>
		/// <para>The added objects will appear in their correct sort position, if sorting
		/// is active (i.e. if PrimarySortColumn is not null). Otherwise, they will appear at the end of the list.</para>
		/// <para>No check is performed to see if any of the objects are already in the ListView.</para>
		/// <para>Null objects are silently ignored.</para>
		/// </remarks>
		public virtual void AddObjects(ICollection<object> modelObjects)
		{
			if (InvokeRequired)
			{
				Invoke((MethodInvoker)delegate () { AddObjects(modelObjects); });
				return;
			}
			InsertObjects(ObjectListView.EnumerableCount(Objects), modelObjects);
			Sort(PrimarySortColumn, PrimarySortOrder);
		}

		private BeforeSortingEventArgs BuildBeforeSortingEventArgs(OLVColumn column, SortOrder order)
		{
			OLVColumn groupBy = AlwaysGroupByColumn ?? column ?? GetColumn(0);
			SortOrder groupByOrder = AlwaysGroupBySortOrder;
			if (order == SortOrder.None)
			{
				order = Sorting;
				if (order == SortOrder.None)
					order = SortOrder.Ascending;
			}
			if (groupByOrder == SortOrder.None)
				groupByOrder = order;

			BeforeSortingEventArgs args = new BeforeSortingEventArgs(
				groupBy, groupByOrder,
				column, order,
				SecondarySortColumn ?? GetColumn(0),
				SecondarySortOrder == SortOrder.None ? order : SecondarySortOrder);
			if (column != null)
				args.Canceled = !column.Sortable;
			return args;
		}

		/// <summary>
		/// Build/rebuild all the list view items in the list, preserving as much state as is possible
		/// </summary>
		public virtual void BuildList()
		{
			if (InvokeRequired)
				Invoke(new MethodInvoker(BuildList));
			else
				BuildList(true);
		}

		/// <summary>
		/// Build/rebuild all the list view items in the list
		/// </summary>
		/// <param name="shouldPreserveState">If this is true, the control will try to preserve the selection,
		/// focused item, and the scroll position (see Remarks)
		/// </param>
		/// <remarks>
		/// <para>
		/// Use this method in situations were the contents of the list is basically the same
		/// as previously.
		/// </para>
		/// </remarks>
		public virtual void BuildList(bool shouldPreserveState)
		{
			ApplyExtendedStyles();
			int previousTopIndex = TopItemIndex;
			Point currentScrollPosition = LowLevelScrollPosition;

			IList<object> previousSelection = new List<object>();
			object previousFocus = null;
			if (shouldPreserveState && objects != null)
			{
				previousSelection = SelectedObjects;
				OLVListItem focusedItem = FocusedItem as OLVListItem;
				if (focusedItem != null)
					previousFocus = focusedItem.RowObject;
			}

			var objectsToDisplay = Objects;

			BeginUpdate();
			try
			{
				Items.Clear();
				ListViewItemSorter = null;

				if (objectsToDisplay != null)
				{
					// Build a list of all our items and then display them. (Building
					// a list and then doing one AddRange is about 10-15% faster than individual adds)
					List<ListViewItem> itemList = new List<ListViewItem>(); // use ListViewItem to avoid co-variant conversion
					foreach (object rowObject in objectsToDisplay)
					{
						OLVListItem lvi = new OLVListItem(rowObject);
						FillInValues(lvi, rowObject);
						itemList.Add(lvi);
					}
					Items.AddRange(itemList.ToArray());
					Sort();

					if (shouldPreserveState)
					{
						SelectedObjects = previousSelection;
						FocusedItem = ModelToItem(previousFocus);
					}
				}
			}
			finally
			{
				EndUpdate();
			}

			// We can only restore the scroll position after the EndUpdate() because
			// of caching that the ListView does internally during a BeginUpdate/EndUpdate pair.
			if (shouldPreserveState)
			{
				// Restore the scroll position. TopItemIndex is best, but doesn't work
				// when the control is grouped.
				if (ShowGroups)
					LowLevelScroll(currentScrollPosition.X, currentScrollPosition.Y);
				else
					TopItemIndex = previousTopIndex;
			}
		}

		/// <summary>
		/// Clear any cached info this list may have been using
		/// </summary>
		public virtual void ClearCachedInfo()
		{
			// ObjectListView doesn't currently cache information but subclass do (or might)
		}

		/// <summary>
		/// Apply all required extended styles to our control.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Whenever .NET code sets an extended style, it erases all other extended styles
		/// that it doesn't use. So, we have to explicit reapply the styles that we have
		/// added.
		/// </para>
		/// <para>
		/// Normally, we would override CreateParms property and update
		/// the ExStyle member, but ListView seems to ignore all ExStyles that
		/// it doesn't already know about. Worse, when we set the LVS_EX_HEADERINALLVIEWS 
		/// value, bad things happen (the control crashes!).
		/// </para>
		/// </remarks>
		protected virtual void ApplyExtendedStyles()
		{
			const int LVS_EX_SUBITEMIMAGES = 0x00000002;
			//const int LVS_EX_TRANSPARENTBKGND = 0x00400000;
			const int LVS_EX_HEADERINALLVIEWS = 0x02000000;

			const int STYLE_MASK = LVS_EX_SUBITEMIMAGES | LVS_EX_HEADERINALLVIEWS;
			int style = 0;

			if (ShowImagesOnSubItems && !VirtualMode)
				style ^= LVS_EX_SUBITEMIMAGES;

			NativeMethods.SetExtendedStyle(this, style, STYLE_MASK);
		}

		/// <summary>
		/// Remove all items from this list
		/// </summary>
		/// <remark>This method can safely be called from background threads.</remark>
		public virtual void ClearObjects()
		{
			if (InvokeRequired)
				Invoke(new MethodInvoker(ClearObjects));
			else
				SetObjects(null);
		}

		/// <summary>
		/// Return the n'th item (0-based) in the order they are shown to the user.
		/// If the control is not grouped, the display order is the same as the
		/// sorted list order. But if the list is grouped, the display order is different.
		/// </summary>
		/// <param name="n"></param>
		/// <returns></returns>
		public virtual OLVListItem GetNthItemInDisplayOrder(int n)
		{
			return GetItem(n);
		}

		/// <summary>
		/// Return the display index of the given listviewitem index.
		/// If the control is not grouped, the display order is the same as the
		/// sorted list order. But if the list is grouped, the display order is different.
		/// </summary>
		/// <param name="itemIndex"></param>
		/// <returns></returns>
		public virtual int GetDisplayOrderOfItemIndex(int itemIndex)
		{
			return itemIndex;
		}

		/// <summary>
		/// Insert the given collection of objects before the given position
		/// </summary>
		/// <param name="index">Where to insert the objects</param>
		/// <param name="modelObjects">The objects to be inserted</param>
		/// <remarks>
		/// <para>
		/// This operation only makes sense of non-sorted, non-grouped
		/// lists, since any subsequent sort/group operation will rearrange
		/// the list.
		/// </para>
		/// <para>This method only works on ObjectListViews and FastObjectListViews.</para>
		///</remarks>
		public virtual void InsertObjects(int index, ICollection<object> modelObjects)
		{
			if (InvokeRequired)
			{
				Invoke((MethodInvoker)delegate ()
				{
					InsertObjects(index, modelObjects);
				});
				return;
			}
			if (modelObjects == null)
				return;

			BeginUpdate();
			try
			{
				// Give the world a chance to cancel or change the added objects
				ItemsAddingEventArgs args = new ItemsAddingEventArgs(modelObjects);
				OnItemsAdding(args);
				if (args.Canceled)
					return;
				modelObjects = args.ObjectsToAdd;

				TakeOwnershipOfObjects();
				var ourObjects = EnumerableToArray(Objects, false);

				ListViewItemSorter = null;
				index = Math.Max(0, Math.Min(index, GetItemCount()));
				int i = index;
				foreach (object modelObject in modelObjects)
				{
					if (modelObject != null)
					{
						ourObjects.Insert(i, modelObject);
						OLVListItem lvi = new OLVListItem(modelObject);
						FillInValues(lvi, modelObject);
						Items.Insert(i, lvi);
						i++;
					}
				}

				for (i = index; i < GetItemCount(); i++)
				{
					OLVListItem lvi = GetItem(i);
					SetSubItemImages(lvi.Index, lvi);
				}

				PostProcessRows();

				// Tell the world that the list has changed
				OnItemsChanged(new ItemsChangedEventArgs());
			}
			finally
			{
				EndUpdate();
			}
		}

		/// <summary>
		/// Return true if the row representing the given model is selected
		/// </summary>
		/// <param name="model">The model object to look for</param>
		/// <returns>Is the row selected</returns>
		public bool IsSelected(object model)
		{
			OLVListItem item = ModelToItem(model);
			return item != null && item.Selected;
		}

		/// <summary>
		/// Scroll the ListView by the given deltas.
		/// </summary>
		/// <param name="dx">Horizontal delta</param>
		/// <param name="dy">Vertical delta</param>
		public void LowLevelScroll(int dx, int dy)
		{
			NativeMethods.Scroll(this, dx, dy);
		}

		/// <summary>
		/// Return a point that represents the current horizontal and vertical scroll positions 
		/// </summary>
		[Browsable(false),
		DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Point LowLevelScrollPosition
		{
			get
			{
				return new Point(NativeMethods.GetScrollPosition(this, true), NativeMethods.GetScrollPosition(this, false));
			}
		}

		/// <summary>
		/// Calculate what item is under the given point?
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		new public ListViewHitTestInfo HitTest(int x, int y)
		{
			// Everything costs something. Playing with the layout of the header can cause problems
			// with the hit testing. If the header shrinks, the underlying control can throw a tantrum.
			try
			{
				return base.HitTest(x, y);
			}
			catch (ArgumentOutOfRangeException)
			{
				return new ListViewHitTestInfo(null, null, ListViewHitTestLocations.None);
			}
		}

		/// <summary>
		/// Perform a hit test using the Windows control's SUBITEMHITTEST message.
		/// This provides information about group hits that the standard ListView.HitTest() does not.
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		protected OlvListViewHitTestInfo LowLevelHitTest(int x, int y)
		{

			// If it's not even in the control, don't bother with anything else
			if (!ClientRectangle.Contains(x, y))
				return new OlvListViewHitTestInfo(null, null, 0, 0);

			// Is the point over the header?
			OlvListViewHitTestInfo.HeaderHitTestInfo headerHitTestInfo = HeaderControl.HitTest(x, y);
			if (headerHitTestInfo != null)
				return new OlvListViewHitTestInfo(this, headerHitTestInfo.ColumnIndex, headerHitTestInfo.IsOverCheckBox, headerHitTestInfo.OverDividerIndex);

			// Call the native hit test method, which is a little confusing.
			NativeMethods.LVHITTESTINFO lParam = new NativeMethods.LVHITTESTINFO();
			lParam.pt_x = x;
			lParam.pt_y = y;
			int index = NativeMethods.HitTest(this, ref lParam);

			// Setup the various values we need to make our hit test structure
			bool isGroupHit = (lParam.flags & (int)HitTestLocationEx.LVHT_EX_GROUP) != 0;
			OLVListItem hitItem = isGroupHit || index == -1 ? null : GetItem(index);
			OLVListSubItem subItem = (View == View.Details && hitItem != null) ? hitItem.GetSubItem(lParam.iSubItem) : null;

			OlvListViewHitTestInfo olvListViewHitTest = new OlvListViewHitTestInfo(hitItem, subItem, lParam.flags, lParam.iSubItem);
			// System.Diagnostics.Debug.WriteLine(String.Format("HitTest({0}, {1})=>{2}", x, y, olvListViewHitTest));
			return olvListViewHitTest;
		}

		/// <summary>
		/// What is under the given point? This takes the various parts of a cell into accout, including
		/// any custom parts that a custom renderer might use
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns>An information block about what is under the point</returns>
		public virtual OlvListViewHitTestInfo OlvHitTest(int x, int y)
		{
			OlvListViewHitTestInfo hti = LowLevelHitTest(x, y);

			// There is a bug/"feature" of the ListView concerning hit testing.
			// If FullRowSelect is false and the point is over cell 0 but not on
			// the text or icon, HitTest will not register a hit. We could turn
			// FullRowSelect on, do the HitTest, and then turn it off again, but
			// toggling FullRowSelect in that way messes up the tooltip in the
			// underlying control. So we have to find another way.
			//
			// It's too hard to try to write the hit test from scratch. Grouping (for
			// example) makes it just too complicated. So, we have to use HitTest
			// but try to get around its limits.
			//
			// First step is to determine if the point was within column 0.
			// If it was, then we only have to determine if there is an actual row
			// under the point. If there is, then we know that the point is over cell 0.
			// So we try a Battleship-style approach: is there a subcell to the right
			// of cell 0? This will return a false negative if column 0 is the rightmost column,
			// so we also check for a subcell to the left. But if only column 0 is visible,
			// then that will fail too, so we check for something at the very left of the
			// control.
			//
			// This will still fail under pathological conditions. If column 0 fills
			// the whole listview and no part of the text column 0 is visible
			// (because it is horizontally scrolled offscreen), then the hit test will fail.

			// Are we in the buggy context? Details view, not full row select, and
			// failing to find anything
			if (hti.Item == null && !FullRowSelect && View == View.Details)
			{
				// Is the point within the column 0? If it is, maybe it should have been a hit.
				// Let's test slightly to the right and then to left of column 0. Hopefully one
				// of those will hit a subitem
				Point sides = NativeMethods.GetScrolledColumnSides(this, 0);
				if (x >= sides.X && x <= sides.Y)
				{
					// We look for:
					// - any subitem to the right of cell 0?
					// - any subitem to the left of cell 0?
					// - cell 0 at the left edge of the screen
					hti = LowLevelHitTest(sides.Y + 4, y);
					if (hti.Item == null)
						hti = LowLevelHitTest(sides.X - 4, y);
					if (hti.Item == null)
						hti = LowLevelHitTest(4, y);

					if (hti.Item != null)
					{
						// We hit something! So, the original point must have been in cell 0
						hti.ColumnIndex = 0;
						hti.SubItem = hti.Item.GetSubItem(0);
						hti.Location = ListViewHitTestLocations.None;
						hti.HitTestLocation = HitTestLocation.InCell;
					}
				}
			}

			if (OwnerDraw)
				CalculateOwnerDrawnHitTest(hti, x, y);
			else
				CalculateStandardHitTest(hti, x, y);

			return hti;
		}

		/// <summary>
		/// Perform a hit test when the control is not owner drawn
		/// </summary>
		/// <param name="hti"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		protected virtual void CalculateStandardHitTest(OlvListViewHitTestInfo hti, int x, int y)
		{

			// Standard hit test works fine for the primary column
			if (View != View.Details || hti.ColumnIndex == 0 ||
				hti.SubItem == null || hti.Column == null)
				return;

			Rectangle cellBounds = hti.SubItem.Bounds;
			bool hasImage = (GetActualImageIndex(hti.SubItem.ImageSelector) != -1);

			// Unless we say otherwise, it was an general incell hit
			hti.HitTestLocation = HitTestLocation.InCell;

			// Check if the point is over where an image should be.
			// If there is a checkbox or image there, tag it and exit.
			Rectangle r = cellBounds;
			r.Width = SmallImageSize.Width;
			if (r.Contains(x, y))
			{
				if (hti.Column.CheckBoxes)
				{
					hti.HitTestLocation = HitTestLocation.CheckBox;
					return;
				}
				if (hasImage)
				{
					hti.HitTestLocation = HitTestLocation.Image;
					return;
				}
			}

			// Figure out where the text actually is and if the point is in it
			// The standard HitTest assumes that any point inside a subitem is
			// a hit on Text -- which is clearly not true.
			Rectangle textBounds = cellBounds;
			textBounds.X += 4;
			if (hasImage)
				textBounds.X += SmallImageSize.Width;

			Size proposedSize = new Size(textBounds.Width, textBounds.Height);
			Size textSize = TextRenderer.MeasureText(hti.SubItem.Text, Font, proposedSize, TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
			textBounds.Width = textSize.Width;

			switch (hti.Column.TextAlign)
			{
				case HorizontalAlignment.Center:
					textBounds.X += (cellBounds.Right - cellBounds.Left - textSize.Width) / 2;
					break;
				case HorizontalAlignment.Right:
					textBounds.X = cellBounds.Right - textSize.Width;
					break;
			}
			if (textBounds.Contains(x, y))
			{
				hti.HitTestLocation = HitTestLocation.Text;
			}
		}

		/// <summary>
		/// Perform a hit test when the control is owner drawn. This hands off responsibility
		/// to the renderer.
		/// </summary>
		/// <param name="hti"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		protected virtual void CalculateOwnerDrawnHitTest(OlvListViewHitTestInfo hti, int x, int y)
		{
			// If the click wasn't on an item, give up
			if (hti.Item == null)
				return;

			// If the list is showing column, but they clicked outside the columns, also give up
			if (View == View.Details && hti.Column == null)
				return;

			// Which renderer was responsible for drawing that point
			IRenderer renderer = View == View.Details
				? GetCellRenderer(hti.RowObject, hti.Column)
				: ItemRenderer;

			// We can't decide who was responsible. Give up
			if (renderer == null)
				return;

			// Ask the responsible renderer what is at that point
			renderer.HitTest(hti, x, y);
		}

		/// <summary>
		/// Remove the given model object from the ListView
		/// </summary>
		/// <param name="modelObject">The model to be removed</param>
		/// <remarks>See RemoveObjects() for more details
		/// <para>This method is thread-safe.</para>
		/// </remarks>
		public virtual void RemoveObject(object modelObject)
		{
			if (InvokeRequired)
				Invoke((MethodInvoker)delegate () { RemoveObject(modelObject); });
			else
				RemoveObjects(new object[] { modelObject });
		}

		/// <summary>
		/// Remove all of the given objects from the control.
		/// </summary>
		/// <param name="modelObjects">Collection of objects to be removed</param>
		/// <remarks>
		/// <para>Nulls and model objects that are not in the ListView are silently ignored.</para>
		/// <para>This method is thread-safe.</para>
		/// </remarks>
		public virtual void RemoveObjects(ICollection<object> modelObjects)
		{
			if (InvokeRequired)
			{
				Invoke((MethodInvoker)delegate () { RemoveObjects(modelObjects); });
				return;
			}
			if (modelObjects == null)
				return;

			BeginUpdate();
			try
			{
				// Give the world a chance to cancel or change the added objects
				ItemsRemovingEventArgs args = new ItemsRemovingEventArgs(modelObjects);
				OnItemsRemoving(args);
				if (args.Canceled)
					return;
				modelObjects = args.ObjectsToRemove;

				TakeOwnershipOfObjects();
				var ourObjects = EnumerableToArray(Objects, false);
				foreach (object modelObject in modelObjects)
				{
					if (modelObject != null)
					{
						int i = ourObjects.IndexOf(modelObject);
						if (i >= 0)
							ourObjects.RemoveAt(i);
						i = IndexOf(modelObject);
						if (i >= 0)
							Items.RemoveAt(i);
					}
				}
				PostProcessRows();

				// Tell the world that the list has changed
				OnItemsChanged(new ItemsChangedEventArgs());
			}
			finally
			{
				EndUpdate();
			}
		}

		/// <summary>
		/// Set the collection of objects that will be shown in this list view.
		/// </summary>
		/// <remark>This method can safely be called from background threads.</remark>
		/// <remarks>The list is updated immediately</remarks>
		/// <param name="collection">The objects to be displayed</param>
		public virtual void SetObjects(IEnumerable<object> collection)
		{
			SetObjects(collection, false);
		}

		/// <summary>
		/// Set the collection of objects that will be shown in this list view.
		/// </summary>
		/// <remark>This method can safely be called from background threads.</remark>
		/// <remarks>The list is updated immediately</remarks>
		/// <param name="collection">The objects to be displayed</param>
		/// <param name="preserveState">Should the state of the list be preserved as far as is possible.</param>
		public virtual void SetObjects(IEnumerable<object> collection, bool preserveState)
		{
			if (InvokeRequired)
			{
				Invoke((MethodInvoker)delegate { SetObjects(collection, preserveState); });
				return;
			}

			// Give the world a chance to cancel or change the assigned collection
			ItemsChangingEventArgs args = new ItemsChangingEventArgs(objects, collection);
			OnItemsChanging(args);
			if (args.Canceled)
				return;
			collection = args.NewObjects;

			// If we own the current list and they change to another list, we don't own it anymore
			if (isOwnerOfObjects && !ReferenceEquals(objects, collection))
				isOwnerOfObjects = false;
			objects = collection;
			BuildList(preserveState);

			// Tell the world that the list has changed
			OnItemsChanged(new ItemsChangedEventArgs());
		}

		/// <summary>
		/// Update the given model object into the ListView. The model will be added if it doesn't already exist.
		/// </summary>
		/// <param name="modelObject">The model to be updated</param>
		/// <remarks>
		/// <para>This method is thread-safe.</para>
		/// <para>This method will cause the list to be resorted.</para>
		/// <para>This method only works on ObjectListViews and FastObjectListViews.</para>
		/// </remarks>
		public virtual void UpdateObject(object modelObject)
		{
			if (InvokeRequired)
				Invoke((MethodInvoker)delegate () { UpdateObject(modelObject); });
			else
				UpdateObjects(new object[] { modelObject });
		}

		/// <summary>
		/// Update the pre-existing models that are equal to the given objects. If any of the model doesn't
		/// already exist in the control, they will be added.
		/// </summary>
		/// <param name="modelObjects">Collection of objects to be updated/added</param>
		/// <remarks>
		/// <para>This method will cause the list to be resorted.</para>
		/// <para>Nulls are silently ignored.</para>
		/// <para>This method is thread-safe.</para>
		/// <para>This method only works on ObjectListViews and FastObjectListViews.</para>
		/// </remarks>
		public virtual void UpdateObjects(ICollection modelObjects)
		{
			if (InvokeRequired)
			{
				Invoke((MethodInvoker)delegate () { UpdateObjects(modelObjects); });
				return;
			}
			if (modelObjects == null || modelObjects.Count == 0)
				return;

			BeginUpdate();
			try
			{
				var objectsToAdd = new List<object>();

				TakeOwnershipOfObjects();
				var ourObjects = EnumerableToArray(Objects, false);
				foreach (object modelObject in modelObjects)
				{
					if (modelObject != null)
					{
						int i = ourObjects.IndexOf(modelObject);
						if (i < 0)
							objectsToAdd.Add(modelObject);
						else
						{
							ourObjects[i] = modelObject;
							OLVListItem olvi = ModelToItem(modelObject);
							if (olvi != null)
							{
								olvi.RowObject = modelObject;
								RefreshItem(olvi);
							}
						}
					}
				}
				PostProcessRows();

				AddObjects(objectsToAdd);

				// Tell the world that the list has changed
				OnItemsChanged(new ItemsChangedEventArgs());
			}
			finally
			{
				EndUpdate();
			}
		}

		#endregion

		#region Event handlers

		/// <summary>
		/// The application is idle. Trigger a SelectionChanged event.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		protected virtual void HandleApplicationIdle(object sender, EventArgs e)
		{
			// Remove the handler before triggering the event
			Application.Idle -= new EventHandler(HandleApplicationIdle);
			hasIdleHandler = false;

			OnSelectionChanged(new EventArgs());
		}

		/// <summary>
		/// The application is idle. Handle the column resizing event.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		protected virtual void HandleApplicationIdleResizeColumns(object sender, EventArgs e)
		{
			// Remove the handler before triggering the event
			Application.Idle -= new EventHandler(HandleApplicationIdleResizeColumns);
			hasResizeColumnsHandler = false;

			ResizeFreeSpaceFillingColumns();
		}

		/// <summary>
		/// Handle the BeginScroll listview notification
		/// </summary>
		/// <param name="m"></param>
		/// <returns>True if the event was completely handled</returns>
		protected virtual bool HandleBeginScroll(ref Message m)
		{
			NativeMethods.NMLVSCROLL nmlvscroll = (NativeMethods.NMLVSCROLL)m.GetLParam(typeof(NativeMethods.NMLVSCROLL));
			if (nmlvscroll.dx != 0)
			{
				int scrollPositionH = NativeMethods.GetScrollPosition(this, true);
				ScrollEventArgs args = new ScrollEventArgs(ScrollEventType.EndScroll, scrollPositionH - nmlvscroll.dx, scrollPositionH, ScrollOrientation.HorizontalScroll);
				OnScroll(args);

				// Force any empty list msg to redraw when the list is scrolled horizontally
				if (GetItemCount() == 0)
					Invalidate();
			}
			if (nmlvscroll.dy != 0)
			{
				int scrollPositionV = NativeMethods.GetScrollPosition(this, false);
				ScrollEventArgs args = new ScrollEventArgs(ScrollEventType.EndScroll, scrollPositionV - nmlvscroll.dy, scrollPositionV, ScrollOrientation.VerticalScroll);
				OnScroll(args);
			}

			return false;
		}

		/// <summary>
		/// Handle the EndScroll listview notification
		/// </summary>
		/// <param name="m"></param>
		/// <returns>True if the event was completely handled</returns>
		protected virtual bool HandleEndScroll(ref Message m)
		{
			//System.Diagnostics.Debug.WriteLine("LVN_BEGINSCROLL");

			// There is a bug in ListView under XP that causes the gridlines to be incorrectly scrolled
			// when the left button is clicked to scroll. This is supposedly documented at
			// KB 813791, but I couldn't find it anywhere. You can follow this thread to see the discussion
			// http://www.ureader.com/msg/1484143.aspx

			if (!IsVistaOrLater && IsLeftMouseDown && GridLines)
			{
				Invalidate();
				Update();
			}

			return false;
		}

		/// <summary>
		/// The cell tooltip control wants information about the tool tip that it should show.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		protected virtual void HandleCellToolTipShowing(object sender, ToolTipShowingEventArgs e)
		{
			BuildCellEvent(e, PointToClient(Cursor.Position));
			if (e.Item != null)
			{
				e.Text = GetCellToolTip(e.ColumnIndex, e.RowIndex);
				OnCellToolTip(e);
			}
		}

		/// <summary>
		/// Event handler for the column click event
		/// </summary>
		protected virtual void HandleColumnClick(object sender, ColumnClickEventArgs e)
		{
			if (!PossibleFinishCellEditing())
				return;

			// Toggle the sorting direction on successive clicks on the same column
			if (PrimarySortColumn != null && e.Column == PrimarySortColumn.Index)
				PrimarySortOrder = (PrimarySortOrder == SortOrder.Descending ? SortOrder.Ascending : SortOrder.Descending);
			else
				PrimarySortOrder = SortOrder.Ascending;

			BeginUpdate();
			try
			{
				Sort(e.Column);
			}
			finally
			{
				EndUpdate();
			}
		}

		#endregion

		#region Low level Windows Message handling

		/// <summary>
		/// Override the basic message pump for this control
		/// </summary>
		/// <param name="m"></param>
		protected override void WndProc(ref Message m)
		{
			// System.Diagnostics.Debug.WriteLine(m.Msg);
			switch (m.Msg)
			{
				case 2: // WM_DESTROY
					if (!HandleDestroy(ref m))
						base.WndProc(ref m);
					break;
				//case 0x14: // WM_ERASEBKGND
				//    Can't do anything here since, when the control is double buffered, anything
				//    done here is immediately over-drawn
				//    break;
				case 0x0F: // WM_PAINT
					if (!HandlePaint(ref m))
						base.WndProc(ref m);
					break;
				case 0x46: // WM_WINDOWPOSCHANGING
					if (PossibleFinishCellEditing() && !HandleWindowPosChanging(ref m))
						base.WndProc(ref m);
					break;
				case 0x4E: // WM_NOTIFY
					if (!HandleNotify(ref m))
						base.WndProc(ref m);
					break;
				case 0x0100: // WM_KEY_DOWN
					if (!HandleKeyDown(ref m))
						base.WndProc(ref m);
					break;
				case 0x0102: // WM_CHAR
					if (!HandleChar(ref m))
						base.WndProc(ref m);
					break;
				case 0x0200: // WM_MOUSEMOVE
					if (!HandleMouseMove(ref m))
						base.WndProc(ref m);
					break;
				case 0x0201: // WM_LBUTTONDOWN
					if (PossibleFinishCellEditing() && !HandleLButtonDown(ref m))
						base.WndProc(ref m);
					break;
				case 0x202:  // WM_LBUTTONUP
					if (PossibleFinishCellEditing() && !HandleLButtonUp(ref m))
						base.WndProc(ref m);
					break;
				case 0x0203: // WM_LBUTTONDBLCLK
					if (PossibleFinishCellEditing() && !HandleLButtonDoubleClick(ref m))
						base.WndProc(ref m);
					break;
				case 0x0204: // WM_RBUTTONDOWN
					if (PossibleFinishCellEditing() && !HandleRButtonDown(ref m))
						base.WndProc(ref m);
					break;
				case 0x0206: // WM_RBUTTONDBLCLK
					if (PossibleFinishCellEditing() && !HandleRButtonDoubleClick(ref m))
						base.WndProc(ref m);
					break;
				case 0x204E: // WM_REFLECT_NOTIFY
					if (!HandleReflectNotify(ref m))
						base.WndProc(ref m);
					break;
				case 0x114: // WM_HSCROLL:
				case 0x115: // WM_VSCROLL:
							//System.Diagnostics.Debug.WriteLine("WM_VSCROLL");
					if (PossibleFinishCellEditing())
						base.WndProc(ref m);
					break;
				case 0x20A: // WM_MOUSEWHEEL:
				case 0x20E: // WM_MOUSEHWHEEL:
					if (PossibleFinishCellEditing())
						base.WndProc(ref m);
					break;
				case 0x7B: // WM_CONTEXTMENU
					if (!HandleContextMenu(ref m))
						base.WndProc(ref m);
					break;
				case 0x1000 + 18: // LVM_HITTEST:
								  //System.Diagnostics.Debug.WriteLine("LVM_HITTEST");
					if (skipNextHitTest)
					{
						//System.Diagnostics.Debug.WriteLine("SKIPPING LVM_HITTEST");
						skipNextHitTest = false;
					}
					else
					{
						base.WndProc(ref m);
					}
					break;
				default:
					base.WndProc(ref m);
					break;
			}
		}

		/// <summary>
		/// Handle the search for item m if possible.
		/// </summary>
		/// <param name="m">The m to be processed</param>
		/// <returns>bool to indicate if the msg has been handled</returns>
		protected virtual bool HandleChar(ref Message m)
		{
			// Trigger a normal KeyPress event, which listeners can handle if they want.
			// Handling the event stops ObjectListView's fancy search-by-typing.
			if (ProcessKeyEventArgs(ref m))
				return true;

			const int MILLISECONDS_BETWEEN_KEYPRESSES = 1000;

			// What character did the user type and was it part of a longer string?
			char character = (char)m.WParam.ToInt32(); //TODO: Will this work on 64 bit or MBCS?
			if (character == (char)Keys.Back)
			{
				// Backspace forces the next key to be considered the start of a new search
				timeLastCharEvent = 0;
				return true;
			}

			if (Environment.TickCount < (timeLastCharEvent + MILLISECONDS_BETWEEN_KEYPRESSES))
				lastSearchString += character;
			else
				lastSearchString = character.ToString(CultureInfo.InvariantCulture);

			// If this control is showing checkboxes, we want to ignore single space presses,
			// since they are used to toggle the selected checkboxes.
			if (CheckBoxes && lastSearchString == " ")
			{
				timeLastCharEvent = 0;
				return true;
			}

			// Where should the search start?
			int start = 0;
			ListViewItem focused = FocusedItem;
			if (focused != null)
			{
				start = GetDisplayOrderOfItemIndex(focused.Index);

				// If the user presses a single key, we search from after the focused item,
				// being careful not to march past the end of the list
				if (lastSearchString.Length == 1)
				{
					start += 1;
					if (start == GetItemCount())
						start = 0;
				}
			}

			// Give the world a chance to fiddle with or completely avoid the searching process
			BeforeSearchingEventArgs args = new BeforeSearchingEventArgs(lastSearchString, start);
			OnBeforeSearching(args);
			if (args.Canceled)
				return true;

			// The parameters of the search may have been changed
			string searchString = args.StringToFind;
			start = args.StartSearchFrom;

			// Do the actual search
			int found = FindMatchingRow(searchString, start, SearchDirectionHint.Down);
			if (found >= 0)
			{
				// Select and focus on the found item
				BeginUpdate();
				try
				{
					SelectedIndices.Clear();
					OLVListItem lvi = GetNthItemInDisplayOrder(found);
					if (lvi != null)
					{
						if (lvi.Enabled)
							lvi.Selected = true;
						lvi.Focused = true;
						EnsureVisible(lvi.Index);
					}
				}
				finally
				{
					EndUpdate();
				}
			}

			// Tell the world that a search has occurred
			AfterSearchingEventArgs args2 = new AfterSearchingEventArgs(searchString, found);
			OnAfterSearching(args2);
			if (!args2.Handled)
			{
				if (found < 0)
					System.Media.SystemSounds.Beep.Play();
			}

			// When did this event occur?
			timeLastCharEvent = Environment.TickCount;
			return true;
		}
		private int timeLastCharEvent;
		private string lastSearchString;

		/// <summary>
		/// The user wants to see the context menu.
		/// </summary>
		/// <param name="m">The windows m</param>
		/// <returns>A bool indicating if this m has been handled</returns>
		/// <remarks>
		/// We want to ignore context menu requests that are triggered by right clicks on the header
		/// </remarks>
		protected virtual bool HandleContextMenu(ref Message m)
		{
			// Don't try to handle context menu commands at design time.
			if (DesignMode)
				return false;

			// If the context menu command was generated by the keyboard, LParam will be -1.
			// We don't want to process these.
			if (m.LParam == minusOne)
				return false;

			// If the context menu came from somewhere other than the header control,
			// we also don't want to ignore it
			if (m.WParam != HeaderControl.Handle)
				return false;

			// OK. Looks like a right click in the header
			if (!PossibleFinishCellEditing())
				return true;

			return false;
		}
		readonly IntPtr minusOne = new IntPtr(-1);

		/// <summary>
		/// Handle the Custom draw series of notifications
		/// </summary>
		/// <param name="m">The message</param>
		/// <returns>True if the message has been handled</returns>
		protected virtual bool HandleCustomDraw(ref Message m)
		{
			const int CDDS_PREPAINT = 1;
			const int CDDS_POSTPAINT = 2;
			const int CDDS_PREERASE = 3;
			const int CDDS_POSTERASE = 4;
			//const int CDRF_NEWFONT = 2;
			//const int CDRF_SKIPDEFAULT = 4;
			const int CDDS_ITEM = 0x00010000;
			const int CDDS_SUBITEM = 0x00020000;
			const int CDDS_ITEMPREPAINT = (CDDS_ITEM | CDDS_PREPAINT);
			const int CDDS_ITEMPOSTPAINT = (CDDS_ITEM | CDDS_POSTPAINT);
			const int CDDS_ITEMPREERASE = (CDDS_ITEM | CDDS_PREERASE);
			const int CDDS_ITEMPOSTERASE = (CDDS_ITEM | CDDS_POSTERASE);
			const int CDDS_SUBITEMPREPAINT = (CDDS_SUBITEM | CDDS_ITEMPREPAINT);
			const int CDDS_SUBITEMPOSTPAINT = (CDDS_SUBITEM | CDDS_ITEMPOSTPAINT);
			const int CDRF_NOTIFYPOSTPAINT = 0x10;
			//const int CDRF_NOTIFYITEMDRAW = 0x20;
			//const int CDRF_NOTIFYSUBITEMDRAW = 0x20; // same value as above!
			const int CDRF_NOTIFYPOSTERASE = 0x40;

			// There is a bug in owner drawn virtual lists which causes lots of custom draw messages
			// to be sent to the control *outside* of a WmPaint event. AFAIK, these custom draw events
			// are spurious and only serve to make the control flicker annoyingly.
			// So, we ignore messages that are outside of a paint event.
			if (!isInWmPaintEvent)
				return true;

			// One more complication! Sometimes with owner drawn virtual lists, the act of drawing
			// the overlays triggers a second attempt to paint the control -- which makes an annoying
			// flicker. So, we only do the custom drawing once per WmPaint event.
			if (!shouldDoCustomDrawing)
				return true;

			NativeMethods.NMLVCUSTOMDRAW nmcustomdraw = (NativeMethods.NMLVCUSTOMDRAW)m.GetLParam(typeof(NativeMethods.NMLVCUSTOMDRAW));
			//System.Diagnostics.Debug.WriteLine(String.Format("cd: {0:x}, {1}, {2}", nmcustomdraw.nmcd.dwDrawStage, nmcustomdraw.dwItemType, nmcustomdraw.nmcd.dwItemSpec));

			// Ignore drawing of group items
			if (nmcustomdraw.dwItemType == 1)
			{
				// This is the basis of an idea about how to owner draw group headers

				//nmcustomdraw.clrText = ColorTranslator.ToWin32(Color.DeepPink);
				//nmcustomdraw.clrFace = ColorTranslator.ToWin32(Color.DeepPink);
				//nmcustomdraw.clrTextBk = ColorTranslator.ToWin32(Color.DeepPink);
				//Marshal.StructureToPtr(nmcustomdraw, m.LParam, false);
				//using (Graphics g = Graphics.FromHdc(nmcustomdraw.nmcd.hdc)) {
				//    g.DrawRectangle(Pens.Red, Rectangle.FromLTRB(nmcustomdraw.rcText.left, nmcustomdraw.rcText.top, nmcustomdraw.rcText.right, nmcustomdraw.rcText.bottom));
				//}
				//m.Result = (IntPtr)((int)m.Result | CDRF_SKIPDEFAULT);
				return true;
			}

			switch (nmcustomdraw.nmcd.dwDrawStage)
			{
				case CDDS_PREPAINT:
					//System.Diagnostics.Debug.WriteLine("CDDS_PREPAINT");
					// Remember which items were drawn during this paint cycle
					if (prePaintLevel == 0)
						drawnItems = new List<OLVListItem>();

					// If there are any items, we have to wait until at least one has been painted
					// before we draw the overlays. If there aren't any items, there will never be any
					// item paint events, so we can draw the overlays whenever
					isAfterItemPaint = (GetItemCount() == 0);
					prePaintLevel++;
					base.WndProc(ref m);

					// Make sure that we get postpaint notifications
					m.Result = (IntPtr)((int)m.Result | CDRF_NOTIFYPOSTPAINT | CDRF_NOTIFYPOSTERASE);
					return true;

				case CDDS_POSTPAINT:
					//System.Diagnostics.Debug.WriteLine("CDDS_POSTPAINT");
					prePaintLevel--;

					// When in group view, we have two problems. On XP, the control sends
					// a whole heap of PREPAINT/POSTPAINT messages before drawing any items.
					// We have to wait until after the first item paint before we draw overlays.
					// On Vista, we have a different problem. On Vista, the control nests calls
					// to PREPAINT and POSTPAINT. We only want to draw overlays on the outermost
					// POSTPAINT.
					if (prePaintLevel == 0 && (isMarqueSelecting || isAfterItemPaint))
					{
						shouldDoCustomDrawing = false;
					}
					break;

				case CDDS_ITEMPREPAINT:
					//System.Diagnostics.Debug.WriteLine("CDDS_ITEMPREPAINT");

					// When in group view on XP, the control send a whole heap of PREPAINT/POSTPAINT
					// messages before drawing any items.
					// We have to wait until after the first item paint before we draw overlays
					isAfterItemPaint = true;

					// This scheme of catching custom draw msgs works fine, except
					// for Tile view. Something in .NET's handling of Tile view causes lots
					// of invalidates and erases. So, we just ignore completely
					// .NET's handling of Tile view and let the underlying control
					// do its stuff. Strangely, if the Tile view is
					// completely owner drawn, those erasures don't happen.
					if (View == View.Tile)
					{
						if (OwnerDraw && ItemRenderer != null)
							base.WndProc(ref m);
					}
					else
					{
						base.WndProc(ref m);
					}

					m.Result = (IntPtr)((int)m.Result | CDRF_NOTIFYPOSTPAINT | CDRF_NOTIFYPOSTERASE);
					return true;

				case CDDS_ITEMPOSTPAINT:
					//System.Diagnostics.Debug.WriteLine("CDDS_ITEMPOSTPAINT");
					// Remember which items have been drawn so we can draw any decorations for them
					// once all other painting is finished
					if (Columns.Count > 0)
					{
						OLVListItem olvi = GetItem((int)nmcustomdraw.nmcd.dwItemSpec);
						if (olvi != null)
							drawnItems.Add(olvi);
					}
					break;

				case CDDS_SUBITEMPREPAINT:
					//System.Diagnostics.Debug.WriteLine(String.Format("CDDS_SUBITEMPREPAINT ({0},{1})", (int)nmcustomdraw.nmcd.dwItemSpec, nmcustomdraw.iSubItem));

					// There is a bug in the .NET framework which appears when column 0 of an owner drawn listview
					// is dragged to another column position.
					// The bounds calculation always returns the left edge of column 0 as being 0.
					// The effects of this bug become apparent
					// when the listview is scrolled horizontally: the control can think that column 0
					// is no longer visible (the horizontal scroll position is subtracted from the bounds, giving a
					// rectangle that is offscreen). In those circumstances, column 0 is not redraw because
					// the control thinks it is not visible and so does not trigger a DrawSubItem event.

					// To fix this problem, we have to detected the situation -- owner drawing column 0 in any column except 0 --
					// trigger our own DrawSubItem, and then prevent the default processing from occuring.

					// Are we owner drawing column 0 when it's in any column except 0?
					if (!OwnerDraw)
						return false;

					int columnIndex = nmcustomdraw.iSubItem;
					if (columnIndex != 0)
						return false;

					int displayIndex = Columns[0].DisplayIndex;
					if (displayIndex == 0)
						return false;

					int rowIndex = (int)nmcustomdraw.nmcd.dwItemSpec;
					OLVListItem item = GetItem(rowIndex);
					if (item == null)
						return false;

					// OK. We have the error condition, so lets do what the .NET framework should do.
					// Trigger an event to draw column 0 when it is not at display index 0
					using (Graphics g = Graphics.FromHdc(nmcustomdraw.nmcd.hdc))
					{

						// Correctly calculate the bounds of cell 0
						Rectangle r = item.GetSubItemBounds(0);

						// We can hardcode "0" here since we know we are only doing this for column 0
						DrawListViewSubItemEventArgs args = new DrawListViewSubItemEventArgs(g, r, item, item.SubItems[0], rowIndex, 0,
							Columns[0], (ListViewItemStates)nmcustomdraw.nmcd.uItemState);
						OnDrawSubItem(args);

						// If the event handler wants to do the default processing (i.e. DrawDefault = true), we are stuck.
						// There is no way we can force the default drawing because of the bug in .NET we are trying to get around.
						System.Diagnostics.Trace.Assert(!args.DrawDefault, "Default drawing is impossible in this situation");
					}
					m.Result = (IntPtr)4;

					return true;

				case CDDS_SUBITEMPOSTPAINT:
					//System.Diagnostics.Debug.WriteLine("CDDS_SUBITEMPOSTPAINT");
					break;

				// I have included these stages, but it doesn't seem that they are sent for ListViews.
				// http://www.tech-archive.net/Archive/VC/microsoft.public.vc.mfc/2006-08/msg00220.html

				case CDDS_PREERASE:
					//System.Diagnostics.Debug.WriteLine("CDDS_PREERASE");
					break;

				case CDDS_POSTERASE:
					//System.Diagnostics.Debug.WriteLine("CDDS_POSTERASE");
					break;

				case CDDS_ITEMPREERASE:
					//System.Diagnostics.Debug.WriteLine("CDDS_ITEMPREERASE");
					break;

				case CDDS_ITEMPOSTERASE:
					//System.Diagnostics.Debug.WriteLine("CDDS_ITEMPOSTERASE");
					break;
			}

			return false;
		}
		bool isAfterItemPaint;
		List<OLVListItem> drawnItems;

		/// <summary>
		/// Handle the underlying control being destroyed
		/// </summary>
		/// <param name="m"></param>
		/// <returns></returns>
		protected virtual bool HandleDestroy(ref Message m)
		{
			//System.Diagnostics.Debug.WriteLine(String.Format("WM_DESTROY: Disposing={0}, IsDisposed={1}, VirtualMode={2}", Disposing, IsDisposed, VirtualMode));

			// Recreate the header control when the listview control is destroyed
			headerControl = null;

			// When the underlying control is destroyed, we need to recreate and reconfigure its tooltip
			if (cellToolTip != null)
			{
				cellToolTip.PushSettings();
				BeginInvoke((MethodInvoker)delegate
				{
					UpdateCellToolTipHandle();
					cellToolTip.PopSettings();
				});
			}

			return false;
		}

		/// <summary>
		/// Find the first row after the given start in which the text value in the
		/// comparison column begins with the given text. The comparison column is column 0,
		/// unless IsSearchOnSortColumn is true, in which case the current sort column is used.
		/// </summary>
		/// <param name="text">The text to be prefix matched</param>
		/// <param name="start">The index of the first row to consider</param>
		/// <param name="direction">Which direction should be searched?</param>
		/// <returns>The index of the first row that matched, or -1</returns>
		/// <remarks>The text comparison is a case-insensitive, prefix match. The search will
		/// search the every row until a match is found, wrapping at the end if needed.</remarks>
		public virtual int FindMatchingRow(string text, int start, SearchDirectionHint direction)
		{
			// We also can't do anything if we don't have data
			int rowCount = GetItemCount();
			if (rowCount == 0)
				return -1;

			// Which column are we going to use for our comparing?
			OLVColumn column = GetColumn(0);
			if (IsSearchOnSortColumn && View == View.Details && PrimarySortColumn != null)
				column = PrimarySortColumn;

			// Do two searches if necessary to find a match. The second search is the wrap-around part of searching
			int i;
			if (direction == SearchDirectionHint.Down)
			{
				i = FindMatchInRange(text, start, rowCount - 1, column);
				if (i == -1 && start > 0)
					i = FindMatchInRange(text, 0, start - 1, column);
			}
			else
			{
				i = FindMatchInRange(text, start, 0, column);
				if (i == -1 && start != rowCount)
					i = FindMatchInRange(text, rowCount - 1, start + 1, column);
			}

			return i;
		}

		/// <summary>
		/// Find the first row in the given range of rows that prefix matches the string value of the given column.
		/// </summary>
		/// <param name="text"></param>
		/// <param name="first"></param>
		/// <param name="last"></param>
		/// <param name="column"></param>
		/// <returns>The index of the matched row, or -1</returns>
		protected virtual int FindMatchInRange(string text, int first, int last, OLVColumn column)
		{
			if (first <= last)
			{
				for (int i = first; i <= last; i++)
				{
					string data = column.GetStringValue(GetNthItemInDisplayOrder(i).RowObject);
					if (data.StartsWith(text, StringComparison.CurrentCultureIgnoreCase))
						return i;
				}
			}
			else
			{
				for (int i = first; i >= last; i--)
				{
					string data = column.GetStringValue(GetNthItemInDisplayOrder(i).RowObject);
					if (data.StartsWith(text, StringComparison.CurrentCultureIgnoreCase))
						return i;
				}
			}

			return -1;
		}

		/// <summary>
		/// Handle a key down message
		/// </summary>
		/// <param name="m"></param>
		/// <returns>True if the msg has been handled</returns>
		protected virtual bool HandleKeyDown(ref Message m)
		{

			// If this is a checkbox list, toggle the selected rows when the user presses Space
			if (CheckBoxes && m.WParam.ToInt32() == (int)Keys.Space && SelectedIndices.Count > 0)
			{
				ToggleSelectedRowCheckBoxes();
				return true;
			}

			// Remember the scroll position so we can decide if the listview has scrolled in the
			// handling of the event.
			int scrollPositionH = NativeMethods.GetScrollPosition(this, true);
			int scrollPositionV = NativeMethods.GetScrollPosition(this, false);

			base.WndProc(ref m);

			// It's possible that the processing in base.WndProc has actually destroyed this control
			if (IsDisposed)
				return true;

			// If the keydown processing changed the scroll position, trigger a Scroll event
			int newScrollPositionH = NativeMethods.GetScrollPosition(this, true);
			int newScrollPositionV = NativeMethods.GetScrollPosition(this, false);

			if (scrollPositionH != newScrollPositionH)
			{
				ScrollEventArgs args = new ScrollEventArgs(ScrollEventType.EndScroll,
					scrollPositionH, newScrollPositionH, ScrollOrientation.HorizontalScroll);
				OnScroll(args);
			}
			if (scrollPositionV != newScrollPositionV)
			{
				ScrollEventArgs args = new ScrollEventArgs(ScrollEventType.EndScroll,
					scrollPositionV, newScrollPositionV, ScrollOrientation.VerticalScroll);
				OnScroll(args);
			}

			return true;
		}

		/// <summary>
		/// Toggle the checkedness of the selected rows
		/// </summary>
		/// <remarks>
		/// <para>
		/// Actually, this doesn't actually toggle all rows. It toggles the first row, and
		/// all other rows get the check state of that first row. This is actually a much
		/// more useful behaviour.
		/// </para>
		/// <para>
		/// If no rows are selected, this method does nothing.
		/// </para>
		/// </remarks>
		public void ToggleSelectedRowCheckBoxes()
		{
			if (SelectedIndices.Count == 0)
				return;
			object primaryModel = GetItem(SelectedIndices[0]).RowObject;
			ToggleCheckObject(primaryModel);
			CheckState? state = GetCheckState(primaryModel);
			if (state.HasValue)
			{
				foreach (object x in SelectedObjects)
					SetObjectCheckedness(x, state.Value);
			}
		}

		/// <summary>
		/// Catch the Left Button down event.
		/// </summary>
		/// <param name="m">The m to be processed</param>
		/// <returns>bool to indicate if the msg has been handled</returns>
		protected virtual bool HandleLButtonDown(ref Message m)
		{
			// We have to intercept this low level message rather than the more natural
			// overridding of OnMouseDown, since ListCtrl's internal mouse down behavior
			// is to select (or deselect) rows when the mouse is released. We don't
			// want the selection to change when the user checks or unchecks a checkbox, so if the
			// mouse down event was to check/uncheck, we have to hide this mouse
			// down event from the control.

			int x = m.LParam.ToInt32() & 0xFFFF;
			int y = (m.LParam.ToInt32() >> 16) & 0xFFFF;

			return ProcessLButtonDown(OlvHitTest(x, y));
		}

		/// <summary>
		/// Handle a left mouse down at the given hit test location
		/// </summary>
		/// <remarks>Subclasses can override this to do something unique</remarks>
		/// <param name="hti"></param>
		/// <returns>True if the message has been handled</returns>
		protected virtual bool ProcessLButtonDown(OlvListViewHitTestInfo hti)
		{

			if (hti.Item == null)
				return false;

			// If the click occurs on a button, ignore it so the row isn't selected
			if (hti.HitTestLocation == HitTestLocation.Button)
			{
				Invalidate();

				return true;
			}

			// If they didn't click checkbox, we can just return
			if (hti.HitTestLocation != HitTestLocation.CheckBox)
				return false;

			// Disabled rows cannot change checkboxes
			if (!hti.Item.Enabled)
				return true;

			// Did they click a sub item checkbox?
			if (hti.Column != null && hti.Column.Index > 0)
			{
				if (hti.Column.IsEditable && hti.Item.Enabled)
					ToggleSubItemCheckBox(hti.RowObject, hti.Column);
				return true;
			}

			// They must have clicked the primary checkbox
			ToggleCheckObject(hti.RowObject);

			// If they change the checkbox of a selected row, all the rows in the selection
			// should be given the same state
			if (hti.Item.Selected)
			{
				CheckState? state = GetCheckState(hti.RowObject);
				if (state.HasValue)
				{
					foreach (object x in SelectedObjects)
						SetObjectCheckedness(x, state.Value);
				}
			}

			return true;
		}

		/// <summary>
		/// Catch the Left Button up event.
		/// </summary>
		/// <param name="m">The m to be processed</param>
		/// <returns>bool to indicate if the msg has been handled</returns>
		protected virtual bool HandleLButtonUp(ref Message m)
		{
			if (MouseMoveHitTest == null)
				return false;

			int x = m.LParam.ToInt32() & 0xFFFF;
			int y = (m.LParam.ToInt32() >> 16) & 0xFFFF;

			// Did they click an enabled, non-empty button?
			if (MouseMoveHitTest.HitTestLocation == HitTestLocation.Button)
			{
				// If a button was hit, Item and Column must be non-null
				if (MouseMoveHitTest.Item.Enabled || MouseMoveHitTest.Column.EnableButtonWhenItemIsDisabled)
				{
					string buttonText = MouseMoveHitTest.Column.GetStringValue(MouseMoveHitTest.RowObject);
					if (!string.IsNullOrEmpty(buttonText))
					{
						Invalidate();
						CellClickEventArgs args = new CellClickEventArgs();
						BuildCellEvent(args, new Point(x, y), MouseMoveHitTest);
						OnButtonClick(args);
						return true;
					}
				}
			}

			return false;
		}

		/// <summary>
		/// Catch the Right Button down event.
		/// </summary>
		/// <param name="m">The m to be processed</param>
		/// <returns>bool to indicate if the msg has been handled</returns>
		protected virtual bool HandleRButtonDown(ref Message m)
		{
			int x = m.LParam.ToInt32() & 0xFFFF;
			int y = (m.LParam.ToInt32() >> 16) & 0xFFFF;

			return ProcessRButtonDown(OlvHitTest(x, y));
		}

		/// <summary>
		/// Handle a left mouse down at the given hit test location
		/// </summary>
		/// <remarks>Subclasses can override this to do something unique</remarks>
		/// <param name="hti"></param>
		/// <returns>True if the message has been handled</returns>
		protected virtual bool ProcessRButtonDown(OlvListViewHitTestInfo hti)
		{
			if (hti.Item == null)
				return false;

			// Ignore clicks on checkboxes
			return (hti.HitTestLocation == HitTestLocation.CheckBox);
		}

		/// <summary>
		/// Catch the Left Button double click event.
		/// </summary>
		/// <param name="m">The m to be processed</param>
		/// <returns>bool to indicate if the msg has been handled</returns>
		protected virtual bool HandleLButtonDoubleClick(ref Message m)
		{
			int x = m.LParam.ToInt32() & 0xFFFF;
			int y = (m.LParam.ToInt32() >> 16) & 0xFFFF;

			return ProcessLButtonDoubleClick(OlvHitTest(x, y));
		}

		/// <summary>
		/// Handle a mouse double click at the given hit test location
		/// </summary>
		/// <remarks>Subclasses can override this to do something unique</remarks>
		/// <param name="hti"></param>
		/// <returns>True if the message has been handled</returns>
		protected virtual bool ProcessLButtonDoubleClick(OlvListViewHitTestInfo hti)
		{

			// If the user double clicked on a checkbox, ignore it
			return (hti.HitTestLocation == HitTestLocation.CheckBox);
		}

		/// <summary>
		/// Catch the right Button double click event.
		/// </summary>
		/// <param name="m">The m to be processed</param>
		/// <returns>bool to indicate if the msg has been handled</returns>
		protected virtual bool HandleRButtonDoubleClick(ref Message m)
		{
			int x = m.LParam.ToInt32() & 0xFFFF;
			int y = (m.LParam.ToInt32() >> 16) & 0xFFFF;

			return ProcessRButtonDoubleClick(OlvHitTest(x, y));
		}

		/// <summary>
		/// Handle a right mouse double click at the given hit test location
		/// </summary>
		/// <remarks>Subclasses can override this to do something unique</remarks>
		/// <param name="hti"></param>
		/// <returns>True if the message has been handled</returns>
		protected virtual bool ProcessRButtonDoubleClick(OlvListViewHitTestInfo hti)
		{

			// If the user double clicked on a checkbox, ignore it
			return (hti.HitTestLocation == HitTestLocation.CheckBox);
		}

		/// <summary>
		/// Catch the MouseMove event.
		/// </summary>
		/// <param name="m">The m to be processed</param>
		/// <returns>bool to indicate if the msg has been handled</returns>
		protected virtual bool HandleMouseMove(ref Message m)
		{
			return false;
		}

		/// <summary>
		/// Handle notifications that have been reflected back from the parent window
		/// </summary>
		/// <param name="m">The m to be processed</param>
		/// <returns>bool to indicate if the msg has been handled</returns>
		protected virtual bool HandleReflectNotify(ref Message m)
		{
			const int NM_CLICK = -2;
			const int NM_DBLCLK = -3;
			const int NM_RDBLCLK = -6;
			const int NM_CUSTOMDRAW = -12;
			const int NM_RELEASEDCAPTURE = -16;
			const int LVN_FIRST = -100;
			const int LVN_ITEMCHANGED = LVN_FIRST - 1;
			const int LVN_ITEMCHANGING = LVN_FIRST - 0;
			const int LVN_HOTTRACK = LVN_FIRST - 21;
			const int LVN_MARQUEEBEGIN = LVN_FIRST - 56;
			const int LVN_GETINFOTIP = LVN_FIRST - 58;
			const int LVN_GETDISPINFO = LVN_FIRST - 77;
			const int LVN_BEGINSCROLL = LVN_FIRST - 80;
			const int LVN_ENDSCROLL = LVN_FIRST - 81;
			const int LVIF_STATE = 8;
			//const int LVIS_FOCUSED = 1;
			const int LVIS_SELECTED = 2;

			bool isMsgHandled = false;

			// TODO: Don't do any logic in this method. Create separate methods for each message

			NativeMethods.NMHDR nmhdr = (NativeMethods.NMHDR)m.GetLParam(typeof(NativeMethods.NMHDR));
			//System.Diagnostics.Debug.WriteLine(String.Format("rn: {0}", nmhdr->code));

			switch (nmhdr.code)
			{
				case NM_CLICK:
					// The standard ListView does some strange stuff here when the list has checkboxes.
					// If you shift click on non-primary columns when FullRowSelect is true, the 
					// checkedness of the selected rows changes. 
					// We can't just not do the base class stuff because it sets up state that is used to
					// determine mouse up events later on.
					// So, we sabotage the base class's process of the click event. The base class does a HITTEST
					// in order to determine which row was clicked -- if that fails, the base class does nothing.
					// So when we get a CLICK, we know that the base class is going to send a HITTEST very soon,
					// so we ignore the next HITTEST message, which will cause the click processing to fail.
					//System.Diagnostics.Debug.WriteLine("NM_CLICK");
					skipNextHitTest = true;
					break;

				case LVN_BEGINSCROLL:
					//System.Diagnostics.Debug.WriteLine("LVN_BEGINSCROLL");
					isMsgHandled = HandleBeginScroll(ref m);
					break;

				case LVN_ENDSCROLL:
					isMsgHandled = HandleEndScroll(ref m);
					break;

				case LVN_MARQUEEBEGIN:
					//System.Diagnostics.Debug.WriteLine("LVN_MARQUEEBEGIN");
					isMarqueSelecting = true;
					break;

				case LVN_GETINFOTIP:
					//System.Diagnostics.Debug.WriteLine("LVN_GETINFOTIP");
					// When virtual lists are in SmallIcon view, they generates tooltip message with invalid item indicies.
					NativeMethods.NMLVGETINFOTIP nmGetInfoTip = (NativeMethods.NMLVGETINFOTIP)m.GetLParam(typeof(NativeMethods.NMLVGETINFOTIP));
					isMsgHandled = nmGetInfoTip.iItem >= GetItemCount();
					break;

				case NM_RELEASEDCAPTURE:
					//System.Diagnostics.Debug.WriteLine("NM_RELEASEDCAPTURE");
					isMarqueSelecting = false;
					Invalidate();
					break;

				case NM_CUSTOMDRAW:
					//System.Diagnostics.Debug.WriteLine("NM_CUSTOMDRAW");
					isMsgHandled = HandleCustomDraw(ref m);
					break;

				case NM_DBLCLK:
					// The default behavior of a .NET ListView with checkboxes is to toggle the checkbox on
					// double-click. That's just silly, if you ask me :)
					if (CheckBoxes)
					{
						// How do we make ListView not do that silliness? We could just ignore the message
						// but the last part of the base code sets up state information, and without that
						// state, the ListView doesn't trigger MouseDoubleClick events. So we fake a
						// right button double click event, which sets up the same state, but without
						// toggling the checkbox.
						nmhdr.code = NM_RDBLCLK;
						Marshal.StructureToPtr(nmhdr, m.LParam, false);
					}
					break;

				case LVN_ITEMCHANGED:
					//System.Diagnostics.Debug.WriteLine("LVN_ITEMCHANGED");
					NativeMethods.NMLISTVIEW nmlistviewPtr2 = (NativeMethods.NMLISTVIEW)m.GetLParam(typeof(NativeMethods.NMLISTVIEW));
					if ((nmlistviewPtr2.uChanged & LVIF_STATE) != 0)
					{
						CheckState currentValue = CalculateCheckState(nmlistviewPtr2.uOldState);
						CheckState newCheckValue = CalculateCheckState(nmlistviewPtr2.uNewState);
						if (currentValue != newCheckValue)
						{
							// Remove the state indicies so that we don't trigger the OnItemChecked method
							// when we call our base method after exiting this method
							nmlistviewPtr2.uOldState = (nmlistviewPtr2.uOldState & 0x0FFF);
							nmlistviewPtr2.uNewState = (nmlistviewPtr2.uNewState & 0x0FFF);
							Marshal.StructureToPtr(nmlistviewPtr2, m.LParam, false);
						}
						else
						{
							bool isSelected = (nmlistviewPtr2.uNewState & LVIS_SELECTED) == LVIS_SELECTED;

							if (isSelected)
							{
								// System.Diagnostics.Debug.WriteLine(String.Format("Selected: {0}", nmlistviewPtr2.iItem));
								bool isShiftDown = (Control.ModifierKeys & Keys.Shift) == Keys.Shift;

								// -1 indicates that all rows are to be selected -- in fact, they already have been.
								// We now have to deselect all the disabled objects.
								if (nmlistviewPtr2.iItem == -1 || isShiftDown)
								{
									Stopwatch sw = Stopwatch.StartNew();
									foreach (object disabledModel in DisabledObjects)
									{
										int modelIndex = IndexOf(disabledModel);
										if (modelIndex >= 0)
											NativeMethods.DeselectOneItem(this, modelIndex);
									}
									System.Diagnostics.Debug.WriteLine(string.Format("PERF - Deselecting took {0}ms / {1} ticks", sw.ElapsedMilliseconds, sw.ElapsedTicks));
								}
								else
								{
									// If the object just selected is disabled, explicitly de-select it
									OLVListItem olvi = GetItem(nmlistviewPtr2.iItem);
									if (olvi != null && !olvi.Enabled)
										NativeMethods.DeselectOneItem(this, nmlistviewPtr2.iItem);
								}
							}
						}
					}
					break;

				case LVN_ITEMCHANGING:
					//System.Diagnostics.Debug.WriteLine("LVN_ITEMCHANGING");
					NativeMethods.NMLISTVIEW nmlistviewPtr = (NativeMethods.NMLISTVIEW)m.GetLParam(typeof(NativeMethods.NMLISTVIEW));
					if ((nmlistviewPtr.uChanged & LVIF_STATE) != 0)
					{
						CheckState currentValue = CalculateCheckState(nmlistviewPtr.uOldState);
						CheckState newCheckValue = CalculateCheckState(nmlistviewPtr.uNewState);

						if (currentValue != newCheckValue)
						{
							// Prevent the base method from seeing the state change,
							// since we handled it elsewhere
							nmlistviewPtr.uChanged &= ~LVIF_STATE;
							Marshal.StructureToPtr(nmlistviewPtr, m.LParam, false);
						}
					}
					break;

				case LVN_HOTTRACK:
					break;

				case LVN_GETDISPINFO:
					break;
			}

			return isMsgHandled;
		}
		private bool skipNextHitTest;

		private CheckState CalculateCheckState(int state)
		{
			switch ((state & 0xf000) >> 12)
			{
				case 1:
					return CheckState.Unchecked;
				case 2:
					return CheckState.Checked;
				case 3:
					return CheckState.Indeterminate;
				default:
					return CheckState.Checked;
			}
		}

		/// <summary>
		/// In the notification messages, we handle attempts to change the width of our columns
		/// </summary>
		/// <param name="m">The m to be processed</param>
		/// <returns>bool to indicate if the msg has been handled</returns>
		protected bool HandleNotify(ref Message m)
		{
			bool isMsgHandled = false;

			const int NM_CUSTOMDRAW = -12;

			const int HDN_FIRST = (0 - 300);
			const int HDN_ITEMCHANGINGA = (HDN_FIRST - 0);
			const int HDN_ITEMCHANGINGW = (HDN_FIRST - 20);
			const int HDN_ITEMCLICKA = (HDN_FIRST - 2);
			const int HDN_ITEMCLICKW = (HDN_FIRST - 22);
			const int HDN_DIVIDERDBLCLICKA = (HDN_FIRST - 5);
			const int HDN_DIVIDERDBLCLICKW = (HDN_FIRST - 25);
			const int HDN_BEGINTRACKA = (HDN_FIRST - 6);
			const int HDN_BEGINTRACKW = (HDN_FIRST - 26);
			const int HDN_TRACKA = (HDN_FIRST - 8);
			const int HDN_TRACKW = (HDN_FIRST - 28);

			// Handle the notification, remembering to handle both ANSI and Unicode versions
			NativeMethods.NMHEADER nmheader = (NativeMethods.NMHEADER)m.GetLParam(typeof(NativeMethods.NMHEADER));
			//System.Diagnostics.Debug.WriteLine(String.Format("not: {0}", nmhdr->code));

			//if (nmhdr.code < HDN_FIRST)
			//    System.Diagnostics.Debug.WriteLine(nmhdr.code);

			// In KB Article #183258, MS states that when a header control has the HDS_FULLDRAG style, it will receive
			// ITEMCHANGING events rather than TRACK events. Under XP SP2 (at least) this is not always true, which may be
			// why MS has withdrawn that particular KB article. It is true that the header is always given the HDS_FULLDRAG
			// style. But even while window style set, the control doesn't always received ITEMCHANGING events.
			// The controlling setting seems to be the Explorer option "Show Window Contents While Dragging"!
			// In the category of "truly bizarre side effects", if the this option is turned on, we will receive
			// ITEMCHANGING events instead of TRACK events. But if it is turned off, we receive lots of TRACK events and
			// only one ITEMCHANGING event at the very end of the process.
			// If we receive HDN_TRACK messages, it's harder to control the resizing process. If we return a result of 1, we
			// cancel the whole drag operation, not just that particular track event, which is clearly not what we want.
			// If we are willing to compile with unsafe code enabled, we can modify the size of the column in place, using the
			// commented out code below. But without unsafe code, the best we can do is allow the user to drag the column to
			// any width, and then spring it back to within bounds once they release the mouse button. UI-wise it's very ugly.
			switch (nmheader.nhdr.code)
			{

				case NM_CUSTOMDRAW:
					if (!OwnerDrawnHeader)
						isMsgHandled = HeaderControl.HandleHeaderCustomDraw(ref m);
					break;

				case HDN_ITEMCLICKA:
				case HDN_ITEMCLICKW:
					if (!PossibleFinishCellEditing())
					{
						m.Result = (IntPtr)1; // prevent the change from happening
						isMsgHandled = true;
					}
					break;

				case HDN_DIVIDERDBLCLICKA:
				case HDN_DIVIDERDBLCLICKW:
				case HDN_BEGINTRACKA:
				case HDN_BEGINTRACKW:
					if (!PossibleFinishCellEditing())
					{
						m.Result = (IntPtr)1; // prevent the change from happening
						isMsgHandled = true;
						break;
					}
					if (nmheader.iItem >= 0 && nmheader.iItem < Columns.Count)
					{
						OLVColumn column = GetColumn(nmheader.iItem);
						// Space filling columns can't be dragged or double-click resized
						if (column.FillsFreeSpace)
						{
							m.Result = (IntPtr)1; // prevent the change from happening
							isMsgHandled = true;
						}
					}
					break;
				case HDN_TRACKA:
				case HDN_TRACKW:
					if (nmheader.iItem >= 0 && nmheader.iItem < Columns.Count)
					{
						NativeMethods.HDITEM hditem = (NativeMethods.HDITEM)Marshal.PtrToStructure(nmheader.pHDITEM, typeof(NativeMethods.HDITEM));
						OLVColumn column = GetColumn(nmheader.iItem);
						if (hditem.cxy < column.MinimumWidth)
							hditem.cxy = column.MinimumWidth;
						else if (column.MaximumWidth != -1 && hditem.cxy > column.MaximumWidth)
							hditem.cxy = column.MaximumWidth;
						Marshal.StructureToPtr(hditem, nmheader.pHDITEM, false);
					}
					break;

				case HDN_ITEMCHANGINGA:
				case HDN_ITEMCHANGINGW:
					nmheader = (NativeMethods.NMHEADER)m.GetLParam(typeof(NativeMethods.NMHEADER));
					if (nmheader.iItem >= 0 && nmheader.iItem < Columns.Count)
					{
						NativeMethods.HDITEM hditem = (NativeMethods.HDITEM)Marshal.PtrToStructure(nmheader.pHDITEM, typeof(NativeMethods.HDITEM));
						OLVColumn column = GetColumn(nmheader.iItem);
						// Check the mask to see if the width field is valid, and if it is, make sure it's within range
						if ((hditem.mask & 1) == 1)
						{
							if (hditem.cxy < column.MinimumWidth ||
								(column.MaximumWidth != -1 && hditem.cxy > column.MaximumWidth))
							{
								m.Result = (IntPtr)1; // prevent the change from happening
								isMsgHandled = true;
							}
						}
					}
					break;

				case ToolTipControl.TTN_SHOW:
					//System.Diagnostics.Debug.WriteLine("olv TTN_SHOW");
					if (CellToolTip.Handle == nmheader.nhdr.hwndFrom)
						isMsgHandled = CellToolTip.HandleShow(ref m);
					break;

				case ToolTipControl.TTN_POP:
					//System.Diagnostics.Debug.WriteLine("olv TTN_POP");
					if (CellToolTip.Handle == nmheader.nhdr.hwndFrom)
						isMsgHandled = CellToolTip.HandlePop(ref m);
					break;

				case ToolTipControl.TTN_GETDISPINFO:
					//System.Diagnostics.Debug.WriteLine("olv TTN_GETDISPINFO");
					if (CellToolTip.Handle == nmheader.nhdr.hwndFrom)
						isMsgHandled = CellToolTip.HandleGetDispInfo(ref m);
					break;
			}

			return isMsgHandled;
		}

		/// <summary>
		/// Create a ToolTipControl to manage the tooltip control used by the listview control
		/// </summary>
		protected virtual void CreateCellToolTip()
		{
			cellToolTip = new ToolTipControl();
			cellToolTip.AssignHandle(NativeMethods.GetTooltipControl(this));
			cellToolTip.Showing += new EventHandler<ToolTipShowingEventArgs>(HandleCellToolTipShowing);
			cellToolTip.SetMaxWidth();
			NativeMethods.MakeTopMost(cellToolTip);
		}

		/// <summary>
		/// Update the handle used by our cell tooltip to be the tooltip used by
		/// the underlying Windows listview control.
		/// </summary>
		protected virtual void UpdateCellToolTipHandle()
		{
			if (cellToolTip != null && cellToolTip.Handle == IntPtr.Zero)
				cellToolTip.AssignHandle(NativeMethods.GetTooltipControl(this));
		}

		/// <summary>
		/// Handle the WM_PAINT event
		/// </summary>
		/// <param name="m"></param>
		/// <returns>Return true if the msg has been handled and nothing further should be done</returns>
		protected virtual bool HandlePaint(ref Message m)
		{
			//System.Diagnostics.Debug.WriteLine("> WMPAINT");

			// We only want to custom draw the control within WmPaint message and only
			// once per paint event. We use these bools to insure this.
			isInWmPaintEvent = true;
			shouldDoCustomDrawing = true;
			prePaintLevel = 0;

			base.WndProc(ref m);
			isInWmPaintEvent = false;
			//System.Diagnostics.Debug.WriteLine("< WMPAINT");
			return true;
		}
		private int prePaintLevel;

		/// <summary>
		/// Handle the window position changing.
		/// </summary>
		/// <param name="m">The m to be processed</param>
		/// <returns>bool to indicate if the msg has been handled</returns>
		protected virtual bool HandleWindowPosChanging(ref Message m)
		{
			const int SWP_NOSIZE = 1;

			NativeMethods.WINDOWPOS pos = (NativeMethods.WINDOWPOS)m.GetLParam(typeof(NativeMethods.WINDOWPOS));
			if ((pos.flags & SWP_NOSIZE) == 0)
			{
				if (pos.cx < Bounds.Width) // only when shrinking
										   // pos.cx is the window width, not the client area width, so we have to subtract the border widths
					ResizeFreeSpaceFillingColumns(pos.cx - (Bounds.Width - ClientSize.Width));
			}

			return false;
		}

		#endregion

		#region Column header clicking, column hiding and resizing

		/// <summary>
		/// Override the OnColumnReordered method to do what we want
		/// </summary>
		/// <param name="e"></param>
		protected override void OnColumnReordered(ColumnReorderedEventArgs e)
		{
			base.OnColumnReordered(e);

			// The internal logic of the .NET code behind a ENDDRAG event means that,
			// at this point, the DisplayIndex's of the columns are not yet as they are
			// going to be. So we have to invoke a method to run later that will remember
			// what the real DisplayIndex's are.
			BeginInvoke(new MethodInvoker(RememberDisplayIndicies));
		}

		private void RememberDisplayIndicies()
		{
			// Remember the display indexes so we can put them back at a later date
			foreach (OLVColumn x in AllColumns)
				x.LastDisplayIndex = x.DisplayIndex;
		}

		/// <summary>
		/// When the column widths are changing, resize the space filling columns
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		protected virtual void HandleColumnWidthChanging(object sender, ColumnWidthChangingEventArgs e)
		{
			if (UpdateSpaceFillingColumnsWhenDraggingColumnDivider && !GetColumn(e.ColumnIndex).FillsFreeSpace)
			{
				// If the width of a column is increasing, resize any space filling columns allowing the extra
				// space that the new column width is going to consume
				int oldWidth = GetColumn(e.ColumnIndex).Width;
				if (e.NewWidth > oldWidth)
					ResizeFreeSpaceFillingColumns(ClientSize.Width - (e.NewWidth - oldWidth));
				else
					ResizeFreeSpaceFillingColumns();
			}
		}

		/// <summary>
		/// When the column widths change, resize the space filling columns
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		protected virtual void HandleColumnWidthChanged(object sender, ColumnWidthChangedEventArgs e)
		{
			if (!GetColumn(e.ColumnIndex).FillsFreeSpace)
				ResizeFreeSpaceFillingColumns();
		}

		/// <summary>
		/// When the size of the control changes, we have to resize our space filling columns.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		protected virtual void HandleLayout(object sender, LayoutEventArgs e)
		{
			// We have to delay executing the recalculation of the columns, since virtual lists
			// get terribly confused if we resize the column widths during this event.
			if (!hasResizeColumnsHandler)
			{
				hasResizeColumnsHandler = true;
				RunWhenIdle(HandleApplicationIdleResizeColumns);
			}
		}

		private void RunWhenIdle(EventHandler eventHandler)
		{
			Application.Idle += eventHandler;
			if (!CanUseApplicationIdle)
			{
				SynchronizationContext.Current.Post(delegate (object x) { Application.RaiseIdle(EventArgs.Empty); }, null);
			}
		}

		/// <summary>
		/// Resize our space filling columns so they fill any unoccupied width in the control
		/// </summary>
		protected virtual void ResizeFreeSpaceFillingColumns()
		{
			ResizeFreeSpaceFillingColumns(ClientSize.Width);
		}

		/// <summary>
		/// Resize our space filling columns so they fill any unoccupied width in the control
		/// </summary>
		protected virtual void ResizeFreeSpaceFillingColumns(int freeSpace)
		{
			// It's too confusing to dynamically resize columns at design time.
			if (DesignMode)
				return;

			// Calculate the free space available
			int totalProportion = 0;
			List<OLVColumn> spaceFillingColumns = new List<OLVColumn>();
			for (int i = 0; i < Columns.Count; i++)
			{
				OLVColumn col = GetColumn(i);
				if (col.FillsFreeSpace)
				{
					spaceFillingColumns.Add(col);
					totalProportion += col.FreeSpaceProportion;
				}
				else
					freeSpace -= col.Width;
			}
			freeSpace = Math.Max(0, freeSpace);

			// Any space filling column that would hit it's Minimum or Maximum
			// width must be treated as a fixed column.
			foreach (OLVColumn col in spaceFillingColumns.ToArray())
			{
				int newWidth = (freeSpace * col.FreeSpaceProportion) / totalProportion;

				if (col.MinimumWidth != -1 && newWidth < col.MinimumWidth)
					newWidth = col.MinimumWidth;
				else if (col.MaximumWidth != -1 && newWidth > col.MaximumWidth)
					newWidth = col.MaximumWidth;
				else
					newWidth = 0;

				if (newWidth > 0)
				{
					col.Width = newWidth;
					freeSpace -= newWidth;
					totalProportion -= col.FreeSpaceProportion;
					spaceFillingColumns.Remove(col);
				}
			}

			// Distribute the free space between the columns
			foreach (OLVColumn col in spaceFillingColumns)
			{
				col.Width = (freeSpace * col.FreeSpaceProportion) / totalProportion;
			}
		}

		#endregion

		#region Checkboxes

		/// <summary>
		/// Check all rows
		/// </summary>
		public virtual void CheckAll()
		{
			CheckedObjects = Objects.Cast<object>().ToList();
		}

		/// <summary>
		/// Check the checkbox in the given column header
		/// </summary>
		/// <remarks>If the given columns header check box is linked to the cell check boxes,
		/// then checkboxes in all cells will also be checked.</remarks>
		/// <param name="column"></param>
		public virtual void CheckHeaderCheckBox(OLVColumn column)
		{
			if (column == null)
				return;

			ChangeHeaderCheckBoxState(column, CheckState.Checked);
		}

		/// <summary>
		/// Mark the checkbox in the given column header as having an indeterminate value
		/// </summary>
		/// <param name="column"></param>
		public virtual void CheckIndeterminateHeaderCheckBox(OLVColumn column)
		{
			if (column == null)
				return;

			ChangeHeaderCheckBoxState(column, CheckState.Indeterminate);
		}

		/// <summary>
		/// Mark the given object as indeterminate check state
		/// </summary>
		/// <param name="modelObject">The model object to be marked indeterminate</param>
		public virtual void CheckIndeterminateObject(object modelObject)
		{
			SetObjectCheckedness(modelObject, CheckState.Indeterminate);
		}

		/// <summary>
		/// Mark the given object as checked in the list
		/// </summary>
		/// <param name="modelObject">The model object to be checked</param>
		public virtual void CheckObject(object modelObject)
		{
			SetObjectCheckedness(modelObject, CheckState.Checked);
		}

		/// <summary>
		/// Mark the given objects as checked in the list
		/// </summary>
		/// <param name="modelObjects">The model object to be checked</param>
		public virtual void CheckObjects(IEnumerable<object> modelObjects)
		{
			foreach (object model in modelObjects)
				CheckObject(model);
		}

		/// <summary>
		/// Put a check into the check box at the given cell
		/// </summary>
		/// <param name="rowObject"></param>
		/// <param name="column"></param>
		public virtual void CheckSubItem(object rowObject, OLVColumn column)
		{
			if (column == null || rowObject == null || !column.CheckBoxes)
				return;

			column.PutCheckState(rowObject, CheckState.Checked);
			RefreshObject(rowObject);
		}

		/// <summary>
		/// Put an indeterminate check into the check box at the given cell
		/// </summary>
		/// <param name="rowObject"></param>
		/// <param name="column"></param>
		public virtual void CheckIndeterminateSubItem(object rowObject, OLVColumn column)
		{
			if (column == null || rowObject == null || !column.CheckBoxes)
				return;

			column.PutCheckState(rowObject, CheckState.Indeterminate);
			RefreshObject(rowObject);
		}

		/// <summary>
		/// Return true of the given object is checked
		/// </summary>
		/// <param name="modelObject">The model object whose checkedness is returned</param>
		/// <returns>Is the given object checked?</returns>
		/// <remarks>If the given object is not in the list, this method returns false.</remarks>
		public virtual bool IsChecked(object modelObject)
		{
			return GetCheckState(modelObject) == CheckState.Checked;
		}

		/// <summary>
		/// Return true of the given object is indeterminately checked
		/// </summary>
		/// <param name="modelObject">The model object whose checkedness is returned</param>
		/// <returns>Is the given object indeterminately checked?</returns>
		/// <remarks>If the given object is not in the list, this method returns false.</remarks>
		public virtual bool IsCheckedIndeterminate(object modelObject)
		{
			return GetCheckState(modelObject) == CheckState.Indeterminate;
		}

		/// <summary>
		/// Is there a check at the check box at the given cell
		/// </summary>
		/// <param name="rowObject"></param>
		/// <param name="column"></param>
		public virtual bool IsSubItemChecked(object rowObject, OLVColumn column)
		{
			if (column == null || rowObject == null || !column.CheckBoxes)
				return false;
			return (column.GetCheckState(rowObject) == CheckState.Checked);
		}

		/// <summary>
		/// Get the checkedness of an object from the model. Returning null means the
		/// model does not know and the value from the control will be used.
		/// </summary>
		/// <param name="modelObject"></param>
		/// <returns></returns>
		protected virtual CheckState? GetCheckState(object modelObject)
		{
			if (CheckStateGetter != null)
				return CheckStateGetter(modelObject);
			return null;
		}

		/// <summary>
		/// Record the change of checkstate for the given object in the model.
		/// This does not update the UI -- only the model
		/// </summary>
		/// <param name="modelObject"></param>
		/// <param name="state"></param>
		/// <returns>The check state that was recorded and that should be used to update
		/// the control.</returns>
		protected virtual CheckState PutCheckState(object modelObject, CheckState state)
		{
			if (CheckStatePutter != null)
				return CheckStatePutter(modelObject, state);
			return state;
		}

		/// <summary>
		/// Change the check state of the given object to be the given state.
		/// </summary>
		/// <remarks>
		/// If the given model object isn't in the list, we still try to remember
		/// its state, in case it is referenced in the future.</remarks>
		/// <param name="modelObject"></param>
		/// <param name="state"></param>
		/// <returns>True if the checkedness of the model changed</returns>
		protected virtual bool SetObjectCheckedness(object modelObject, CheckState state)
		{

			if (GetCheckState(modelObject) == state)
				return false;

			OLVListItem olvi = ModelToItem(modelObject);

			// If we didn't find the given, we still try to record the check state.
			if (olvi == null)
			{
				PutCheckState(modelObject, state);
				return true;
			}

			// Trigger checkbox changing event
			ItemCheckEventArgs ice = new ItemCheckEventArgs(olvi.Index, state, olvi.CheckState);
			OnItemCheck(ice);
			if (ice.NewValue == olvi.CheckState)
				return false;

			olvi.CheckState = PutCheckState(modelObject, state);
			RefreshItem(olvi);

			// Trigger check changed event
			OnItemChecked(new ItemCheckedEventArgs(olvi));
			return true;
		}

		/// <summary>
		/// Toggle the checkedness of the given object. A checked object becomes
		/// unchecked; an unchecked or indeterminate object becomes checked.
		/// If the list has tristate checkboxes, the order is:
		///    unchecked -> checked -> indeterminate -> unchecked ...
		/// </summary>
		/// <param name="modelObject">The model object to be checked</param>
		public virtual void ToggleCheckObject(object modelObject)
		{
			OLVListItem olvi = ModelToItem(modelObject);
			if (olvi == null)
				return;

			CheckState newState = CheckState.Checked;

			if (olvi.CheckState == CheckState.Checked)
			{
				newState = TriStateCheckBoxes ? CheckState.Indeterminate : CheckState.Unchecked;
			}
			else
			{
				if (olvi.CheckState == CheckState.Indeterminate && TriStateCheckBoxes)
					newState = CheckState.Unchecked;
			}
			SetObjectCheckedness(modelObject, newState);
		}

		/// <summary>
		/// Toggle the checkbox in the header of the given column
		/// </summary>
		/// <remarks>Obviously, this is only useful if the column actually has a header checkbox.</remarks>
		/// <param name="column"></param>
		public virtual void ToggleHeaderCheckBox(OLVColumn column)
		{
			if (column == null)
				return;

			CheckState newState = CalculateToggledCheckState(column.HeaderCheckState, column.HeaderTriStateCheckBox, column.HeaderCheckBoxDisabled);
			ChangeHeaderCheckBoxState(column, newState);
		}

		private void ChangeHeaderCheckBoxState(OLVColumn column, CheckState newState)
		{
			// Tell the world the checkbox was clicked
			HeaderCheckBoxChangingEventArgs args = new HeaderCheckBoxChangingEventArgs();
			args.Column = column;
			args.NewCheckState = newState;

			OnHeaderCheckBoxChanging(args);
			if (args.Cancel || column.HeaderCheckState == args.NewCheckState)
				return;

			Stopwatch sw = Stopwatch.StartNew();

			column.HeaderCheckState = args.NewCheckState;
			HeaderControl.Invalidate(column);

			if (column.HeaderCheckBoxUpdatesRowCheckBoxes)
			{
				if (column.Index == 0)
					UpdateAllPrimaryCheckBoxes(column);
				else
					UpdateAllSubItemCheckBoxes(column);
			}

			Debug.WriteLine(string.Format("PERF - Changing row checkboxes on {2} objects took {0}ms / {1} ticks", sw.ElapsedMilliseconds, sw.ElapsedTicks, GetItemCount()));
		}

		private void UpdateAllPrimaryCheckBoxes(OLVColumn column)
		{
			if (!CheckBoxes || column.HeaderCheckState == CheckState.Indeterminate)
				return;

			if (column.HeaderCheckState == CheckState.Checked)
				CheckAll();
			else
				UncheckAll();
		}

		private void UpdateAllSubItemCheckBoxes(OLVColumn column)
		{
			if (!column.CheckBoxes || column.HeaderCheckState == CheckState.Indeterminate)
				return;

			foreach (object model in Objects)
				column.PutCheckState(model, column.HeaderCheckState);
			BuildList(true);
		}

		/// <summary>
		/// Toggle the check at the check box of the given cell
		/// </summary>
		/// <param name="rowObject"></param>
		/// <param name="column"></param>
		public virtual void ToggleSubItemCheckBox(object rowObject, OLVColumn column)
		{
			CheckState currentState = column.GetCheckState(rowObject);
			CheckState newState = CalculateToggledCheckState(currentState, column.TriStateCheckBoxes, false);

			SubItemCheckingEventArgs args = new SubItemCheckingEventArgs(column, ModelToItem(rowObject), column.Index, currentState, newState);
			OnSubItemChecking(args);
			if (args.Canceled)
				return;

			switch (args.NewValue)
			{
				case CheckState.Checked:
					CheckSubItem(rowObject, column);
					break;
				case CheckState.Indeterminate:
					CheckIndeterminateSubItem(rowObject, column);
					break;
				case CheckState.Unchecked:
					UncheckSubItem(rowObject, column);
					break;
			}
		}

		/// <summary>
		/// Uncheck all rows
		/// </summary>
		public virtual void UncheckAll()
		{
			CheckedObjects = null;
		}

		/// <summary>
		/// Mark the given object as unchecked in the list
		/// </summary>
		/// <param name="modelObject">The model object to be unchecked</param>
		public virtual void UncheckObject(object modelObject)
		{
			SetObjectCheckedness(modelObject, CheckState.Unchecked);
		}

		/// <summary>
		/// Mark the given objects as unchecked in the list
		/// </summary>
		/// <param name="modelObjects">The model object to be checked</param>
		public virtual void UncheckObjects(IEnumerable<object> modelObjects)
		{
			foreach (object model in modelObjects)
				UncheckObject(model);
		}

		/// <summary>
		/// Uncheck the checkbox in the given column header
		/// </summary>
		/// <param name="column"></param>
		public virtual void UncheckHeaderCheckBox(OLVColumn column)
		{
			if (column == null)
				return;

			ChangeHeaderCheckBoxState(column, CheckState.Unchecked);
		}

		/// <summary>
		/// Uncheck the check at the given cell
		/// </summary>
		/// <param name="rowObject"></param>
		/// <param name="column"></param>
		public virtual void UncheckSubItem(object rowObject, OLVColumn column)
		{
			if (column == null || rowObject == null || !column.CheckBoxes)
				return;

			column.PutCheckState(rowObject, CheckState.Unchecked);
			RefreshObject(rowObject);
		}

		#endregion

		#region OLV accessing

		/// <summary>
		/// Return the column at the given index
		/// </summary>
		/// <param name="index">Index of the column to be returned</param>
		/// <returns>An OLVColumn</returns>
		public virtual OLVColumn GetColumn(int index)
		{
			return (OLVColumn)Columns[index];
		}

		/// <summary>
		/// Return the column at the given title.
		/// </summary>
		/// <param name="name">Name of the column to be returned</param>
		/// <returns>An OLVColumn</returns>
		public virtual OLVColumn GetColumn(string name)
		{
			foreach (ColumnHeader column in Columns)
			{
				if (column.Text == name)
					return (OLVColumn)column;
			}
			return null;
		}

		/// <summary>
		/// Return a collection of columns that are visible to the given view.
		/// Only Tile and Details have columns; all other views have 0 columns.
		/// </summary>
		/// <param name="view">Which view are the columns being calculate for?</param>
		/// <returns>A list of columns</returns>
		public virtual List<OLVColumn> GetFilteredColumns(View view)
		{
			// For both detail and tile view, the first column must be included. Normally, we would
			// use the ColumnHeader.Index property, but if the header is not currently part of a ListView
			// that property returns -1. So, we track the index of
			// the column header, and always include the first header.

			int index = 0;
			return AllColumns.FindAll(delegate (OLVColumn x)
			{
				return (index++ == 0) || x.IsVisible;
			});
		}

		/// <summary>
		/// Return the number of items in the list
		/// </summary>
		/// <returns>the number of items in the list</returns>
		/// <remarks>If a filter is installed, this will return the number of items that match the filter.</remarks>
		public virtual int GetItemCount()
		{
			return Items.Count;
		}

		/// <summary>
		/// Return the item at the given index
		/// </summary>
		/// <param name="index">Index of the item to be returned</param>
		/// <returns>An OLVListItem</returns>
		public virtual OLVListItem GetItem(int index)
		{
			if (index < 0 || index >= GetItemCount())
				return null;

			return (OLVListItem)Items[index];
		}

		/// <summary>
		/// Return the model object at the given index
		/// </summary>
		/// <param name="index">Index of the model object to be returned</param>
		/// <returns>A model object</returns>
		public virtual object GetModelObject(int index)
		{
			OLVListItem item = GetItem(index);
			return item == null ? null : item.RowObject;
		}

		/// <summary>
		/// Find the item and column that are under the given co-ords
		/// </summary>
		/// <param name="x">X co-ord</param>
		/// <param name="y">Y co-ord</param>
		/// <param name="hitColumn">The column under the given point</param>
		/// <returns>The item under the given point. Can be null.</returns>
		public virtual OLVListItem GetItemAt(int x, int y, out OLVColumn hitColumn)
		{
			hitColumn = null;
			ListViewHitTestInfo info = HitTest(x, y);
			if (info.Item == null)
				return null;

			if (info.SubItem != null)
			{
				int subItemIndex = info.Item.SubItems.IndexOf(info.SubItem);
				hitColumn = GetColumn(subItemIndex);
			}

			return (OLVListItem)info.Item;
		}

		/// <summary>
		/// Return the sub item at the given index/column
		/// </summary>
		/// <param name="index">Index of the item to be returned</param>
		/// <param name="columnIndex">Index of the subitem to be returned</param>
		/// <returns>An OLVListSubItem</returns>
		public virtual OLVListSubItem GetSubItem(int index, int columnIndex)
		{
			OLVListItem olvi = GetItem(index);
			return olvi == null ? null : olvi.GetSubItem(columnIndex);
		}

		#endregion

		#region Object manipulation

		/// <summary>
		/// Find the given model object within the listview and return its index
		/// </summary>
		/// <param name="modelObject">The model object to be found</param>
		/// <returns>The index of the object. -1 means the object was not present</returns>
		public virtual int IndexOf(object modelObject)
		{
			for (int i = 0; i < GetItemCount(); i++)
			{
				if (GetModelObject(i).Equals(modelObject))
					return i;
			}
			return -1;
		}

		/// <summary>
		/// Rebuild the given ListViewItem with the data from its associated model.
		/// </summary>
		/// <remarks>This method does not resort or regroup the view. It simply updates
		/// the displayed data of the given item</remarks>
		public virtual void RefreshItem(OLVListItem olvi)
		{
			olvi.UseItemStyleForSubItems = true;
			olvi.SubItems.Clear();
			FillInValues(olvi, olvi.RowObject);
			PostProcessOneRow(olvi.Index, GetDisplayOrderOfItemIndex(olvi.Index), olvi);
		}

		/// <summary>
		/// Rebuild the data on the row that is showing the given object.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This method does not resort or regroup the view.
		/// </para>
		/// <para>
		/// The given object is *not* used as the source of data for the rebuild.
		/// It is only used to locate the matching model in the <see cref="Objects"/> collection,
		/// then that matching model is used as the data source. This distinction is
		/// only important in model classes that have overridden the Equals() method.
		/// </para>
		/// <para>
		/// If you want the given model object to replace the pre-existing model,
		/// use <see cref="UpdateObject"/>. 
		/// </para>
		/// </remarks>
		public virtual void RefreshObject(object modelObject)
		{
			RefreshObjects(new object[] { modelObject });
		}

		/// <summary>
		/// Update the rows that are showing the given objects
		/// </summary>
		/// <remarks>
		/// <para>This method does not resort or regroup the view.</para>
		/// <para>This method can safely be called from background threads.</para>
		/// </remarks>
		public virtual void RefreshObjects(IList<object> modelObjects)
		{
			if (InvokeRequired)
			{
				Invoke((MethodInvoker)delegate { RefreshObjects(modelObjects); });
				return;
			}
			foreach (object modelObject in modelObjects)
			{
				OLVListItem olvi = ModelToItem(modelObject);
				if (olvi != null)
				{
					ReplaceModel(olvi, modelObject);
					RefreshItem(olvi);
				}
			}
		}

		private void ReplaceModel(OLVListItem olvi, object newModel)
		{
			if (ReferenceEquals(olvi.RowObject, newModel))
				return;

			TakeOwnershipOfObjects();
			var array = EnumerableToArray(Objects, false);
			int i = array.IndexOf(olvi.RowObject);
			if (i >= 0)
				array[i] = newModel;

			olvi.RowObject = newModel;
		}

		/// <summary>
		/// Update the rows that are selected
		/// </summary>
		/// <remarks>This method does not resort or regroup the view.</remarks>
		public virtual void RefreshSelectedObjects()
		{
			foreach (ListViewItem lvi in SelectedItems)
				RefreshItem((OLVListItem)lvi);
		}

		/// <summary>
		/// Select the row that is displaying the given model object, in addition to any current selection.
		/// </summary>
		/// <param name="modelObject">The object to be selected</param>
		/// <remarks>Use the <see cref="SelectedObject"/> property to deselect all other rows</remarks>
		public virtual void SelectObject(object modelObject)
		{
			SelectObject(modelObject, false);
		}

		/// <summary>
		/// Select the row that is displaying the given model object, in addition to any current selection.
		/// </summary>
		/// <param name="modelObject">The object to be selected</param>
		/// <param name="setFocus">Should the object be focused as well?</param>
		/// <remarks>Use the <see cref="SelectedObject"/> property to deselect all other rows</remarks>
		public virtual void SelectObject(object modelObject, bool setFocus)
		{
			OLVListItem olvi = ModelToItem(modelObject);
			if (olvi != null && olvi.Enabled)
			{
				olvi.Selected = true;
				if (setFocus)
					olvi.Focused = true;
			}
		}

		/// <summary>
		/// Select the rows that is displaying any of the given model object. All other rows are deselected.
		/// </summary>
		/// <param name="modelObjects">A collection of model objects</param>
		public virtual void SelectObjects(IList<object> modelObjects)
		{
			SelectedIndices.Clear();

			if (modelObjects == null)
				return;

			foreach (object modelObject in modelObjects)
			{
				OLVListItem olvi = ModelToItem(modelObject);
				if (olvi != null && olvi.Enabled)
					olvi.Selected = true;
			}
		}

		#endregion

		#region Freezing/Suspending

		/// <summary>
		/// Returns true if selection events are currently suspended.
		/// While selection events are suspended, neither SelectedIndexChanged
		/// or SelectionChanged events will be raised.
		/// </summary>
		[Browsable(false),
		DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		protected bool SelectionEventsSuspended
		{
			get { return suspendSelectionEventCount > 0; }
		}

		/// <summary>
		/// Suspend selection events until a matching ResumeSelectionEvents()
		/// is called.
		/// </summary>
		/// <remarks>Calls to this method nest correctly. Every call to SuspendSelectionEvents()
		/// must have a matching ResumeSelectionEvents().</remarks>
		protected void SuspendSelectionEvents()
		{
			suspendSelectionEventCount++;
		}

		/// <summary>
		/// Resume raising selection events.
		/// </summary>
		protected void ResumeSelectionEvents()
		{
			Debug.Assert(SelectionEventsSuspended, "Mismatched called to ResumeSelectionEvents()");
			suspendSelectionEventCount--;
		}

		/// <summary>
		/// Returns a disposable that will disable selection events
		/// during a using() block.
		/// </summary>
		/// <returns></returns>
		protected IDisposable SuspendSelectionEventsDuring()
		{
			return new SuspendSelectionDisposable(this);
		}

		/// <summary>
		/// Implementation only class that suspends and resumes selection
		/// events on instance creation and disposal.
		/// </summary>
		private class SuspendSelectionDisposable : IDisposable
		{
			public SuspendSelectionDisposable(ObjectListView objectListView)
			{
				this.objectListView = objectListView;
				this.objectListView.SuspendSelectionEvents();
			}

			public void Dispose()
			{
				objectListView.ResumeSelectionEvents();
			}

			private readonly ObjectListView objectListView;
		}

		#endregion

		#region Column sorting

		/// <summary>
		/// Sort the items by the last sort column and order
		/// </summary>
		new public void Sort()
		{
			Sort(PrimarySortColumn, PrimarySortOrder);
		}

		/// <summary>
		/// Sort the items in the list view by the values in the given column and the last sort order
		/// </summary>
		/// <param name="columnToSortIndex">The index of the column whose values will be used for the sorting</param>
		public virtual void Sort(int columnToSortIndex)
		{
			if (columnToSortIndex >= 0 && columnToSortIndex < Columns.Count)
				Sort(GetColumn(columnToSortIndex), PrimarySortOrder);
		}

		/// <summary>
		/// Sort the items in the list view by the values in the given column and the last sort order
		/// </summary>
		/// <param name="columnToSort">The column whose values will be used for the sorting</param>
		public virtual void Sort(OLVColumn columnToSort)
		{
			if (InvokeRequired)
			{
				Invoke((MethodInvoker)delegate { Sort(columnToSort); });
			}
			else
			{
				Sort(columnToSort, PrimarySortOrder);
			}
		}

		/// <summary>
		/// Sort the items in the list view by the values in the given column and by the given order.
		/// </summary>
		/// <param name="columnToSort">The column whose values will be used for the sorting.
		/// If null, the first column will be used.</param>
		/// <param name="order">The ordering to be used for sorting. If this is None,
		/// this.Sorting and then SortOrder.Ascending will be used</param>
		/// <remarks>If ShowGroups is true, the rows will be grouped by the given column.
		/// If AlwaysGroupsByColumn is not null, the rows will be grouped by that column,
		/// and the rows within each group will be sorted by the given column.</remarks>
		public virtual void Sort(OLVColumn columnToSort, SortOrder order)
		{
			if (InvokeRequired)
			{
				Invoke((MethodInvoker)delegate { Sort(columnToSort, order); });
			}
			else
			{
				DoSort(columnToSort, order);
				PostProcessRows();
			}
		}

		private void DoSort(OLVColumn columnToSort, SortOrder order)
		{
			// Sanity checks
			if (GetItemCount() == 0 || Columns.Count == 0)
				return;

			// Fill in default values, if the parameters don't make sense
			if (ShowGroups)
			{
				columnToSort = columnToSort ?? GetColumn(0);
				if (order == SortOrder.None)
				{
					order = Sorting;
					if (order == SortOrder.None)
						order = SortOrder.Ascending;
				}
			}

			// Give the world a chance to fiddle with or completely avoid the sorting process
			BeforeSortingEventArgs args = BuildBeforeSortingEventArgs(columnToSort, order);
			OnBeforeSorting(args);
			if (args.Canceled)
				return;

			// Virtual lists don't preserve selection, so we have to do it specifically
			// THINK: Do we need to preserve focus too?
			IList<object> selection = VirtualMode ? SelectedObjects : null;
			SuspendSelectionEvents();

			// Finally, do the work of sorting, unless an event handler has already done the sorting for us
			if (!args.Handled)
			{
				// Sanity checks
				if (args.ColumnToSort != null && args.SortOrder != SortOrder.None)
				{
					if (CustomSorter != null)
						CustomSorter(args.ColumnToSort, args.SortOrder);
					else
						ListViewItemSorter = new ColumnComparer(args.ColumnToSort, args.SortOrder,
							args.SecondaryColumnToSort, args.SecondarySortOrder);
				}
			}

			if (ShowSortIndicators)
				ShowSortIndicator(args.ColumnToSort, args.SortOrder);

			PrimarySortColumn = args.ColumnToSort;
			PrimarySortOrder = args.SortOrder;

			if (selection != null && selection.Count > 0)
				SelectedObjects = selection;
			ResumeSelectionEvents();

			OnAfterSorting(new AfterSortingEventArgs(args));
		}

		/// <summary>
		/// Put a sort indicator next to the text of the sort column
		/// </summary>
		public virtual void ShowSortIndicator()
		{
			if (ShowSortIndicators && PrimarySortOrder != SortOrder.None)
				ShowSortIndicator(PrimarySortColumn, PrimarySortOrder);
		}

		/// <summary>
		/// Put a sort indicator next to the text of the given given column
		/// </summary>
		/// <param name="columnToSort">The column to be marked</param>
		/// <param name="sortOrder">The sort order in effect on that column</param>
		protected virtual void ShowSortIndicator(OLVColumn columnToSort, SortOrder sortOrder)
		{
			int imageIndex = -1;

			if (!NativeMethods.HasBuiltinSortIndicators())
			{
				// If we can't use builtin image, we have to make and then locate the index of the
				// sort indicator we want to use. SortOrder.None doesn't show an image.
				if (SmallImageList == null || !SmallImageList.Images.ContainsKey(SORT_INDICATOR_UP_KEY))
					MakeSortIndicatorImages();

				if (SmallImageList != null)
				{
					string key = sortOrder == SortOrder.Ascending ? SORT_INDICATOR_UP_KEY : SORT_INDICATOR_DOWN_KEY;
					imageIndex = SmallImageList.Images.IndexOfKey(key);
				}
			}

			// Set the image for each column
			for (int i = 0; i < Columns.Count; i++)
			{
				if (columnToSort != null && i == columnToSort.Index)
					NativeMethods.SetColumnImage(this, i, sortOrder, imageIndex);
				else
					NativeMethods.SetColumnImage(this, i, SortOrder.None, -1);
			}
		}

		/// <summary>
		/// The name of the image used when a column is sorted ascending
		/// </summary>
		/// <remarks>This image is only used on pre-XP systems. System images are used for XP and later</remarks>
		public const string SORT_INDICATOR_UP_KEY = "sort-indicator-up";

		/// <summary>
		/// The name of the image used when a column is sorted descending
		/// </summary>
		/// <remarks>This image is only used on pre-XP systems. System images are used for XP and later</remarks>
		public const string SORT_INDICATOR_DOWN_KEY = "sort-indicator-down";

		/// <summary>
		/// If the sort indicator images don't already exist, this method will make and install them
		/// </summary>
		protected virtual void MakeSortIndicatorImages()
		{
			// Don't mess with the image list in design mode
			if (DesignMode)
				return;

			ImageList il = SmallImageList;
			if (il == null)
			{
				il = new ImageList();
				il.ImageSize = new Size(16, 16);
				il.ColorDepth = ColorDepth.Depth32Bit;
			}

			// This arrangement of points works well with (16,16) images, and OK with others
			int midX = il.ImageSize.Width / 2;
			int midY = (il.ImageSize.Height / 2) - 1;
			int deltaX = midX - 2;
			int deltaY = deltaX / 2;

			if (il.Images.IndexOfKey(SORT_INDICATOR_UP_KEY) == -1)
			{
				Point pt1 = new Point(midX - deltaX, midY + deltaY);
				Point pt2 = new Point(midX, midY - deltaY - 1);
				Point pt3 = new Point(midX + deltaX, midY + deltaY);
				il.Images.Add(SORT_INDICATOR_UP_KEY, MakeTriangleBitmap(il.ImageSize, new Point[] { pt1, pt2, pt3 }));
			}

			if (il.Images.IndexOfKey(SORT_INDICATOR_DOWN_KEY) == -1)
			{
				Point pt1 = new Point(midX - deltaX, midY - deltaY);
				Point pt2 = new Point(midX, midY + deltaY);
				Point pt3 = new Point(midX + deltaX, midY - deltaY);
				il.Images.Add(SORT_INDICATOR_DOWN_KEY, MakeTriangleBitmap(il.ImageSize, new Point[] { pt1, pt2, pt3 }));
			}

			SmallImageList = il;
		}

		private Bitmap MakeTriangleBitmap(Size sz, Point[] pts)
		{
			Bitmap bm = new Bitmap(sz.Width, sz.Height);
			Graphics g = Graphics.FromImage(bm);
			g.FillPolygon(new SolidBrush(Color.Gray), pts);
			return bm;
		}

		/// <summary>
		/// Remove any sorting and revert to the given order of the model objects
		/// </summary>
		public virtual void Unsort()
		{
			ShowGroups = false;
			PrimarySortColumn = null;
			PrimarySortOrder = SortOrder.None;
			BuildList();
		}

		#endregion

		#region Utilities

		private static CheckState CalculateToggledCheckState(CheckState currentState, bool isTriState, bool isDisabled)
		{
			if (isDisabled)
				return currentState;
			switch (currentState)
			{
				case CheckState.Checked: return isTriState ? CheckState.Indeterminate : CheckState.Unchecked;
				case CheckState.Indeterminate: return CheckState.Unchecked;
				default: return CheckState.Checked;
			}
		}

		/// <summary>
		/// Fill in the given OLVListItem with values of the given row
		/// </summary>
		/// <param name="lvi">the OLVListItem that is to be stuff with values</param>
		/// <param name="rowObject">the model object from which values will be taken</param>
		protected virtual void FillInValues(OLVListItem lvi, object rowObject)
		{
			if (Columns.Count == 0)
				return;

			OLVListSubItem subItem = MakeSubItem(rowObject, GetColumn(0));
			lvi.SubItems[0] = subItem;
			lvi.ImageSelector = subItem.ImageSelector;

			// Give the item the same font/colors as the control
			lvi.Font = Font;
			lvi.BackColor = BackColor;
			lvi.ForeColor = ForeColor;

			// Should the row be selectable?
			lvi.Enabled = !IsDisabled(rowObject);

			// Only Details and Tile views have subitems
			switch (View)
			{
				case View.Details:
					for (int i = 1; i < Columns.Count; i++)
					{
						lvi.SubItems.Add(MakeSubItem(rowObject, GetColumn(i)));
					}
					break;
				case View.Tile:
					for (int i = 1; i < Columns.Count; i++)
					{
						OLVColumn column = GetColumn(i);
						if (column.IsTileViewColumn)
							lvi.SubItems.Add(MakeSubItem(rowObject, column));
					}
					break;
			}

			// Should the row be selectable?
			if (!lvi.Enabled)
			{
				lvi.UseItemStyleForSubItems = false;
				ApplyRowStyle(lvi, DisabledItemStyle ?? ObjectListView.DefaultDisabledItemStyle);
			}

			// Set the check state of the row, if we are showing check boxes
			if (CheckBoxes)
			{
				CheckState? state = GetCheckState(lvi.RowObject);
				if (state.HasValue)
					lvi.CheckState = state.Value;
			}
		}

		private OLVListSubItem MakeSubItem(object rowObject, OLVColumn column)
		{
			object cellValue = column.GetValue(rowObject);
			OLVListSubItem subItem = new OLVListSubItem(cellValue,
														column.ValueToString(cellValue),
														column.GetImage(rowObject));
			return subItem;
		}

		/// <summary>
		/// Make sure the ListView has the extended style that says to display subitem images.
		/// </summary>
		/// <remarks>This method must be called after any .NET call that update the extended styles
		/// since they seem to erase this setting.</remarks>
		protected virtual void ForceSubItemImagesExStyle()
		{
			// Virtual lists can't show subitem images natively, so don't turn on this flag
			if (!VirtualMode)
				NativeMethods.ForceSubItemImagesExStyle(this);
		}

		/// <summary>
		/// Convert the given image selector to an index into our image list.
		/// Return -1 if that's not possible
		/// </summary>
		/// <param name="imageSelector"></param>
		/// <returns>Index of the image in the imageList, or -1</returns>
		protected virtual int GetActualImageIndex(object imageSelector)
		{
			if (imageSelector == null)
				return -1;

			if (imageSelector is Int32)
				return (int)imageSelector;

			string imageSelectorAsString = imageSelector as string;
			if (imageSelectorAsString != null && SmallImageList != null)
				return SmallImageList.Images.IndexOfKey(imageSelectorAsString);

			return -1;
		}

		/// <summary>
		/// Return the tooltip that should be shown when the mouse is hovered over the given cell
		/// </summary>
		/// <param name="columnIndex">The column index whose tool tip is to be fetched</param>
		/// <param name="rowIndex">The row index whose tool tip is to be fetched</param>
		/// <returns>A string or null if no tool tip is to be shown</returns>
		public virtual string GetCellToolTip(int columnIndex, int rowIndex)
		{
			if (CellToolTipGetter != null)
				return CellToolTipGetter(GetColumn(columnIndex), GetModelObject(rowIndex));

			return null;
		}

		/// <summary>
		/// Return the OLVListItem that displays the given model object
		/// </summary>
		/// <param name="modelObject">The modelObject whose item is to be found</param>
		/// <returns>The OLVListItem that displays the model, or null</returns>
		/// <remarks>This method has O(n) performance.</remarks>
		public virtual OLVListItem ModelToItem(object modelObject)
		{
			if (modelObject == null)
				return null;

			foreach (OLVListItem olvi in Items)
			{
				if (olvi.RowObject != null && olvi.RowObject.Equals(modelObject))
					return olvi;
			}
			return null;
		}

		/// <summary>
		/// Do the work required after the items in a listview have been created
		/// </summary>
		protected virtual void PostProcessRows()
		{
			int i = 0;
			if (ShowGroups)
			{
				foreach (ListViewGroup group in Groups)
				{
					foreach (OLVListItem olvi in group.Items)
					{
						PostProcessOneRow(olvi.Index, i, olvi);
						i++;
					}
				}
			}
			else
			{
				foreach (OLVListItem olvi in Items)
				{
					PostProcessOneRow(olvi.Index, i, olvi);
					i++;
				}
			}
		}

		/// <summary>
		/// Do the work required after one item in a listview have been created
		/// </summary>
		protected virtual void PostProcessOneRow(int rowIndex, int displayIndex, OLVListItem olvi)
		{
			if (ShowImagesOnSubItems && !VirtualMode)
				SetSubItemImages(rowIndex, olvi);
		}

		/// <summary>
		/// Tell the underlying list control which images to show against the subitems
		/// </summary>
		/// <param name="rowIndex">the index at which the item occurs</param>
		/// <param name="item">the item whose subitems are to be set</param>
		protected virtual void SetSubItemImages(int rowIndex, OLVListItem item)
		{
			SetSubItemImages(rowIndex, item, false);
		}

		/// <summary>
		/// Tell the underlying list control which images to show against the subitems
		/// </summary>
		/// <param name="rowIndex">the index at which the item occurs</param>
		/// <param name="item">the item whose subitems are to be set</param>
		/// <param name="shouldClearImages">will existing images be cleared if no new image is provided?</param>
		protected virtual void SetSubItemImages(int rowIndex, OLVListItem item, bool shouldClearImages)
		{
			if (!ShowImagesOnSubItems || OwnerDraw)
				return;

			for (int i = 1; i < item.SubItems.Count; i++)
			{
				SetSubItemImage(rowIndex, i, item.GetSubItem(i), shouldClearImages);
			}
		}

		/// <summary>
		/// Set the subitem image natively
		/// </summary>
		/// <param name="rowIndex"></param>
		/// <param name="subItemIndex"></param>
		/// <param name="subItem"></param>
		/// <param name="shouldClearImages"></param>
		public virtual void SetSubItemImage(int rowIndex, int subItemIndex, OLVListSubItem subItem, bool shouldClearImages)
		{
			int imageIndex = GetActualImageIndex(subItem.ImageSelector);
			if (shouldClearImages || imageIndex != -1)
				NativeMethods.SetSubItemImage(this, rowIndex, subItemIndex, imageIndex);
		}

		/// <summary>
		/// Take ownership of the 'objects' collection. This separats our collection from the source.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This method
		/// separates the 'objects' instance variable from its source, so that any AddObject/RemoveObject
		/// calls will modify our collection and not the original colleciton.
		/// </para>
		/// <para>
		/// This method has the intentional side-effect of converting our list of objects to an ArrayList.
		/// </para>
		/// </remarks>
		protected virtual void TakeOwnershipOfObjects()
		{
			if (isOwnerOfObjects)
				return;

			isOwnerOfObjects = true;

			objects = EnumerableToArray(objects, true);
		}

		/// <summary>
		/// Make the list forget everything -- all rows and all columns
		/// </summary>
		/// <remarks>Use <see cref="ClearObjects"/> if you want to remove just the rows.</remarks>
		public virtual void Reset()
		{
			Clear();
			AllColumns.Clear();
			ClearObjects();
			PrimarySortColumn = null;
			SecondarySortColumn = null;
			ClearDisabledObjects();
			ClearPersistentCheckState();
		}

		#endregion

		#region ISupportInitialize Members

		void ISupportInitialize.BeginInit()
		{

		}

		void ISupportInitialize.EndInit()
		{
			if (RowHeight != -1)
			{
				SmallImageList = SmallImageList;
				if (CheckBoxes)
					InitializeStateImageList();
			}

			if (UseSubItemCheckBoxes || (VirtualMode && CheckBoxes))
				SetupSubItemCheckBoxes();
		}

		#endregion

		#region Image list manipulation

		/// <summary>
		/// Update our externally visible image list so it holds the same images as our shadow list, but sized correctly
		/// </summary>
		private void SetupBaseImageList()
		{
			// If a row height hasn't been set, or an image list has been give which is the required size, just assign it
			if (rowHeight == -1 ||
				View != View.Details ||
				(shadowedImageList != null && shadowedImageList.ImageSize.Height == rowHeight))
				BaseSmallImageList = shadowedImageList;
			else
			{
				int width = (shadowedImageList == null ? 16 : shadowedImageList.ImageSize.Width);
				BaseSmallImageList = MakeResizedImageList(width, rowHeight, shadowedImageList);
			}
		}

		/// <summary>
		/// Return a copy of the given source image list, where each image has been resized to be height x height in size.
		/// If source is null, an empty image list of the given size is returned
		/// </summary>
		/// <param name="width">Height and width of the new images</param>
		/// <param name="height">Height and width of the new images</param>
		/// <param name="source">Source of the images (can be null)</param>
		/// <returns>A new image list</returns>
		private ImageList MakeResizedImageList(int width, int height, ImageList source)
		{
			ImageList il = new ImageList();
			il.ImageSize = new Size(width, height);

			// If there's nothing to copy, just return the new list
			if (source == null)
				return il;

			il.TransparentColor = source.TransparentColor;
			il.ColorDepth = source.ColorDepth;

			// Fill the imagelist with resized copies from the source
			for (int i = 0; i < source.Images.Count; i++)
			{
				Bitmap bm = MakeResizedImage(width, height, source.Images[i], source.TransparentColor);
				il.Images.Add(bm);
			}

			// Give each image the same key it has in the original
			foreach (string key in source.Images.Keys)
			{
				il.Images.SetKeyName(source.Images.IndexOfKey(key), key);
			}

			return il;
		}

		/// <summary>
		/// Return a bitmap of the given height x height, which shows the given image, centred.
		/// </summary>
		/// <param name="width">Height and width of new bitmap</param>
		/// <param name="height">Height and width of new bitmap</param>
		/// <param name="image">Image to be centred</param>
		/// <param name="transparent">The background color</param>
		/// <returns>A new bitmap</returns>
		private Bitmap MakeResizedImage(int width, int height, Image image, Color transparent)
		{
			Bitmap bm = new Bitmap(width, height);
			Graphics g = Graphics.FromImage(bm);
			g.Clear(transparent);
			int x = Math.Max(0, (bm.Size.Width - image.Size.Width) / 2);
			int y = Math.Max(0, (bm.Size.Height - image.Size.Height) / 2);
			g.DrawImage(image, x, y, image.Size.Width, image.Size.Height);
			return bm;
		}

		/// <summary>
		/// Initialize the state image list with the required checkbox images
		/// </summary>
		protected virtual void InitializeStateImageList()
		{
			if (DesignMode)
				return;

			if (!CheckBoxes)
				return;

			if (StateImageList == null)
			{
				StateImageList = new ImageList();
				StateImageList.ImageSize = new Size(16, RowHeight == -1 ? 16 : RowHeight);
				StateImageList.ColorDepth = ColorDepth.Depth32Bit;
			}

			if (RowHeight != -1 &&
				View == View.Details &&
				StateImageList.ImageSize.Height != RowHeight)
			{
				StateImageList = new ImageList();
				StateImageList.ImageSize = new Size(16, RowHeight);
				StateImageList.ColorDepth = ColorDepth.Depth32Bit;
			}

			// The internal logic of ListView cycles through the state images when the primary
			// checkbox is clicked. So we have to get exactly the right number of images in the 
			// image list.
			if (StateImageList.Images.Count == 0)
				AddCheckStateBitmap(StateImageList, UNCHECKED_KEY, CheckBoxState.UncheckedNormal);
			if (StateImageList.Images.Count <= 1)
				AddCheckStateBitmap(StateImageList, CHECKED_KEY, CheckBoxState.CheckedNormal);
			if (TriStateCheckBoxes && StateImageList.Images.Count <= 2)
				AddCheckStateBitmap(StateImageList, INDETERMINATE_KEY, CheckBoxState.MixedNormal);
			else
			{
				if (StateImageList.Images.ContainsKey(INDETERMINATE_KEY))
					StateImageList.Images.RemoveByKey(INDETERMINATE_KEY);
			}
		}

		/// <summary>
		/// The name of the image used when a check box is checked
		/// </summary>
		public const string CHECKED_KEY = "checkbox-checked";

		/// <summary>
		/// The name of the image used when a check box is unchecked
		/// </summary>
		public const string UNCHECKED_KEY = "checkbox-unchecked";

		/// <summary>
		/// The name of the image used when a check box is Indeterminate
		/// </summary>
		public const string INDETERMINATE_KEY = "checkbox-indeterminate";

		/// <summary>
		/// Setup this control so it can display check boxes on subitems
		/// (or primary checkboxes in virtual mode)
		/// </summary>
		/// <remarks>This gives the ListView a small image list, if it doesn't already have one.</remarks>
		public virtual void SetupSubItemCheckBoxes()
		{
			ShowImagesOnSubItems = true;
			if (SmallImageList == null || !SmallImageList.Images.ContainsKey(CHECKED_KEY))
				InitializeSubItemCheckBoxImages();
		}

		/// <summary>
		/// Make sure the small image list for this control has checkbox images 
		/// (used for sub-item checkboxes).
		/// </summary>
		/// <remarks>
		/// <para>
		/// This gives the ListView a small image list, if it doesn't already have one.
		/// </para>
		/// <para>
		/// ObjectListView has to manage checkboxes on subitems separate from the checkboxes on each row.
		/// The underlying ListView knows about the per-row checkboxes, and to make them work, OLV has to 
		/// correctly configure the StateImageList. However, the ListView cannot do checkboxes in subitems,
		/// so ObjectListView has to handle them in a differnt fashion. So, per-row checkboxes are controlled
		/// by images in the StateImageList, but per-cell checkboxes are handled by images in the SmallImageList.
		/// </para>
		/// </remarks>
		protected virtual void InitializeSubItemCheckBoxImages()
		{
			// Don't mess with the image list in design mode
			if (DesignMode)
				return;

			ImageList il = SmallImageList;
			if (il == null)
			{
				il = new ImageList();
				il.ImageSize = new Size(16, 16);
				il.ColorDepth = ColorDepth.Depth32Bit;
			}

			AddCheckStateBitmap(il, CHECKED_KEY, CheckBoxState.CheckedNormal);
			AddCheckStateBitmap(il, UNCHECKED_KEY, CheckBoxState.UncheckedNormal);
			AddCheckStateBitmap(il, INDETERMINATE_KEY, CheckBoxState.MixedNormal);

			SmallImageList = il;
		}

		private void AddCheckStateBitmap(ImageList il, string key, CheckBoxState boxState)
		{
			Bitmap b = new Bitmap(il.ImageSize.Width, il.ImageSize.Height);
			Graphics g = Graphics.FromImage(b);
			g.Clear(il.TransparentColor);
			Point location = new Point(b.Width / 2 - 5, b.Height / 2 - 6);
			CheckBoxRenderer.DrawCheckBox(g, location, boxState);
			il.Images.Add(key, b);
		}

		#endregion

		#region Owner drawing

		/// <summary>
		/// Owner draw the column header
		/// </summary>
		/// <param name="e"></param>
		protected override void OnDrawColumnHeader(DrawListViewColumnHeaderEventArgs e)
		{
			e.DrawDefault = true;
			base.OnDrawColumnHeader(e);
		}

		/// <summary>
		/// Owner draw the item
		/// </summary>
		/// <param name="e"></param>
		protected override void OnDrawItem(DrawListViewItemEventArgs e)
		{
			if (View == View.Details)
				e.DrawDefault = false;
			else
			{
				if (ItemRenderer == null)
					e.DrawDefault = true;
				else
				{
					object row = ((OLVListItem)e.Item).RowObject;
					e.DrawDefault = !ItemRenderer.RenderItem(e, e.Graphics, e.Bounds, row);
				}
			}

			if (e.DrawDefault)
				base.OnDrawItem(e);
		}

		/// <summary>
		/// Owner draw a single subitem
		/// </summary>
		/// <param name="e"></param>
		protected override void OnDrawSubItem(DrawListViewSubItemEventArgs e)
		{
			//System.Diagnostics.Debug.WriteLine(String.Format("OnDrawSubItem ({0}, {1})", e.ItemIndex, e.ColumnIndex));
			// Don't try to do owner drawing at design time
			if (DesignMode)
			{
				e.DrawDefault = true;
				return;
			}

			object rowObject = ((OLVListItem)e.Item).RowObject;

			// Calculate where the subitem should be drawn
			Rectangle r = e.Bounds;

			// Get the special renderer for this column. If there isn't one, use the default draw mechanism.
			OLVColumn column = GetColumn(e.ColumnIndex);
			IRenderer renderer = GetCellRenderer(rowObject, column);

			// Get a graphics context for the renderer to use.
			// But we have more complications. Virtual lists have a nasty habit of drawing column 0
			// whenever there is any mouse move events over a row, and doing it in an un-double-buffered manner,
			// which results in nasty flickers! There are also some unbuffered draw when a mouse is first
			// hovered over column 0 of a normal row. So, to avoid all complications,
			// we always manually double-buffer the drawing.
			// Except with Mono, which doesn't seem to handle double buffering at all :-(
			BufferedGraphics buffer = BufferedGraphicsManager.Current.Allocate(e.Graphics, r);
			Graphics g = buffer.Graphics;

			g.TextRenderingHint = ObjectListView.TextRenderingHint;
			g.SmoothingMode = ObjectListView.SmoothingMode;

			// Finally, give the renderer a chance to draw something
			e.DrawDefault = !renderer.RenderSubItem(e, g, r, rowObject);

			if (!e.DrawDefault)
				buffer.Render();
			buffer.Dispose();
		}

		#endregion

		#region OnEvent Handling

		/// <summary>
		/// We need the click count in the mouse up event, but that is always 1.
		/// So we have to remember the click count from the preceding mouse down event.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnMouseDown(MouseEventArgs e)
		{
			lastMouseDownClickCount = e.Clicks;
			base.OnMouseDown(e);
		}
		private int lastMouseDownClickCount;

		/// <summary>
		/// When the mouse leaves the control, remove any hot item highlighting
		/// </summary>
		/// <param name="e"></param>
		protected override void OnMouseLeave(EventArgs e)
		{
			base.OnMouseLeave(e);

			if (!Created)
				return;
		}

		// We could change the hot item on the mouse hover event, but it looks wrong.

		//protected override void OnMouseHover(EventArgs e) {
		//    System.Diagnostics.Debug.WriteLine(String.Format("OnMouseHover"));
		//    base.OnMouseHover(e);
		//    this.UpdateHotItem(this.PointToClient(Cursor.Position));
		//}

		/// <summary>
		/// When the mouse moves, we might need to change the hot item.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);

			if (Created)
				HandleMouseMove(e.Location);
		}

		internal void HandleMouseMove(Point pt)
		{

			//System.Diagnostics.Debug.WriteLine(String.Format("HandleMouseMove: {0}", pt));

			CellOverEventArgs args = new CellOverEventArgs();
			BuildCellEvent(args, pt);
			OnCellOver(args);
			MouseMoveHitTest = args.HitTest;
		}

		/// <summary>
		/// Check to see if we need to start editing a cell
		/// </summary>
		/// <param name="e"></param>
		protected override void OnMouseUp(MouseEventArgs e)
		{

			//System.Diagnostics.Debug.WriteLine(String.Format("OnMouseUp"));

			base.OnMouseUp(e);

			if (!Created)
				return;

			if (e.Button == MouseButtons.Right)
			{
				OnRightMouseUp(e);
				return;
			}

			// What event should we listen for to start cell editing?
			// ------------------------------------------------------
			//
			// We can't use OnMouseClick, OnMouseDoubleClick, OnClick, or OnDoubleClick
			// since they are not triggered for clicks on subitems without Full Row Select.
			//
			// We could use OnMouseDown, but selecting rows is done in OnMouseUp. This means
			// that if we start the editing during OnMouseDown, the editor will automatically
			// lose focus when mouse up happens.
			//

			// Tell the world about a cell click. If someone handles it, don't do anything else
			CellClickEventArgs args = new CellClickEventArgs();
			BuildCellEvent(args, e.Location);
			args.ClickCount = lastMouseDownClickCount;
			OnCellClick(args);
			if (args.Handled)
				return;

			// No one handled it so check to see if we should start editing.
			if (!ShouldStartCellEdit(e))
				return;

			// We only start the edit if the user clicked on the image or text.
			if (args.HitTest.HitTestLocation == HitTestLocation.Nothing)
				return;

			// We don't edit the primary column by single clicks -- only subitems.
			if (CellEditActivation == CellEditActivateMode.SingleClick && args.ColumnIndex <= 0)
				return;

			// Don't start a cell edit operation when the user clicks on the background of a checkbox column -- it just looks wrong.
			// If the user clicks on the actual checkbox, changing the checkbox state is handled elsewhere.
			if (args.Column != null && args.Column.CheckBoxes)
				return;

			EditSubItem(args.Item, args.ColumnIndex);
		}

		/// <summary>
		/// The user right clicked on the control
		/// </summary>
		/// <param name="e"></param>
		protected virtual void OnRightMouseUp(MouseEventArgs e)
		{
			CellRightClickEventArgs args = new CellRightClickEventArgs();
			BuildCellEvent(args, e.Location);
			OnCellRightClick(args);
			if (!args.Handled)
			{
				if (args.MenuStrip != null)
				{
					args.MenuStrip.Show(this, args.Location);
				}
			}
		}

		internal void BuildCellEvent(CellEventArgs args, Point location)
		{
			BuildCellEvent(args, location, OlvHitTest(location.X, location.Y));
		}

		internal void BuildCellEvent(CellEventArgs args, Point location, OlvListViewHitTestInfo hitTest)
		{
			args.HitTest = hitTest;
			args.ListView = this;
			args.Location = location;
			args.Item = hitTest.Item;
			args.SubItem = hitTest.SubItem;
			args.Model = hitTest.RowObject;
			args.ColumnIndex = hitTest.ColumnIndex;
			args.Column = hitTest.Column;
			if (hitTest.Item != null)
				args.RowIndex = hitTest.Item.Index;
			args.ModifierKeys = Control.ModifierKeys;

			// In non-details view, we want any hit on an item to act as if it was a hit
			// on column 0 -- which, effectively, it was.
			if (args.Item != null && args.ListView.View != View.Details)
			{
				args.ColumnIndex = 0;
				args.Column = args.ListView.GetColumn(0);
				args.SubItem = args.Item.GetSubItem(0);
			}
		}

		/// <summary>
		/// This method is called every time a row is selected or deselected. This can be
		/// a pain if the user shift-clicks 100 rows. We override this method so we can
		/// trigger one event for any number of select/deselects that come from one user action
		/// </summary>
		/// <param name="e"></param>
		protected override void OnSelectedIndexChanged(EventArgs e)
		{
			if (SelectionEventsSuspended)
				return;

			base.OnSelectedIndexChanged(e);

			// If we haven't already scheduled an event, schedule it to be triggered
			// By using idle time, we will wait until all select events for the same
			// user action have finished before triggering the event.
			if (!hasIdleHandler)
			{
				hasIdleHandler = true;
				RunWhenIdle(HandleApplicationIdle);
			}
		}

		/// <summary>
		/// Called when the handle of the underlying control is created
		/// </summary>
		/// <param name="e"></param>
		protected override void OnHandleCreated(EventArgs e)
		{
			//Debug.WriteLine("OnHandleCreated");
			base.OnHandleCreated(e);

			Invoke((MethodInvoker)OnControlCreated);
		}

		/// <summary>
		/// This method is called after the control has been fully created.
		/// </summary>
		protected virtual void OnControlCreated()
		{

			//Debug.WriteLine("OnControlCreated");

			// Force the header control to be created when the listview handle is
			HeaderControl hc = HeaderControl;
			hc.WordWrap = HeaderWordWrap;

			RememberDisplayIndicies();

			if (VirtualMode)
				ApplyExtendedStyles();
		}

		#endregion

		#region Cell editing

		/// <summary>
		/// Should we start editing the cell in response to the given mouse button event?
		/// </summary>
		/// <param name="e"></param>
		/// <returns></returns>
		protected virtual bool ShouldStartCellEdit(MouseEventArgs e)
		{
			if (IsCellEditing)
				return false;

			if (e.Button != MouseButtons.Left && e.Button != MouseButtons.Right)
				return false;

			if ((Control.ModifierKeys & (Keys.Shift | Keys.Control | Keys.Alt)) != 0)
				return false;

			if (lastMouseDownClickCount == 1 && (
				CellEditActivation == CellEditActivateMode.SingleClick ||
				CellEditActivation == CellEditActivateMode.SingleClickAlways))
				return true;

			return (lastMouseDownClickCount == 2 && CellEditActivation == CellEditActivateMode.DoubleClick);
		}

		/// <summary>
		/// Handle a key press on this control. We specifically look for F2 which edits the primary column,
		/// or a Tab character during an edit operation, which tries to start editing on the next (or previous) cell.
		/// </summary>
		/// <param name="keyData"></param>
		/// <returns></returns>
		protected override bool ProcessDialogKey(Keys keyData)
		{

			// Treat F2 as a request to edit the primary column
			if (keyData == Keys.F2)
			{
				EditSubItem((OLVListItem)FocusedItem, 0);
				return base.ProcessDialogKey(keyData);
			}

			return base.ProcessDialogKey(keyData);
		}

		/// <summary>
		/// Begin an edit operation on the given cell.
		/// </summary>
		/// <remarks>This performs various sanity checks and passes off the real work to StartCellEdit().</remarks>
		/// <param name="item">The row to be edited</param>
		/// <param name="subItemIndex">The index of the cell to be edited</param>
		public virtual void EditSubItem(OLVListItem item, int subItemIndex)
		{
			if (item == null)
				return;

			if (subItemIndex < 0 && subItemIndex >= item.SubItems.Count)
				return;

			if (CellEditActivation == CellEditActivateMode.None)
				return;

			if (!GetColumn(subItemIndex).IsEditable)
				return;

			if (!item.Enabled)
				return;

			StartCellEdit(item, subItemIndex);
		}

		/// <summary>
		/// Really start an edit operation on a given cell. The parameters are assumed to be sane.
		/// </summary>
		/// <param name="item">The row to be edited</param>
		/// <param name="subItemIndex">The index of the cell to be edited</param>
		public virtual void StartCellEdit(OLVListItem item, int subItemIndex)
		{
			OLVColumn column = GetColumn(subItemIndex);
			Control c = new TextBox
			{
				BorderStyle = BorderStyle.None
			};
			Rectangle cellBounds = CalculateCellBounds(item, subItemIndex);
			c.Bounds = CalculateCellEditorBounds(item, subItemIndex, c.PreferredSize);

			// Give the control the value from the model
			SetControlValue(c, column.GetValue(item.RowObject), column.GetStringValue(item.RowObject));

			// Give the outside world the chance to munge with the process
			CellEditEventArgs = new CellEditEventArgs(column, c, cellBounds, item, subItemIndex);
			OnCellEditStarting(CellEditEventArgs);
			if (CellEditEventArgs.Cancel)
				return;

			// The event handler may have completely changed the control, so we need to remember it
			cellEditor = CellEditEventArgs.Control;

			Invalidate();
			Controls.Add(cellEditor);
			ConfigureControl();
		}
		private Control cellEditor;
		internal CellEditEventArgs CellEditEventArgs;

		/// <summary>
		/// Calculate the bounds of the edit control for the given item/column
		/// </summary>
		/// <param name="item"></param>
		/// <param name="subItemIndex"></param>
		/// <param name="preferredSize"> </param>
		/// <returns></returns>
		public Rectangle CalculateCellEditorBounds(OLVListItem item, int subItemIndex, Size preferredSize)
		{
			Rectangle r = CalculateCellBounds(item, subItemIndex);

			// Calculate the width of the cell's current contents
			return OwnerDraw
				? CalculateCellEditorBoundsOwnerDrawn(item, subItemIndex, r, preferredSize)
				: CalculateCellEditorBoundsStandard(item, subItemIndex, r, preferredSize);
		}

		/// <summary>
		/// Calculate the bounds of the edit control for the given item/column, when the listview
		/// is being owner drawn.
		/// </summary>
		/// <param name="item"></param>
		/// <param name="subItemIndex"></param>
		/// <param name="r"></param>
		/// <param name="preferredSize"> </param>
		/// <returns>A rectangle that is the bounds of the cell editor</returns>
		protected Rectangle CalculateCellEditorBoundsOwnerDrawn(OLVListItem item, int subItemIndex, Rectangle r, Size preferredSize)
		{
			IRenderer renderer = View == View.Details
				? GetCellRenderer(item.RowObject, GetColumn(subItemIndex))
				: ItemRenderer;

			if (renderer == null)
				return r;

			using (Graphics g = CreateGraphics())
			{
				return renderer.GetEditRectangle(g, r, item, subItemIndex, preferredSize);
			}
		}

		/// <summary>
		/// Calculate the bounds of the edit control for the given item/column, when the listview
		/// is not being owner drawn.
		/// </summary>
		/// <param name="item"></param>
		/// <param name="subItemIndex"></param>
		/// <param name="cellBounds"></param>
		/// <param name="preferredSize"> </param>
		/// <returns>A rectangle that is the bounds of the cell editor</returns>
		protected Rectangle CalculateCellEditorBoundsStandard(OLVListItem item, int subItemIndex, Rectangle cellBounds, Size preferredSize)
		{
			if (View == View.Tile)
				return cellBounds;

			// Center the editor vertically
			if (cellBounds.Height != preferredSize.Height)
				cellBounds.Y += (cellBounds.Height - preferredSize.Height) / 2;

			// Only Details view needs more processing
			if (View != View.Details)
				return cellBounds;

			// Allow for image (if there is one). 
			int offset = 0;
			object imageSelector = null;
			if (subItemIndex == 0)
				imageSelector = item.ImageSelector;
			else
			{
				// We only check for subitem images if we are owner drawn or showing subitem images
				if (OwnerDraw || ShowImagesOnSubItems)
					imageSelector = item.GetSubItem(subItemIndex).ImageSelector;
			}
			if (GetActualImageIndex(imageSelector) != -1)
			{
				offset += SmallImageSize.Width + 2;
			}

			// Allow for checkbox
			if (CheckBoxes && StateImageList != null && subItemIndex == 0)
			{
				offset += StateImageList.ImageSize.Width + 2;
			}

			// Allow for indent (first column only)
			if (subItemIndex == 0 && item.IndentCount > 0)
			{
				offset += (SmallImageSize.Width * item.IndentCount);
			}

			// Do the adjustment
			if (offset > 0)
			{
				cellBounds.X += offset;
				cellBounds.Width -= offset;
			}

			return cellBounds;
		}

		/// <summary>
		/// Try to give the given value to the provided control. Fall back to assigning a string
		/// if the value assignment fails.
		/// </summary>
		/// <param name="control">A control</param>
		/// <param name="value">The value to be given to the control</param>
		/// <param name="stringValue">The string to be given if the value doesn't work</param>
		protected virtual void SetControlValue(Control control, object value, string stringValue)
		{
			// Handle combobox explicitly
			ComboBox cb = control as ComboBox;
			if (cb != null)
			{
				if (cb.Created)
					cb.SelectedValue = value;
				else
					BeginInvoke(new MethodInvoker(delegate
					{
						cb.SelectedValue = value;
					}));
				return;
			}

			// There wasn't a Value property, or we couldn't set it, so set the text instead
			try
			{
				string valueAsString = value as string;
				control.Text = valueAsString ?? stringValue;
			}
			catch (ArgumentOutOfRangeException)
			{
				// The value couldn't be set via the Text property.
			}
		}

		/// <summary>
		/// Setup the given control to be a cell editor
		/// </summary>
		protected virtual void ConfigureControl()
		{
			cellEditor.Validating += new CancelEventHandler(CellEditor_Validating);
			cellEditor.Select();
		}

		/// <summary>
		/// Return the value that the given control is showing
		/// </summary>
		/// <param name="control"></param>
		/// <returns></returns>
		protected virtual object GetControlValue(Control control)
		{
			if (control == null)
				return null;

			TextBox box = control as TextBox;
			if (box != null)
				return box.Text;

			ComboBox comboBox = control as ComboBox;
			if (comboBox != null)
				return comboBox.SelectedValue;

			CheckBox checkBox = control as CheckBox;
			if (checkBox != null)
				return checkBox.Checked;

			try
			{
				return control.GetType().InvokeMember("Value", BindingFlags.GetProperty, null, control, null);
			}
			catch (MissingMethodException)
			{ // Microsoft throws this
				return control.Text;
			}
			catch (MissingFieldException)
			{ // Mono throws this
				return control.Text;
			}
		}

		/// <summary>
		/// Called when the cell editor could be about to lose focus. Time to commit the change
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		protected virtual void CellEditor_Validating(object sender, CancelEventArgs e)
		{
			CellEditEventArgs.Cancel = false;
			CellEditEventArgs.NewValue = GetControlValue(cellEditor);
			OnCellEditorValidating(CellEditEventArgs);

			if (CellEditEventArgs.Cancel)
			{
				CellEditEventArgs.Control.Select();
				e.Cancel = true;
			}
			else
				FinishCellEdit();
		}

		/// <summary>
		/// Return the bounds of the given cell
		/// </summary>
		/// <param name="item">The row to be edited</param>
		/// <param name="subItemIndex">The index of the cell to be edited</param>
		/// <returns>A Rectangle</returns>
		public virtual Rectangle CalculateCellBounds(OLVListItem item, int subItemIndex)
		{

			// It seems on Win7, GetSubItemBounds() does not have the same problems with
			// column 0 that it did previously.

			// TODO - Check on XP

			if (View != View.Details)
				return GetItemRect(item.Index, ItemBoundsPortion.Label);

			Rectangle r = item.GetSubItemBounds(subItemIndex);
			r.Width -= 1;
			r.Height -= 1;
			return r;

			// We use ItemBoundsPortion.Label rather than ItemBoundsPortion.Item
			// since Label extends to the right edge of the cell, whereas Item gives just the
			// current text width.
			//return this.CalculateCellBounds(item, subItemIndex, ItemBoundsPortion.Label);
		}

		/// <summary>
		/// Stop editing a cell and throw away any changes.
		/// </summary>
		public virtual void CancelCellEdit()
		{
			if (!IsCellEditing)
				return;

			// Let the world know that the user has cancelled the edit operation
			CellEditEventArgs.Cancel = true;
			CellEditEventArgs.NewValue = GetControlValue(cellEditor);
			OnCellEditFinishing(CellEditEventArgs);

			// Now cleanup the editing process
			CleanupCellEdit(false, CellEditEventArgs.AutoDispose);
		}

		/// <summary>
		/// If a cell edit is in progress, finish the edit.
		/// </summary>
		/// <returns>Returns false if the finishing process was cancelled
		/// (i.e. the cell editor is still on screen)</returns>
		/// <remarks>This method does not guarantee that the editing will finish. The validation
		/// process can cause the finishing to be aborted. Developers should check the return value
		/// or use IsCellEditing property after calling this method to see if the user is still
		/// editing a cell.</remarks>
		public virtual bool PossibleFinishCellEditing()
		{
			return PossibleFinishCellEditing(false);
		}

		/// <summary>
		/// If a cell edit is in progress, finish the edit.
		/// </summary>
		/// <returns>Returns false if the finishing process was cancelled
		/// (i.e. the cell editor is still on screen)</returns>
		/// <remarks>This method does not guarantee that the editing will finish. The validation
		/// process can cause the finishing to be aborted. Developers should check the return value
		/// or use IsCellEditing property after calling this method to see if the user is still
		/// editing a cell.</remarks>
		/// <param name="expectingCellEdit">True if it is likely that another cell is going to be 
		/// edited immediately after this cell finishes editing</param>
		public virtual bool PossibleFinishCellEditing(bool expectingCellEdit)
		{
			if (!IsCellEditing)
				return true;

			CellEditEventArgs.Cancel = false;
			CellEditEventArgs.NewValue = GetControlValue(cellEditor);
			OnCellEditorValidating(CellEditEventArgs);

			if (CellEditEventArgs.Cancel)
				return false;

			FinishCellEdit(expectingCellEdit);

			return true;
		}

		/// <summary>
		/// Finish the cell edit operation, writing changed data back to the model object
		/// </summary>
		/// <remarks>This method does not trigger a Validating event, so it always finishes
		/// the cell edit.</remarks>
		public virtual void FinishCellEdit()
		{
			FinishCellEdit(false);
		}

		/// <summary>
		/// Finish the cell edit operation, writing changed data back to the model object
		/// </summary>
		/// <remarks>This method does not trigger a Validating event, so it always finishes
		/// the cell edit.</remarks>
		/// <param name="expectingCellEdit">True if it is likely that another cell is going to be 
		/// edited immediately after this cell finishes editing</param>
		public virtual void FinishCellEdit(bool expectingCellEdit)
		{
			if (!IsCellEditing)
				return;

			CellEditEventArgs.Cancel = false;
			CellEditEventArgs.NewValue = GetControlValue(cellEditor);
			OnCellEditFinishing(CellEditEventArgs);

			// If someone doesn't cancel the editing process, write the value back into the model
			if (!CellEditEventArgs.Cancel)
			{
				CellEditEventArgs.Column.PutValue(CellEditEventArgs.RowObject, CellEditEventArgs.NewValue);
				RefreshItem(CellEditEventArgs.ListViewItem);
			}

			CleanupCellEdit(expectingCellEdit, CellEditEventArgs.AutoDispose);

			// Tell the world that the cell has been edited
			OnCellEditFinished(CellEditEventArgs);
		}

		/// <summary>
		/// Remove all trace of any existing cell edit operation
		/// </summary>
		/// <param name="expectingCellEdit">True if it is likely that another cell is going to be 
		/// edited immediately after this cell finishes editing</param>
		/// <param name="disposeOfCellEditor">True if the cell editor should be disposed </param>
		protected virtual void CleanupCellEdit(bool expectingCellEdit, bool disposeOfCellEditor)
		{
			if (cellEditor == null)
				return;

			cellEditor.Validating -= new CancelEventHandler(CellEditor_Validating);

			Control soonToBeOldCellEditor = cellEditor;
			cellEditor = null;

			// Delay cleaning up the cell editor so that if we are immediately going to 
			// start a new cell edit (because the user pressed Tab) the new cell editor
			// has a chance to grab the focus. Without this, the ListView gains focus
			// momentarily (after the cell editor is remove and before the new one is created)
			// causing the list's selection to flash momentarily.
			EventHandler toBeRun = null;
			toBeRun = delegate (object sender, EventArgs e)
			{
				Application.Idle -= toBeRun;
				Controls.Remove(soonToBeOldCellEditor);
				if (disposeOfCellEditor)
					soonToBeOldCellEditor.Dispose();
				Invalidate();

				if (!IsCellEditing)
				{
					if (Focused)
						Select();
				}
			};

			// We only want to delay the removal of the control if we are expecting another cell
			// to be edited. Otherwise, we remove the control immediately.
			if (expectingCellEdit)
				RunWhenIdle(toBeRun);
			else
				toBeRun(null, null);
		}

		#endregion

		#region Hot row and cell handling

		/// <summary>
		/// Apply a style to the given row
		/// </summary>
		/// <param name="olvi"></param>
		/// <param name="style"></param>
		public virtual void ApplyRowStyle(OLVListItem olvi, IItemStyle style)
		{
			if (style == null)
				return;

			Font font = style.Font ?? olvi.Font;

			if (style.FontStyle != FontStyle.Regular)
				font = new Font(font ?? Font, style.FontStyle);

			if (!Equals(font, olvi.Font))
			{
				if (olvi.UseItemStyleForSubItems)
					olvi.Font = font;
				else
				{
					foreach (ListViewItem.ListViewSubItem x in olvi.SubItems)
						x.Font = font;
				}
			}

			if (!style.ForeColor.IsEmpty)
			{
				if (olvi.UseItemStyleForSubItems)
					olvi.ForeColor = style.ForeColor;
				else
				{
					foreach (ListViewItem.ListViewSubItem x in olvi.SubItems)
						x.ForeColor = style.ForeColor;
				}
			}

			if (!style.BackColor.IsEmpty)
			{
				if (olvi.UseItemStyleForSubItems)
					olvi.BackColor = style.BackColor;
				else
				{
					foreach (ListViewItem.ListViewSubItem x in olvi.SubItems)
						x.BackColor = style.BackColor;
				}
			}
		}

		#endregion

		#region Persistent check state

		/// <summary>
		/// Gets the checkedness of the given model.
		/// </summary>
		/// <param name="model">The model</param>
		/// <returns>The checkedness of the model. Defaults to unchecked.</returns>
		protected virtual CheckState GetPersistentCheckState(object model)
		{
			CheckState state;
			if (model != null && CheckStateMap.TryGetValue(model, out state))
				return state;
			return CheckState.Unchecked;
		}

		/// <summary>
		/// Remember the check state of the given model object
		/// </summary>
		/// <param name="model">The model to be remembered</param>
		/// <param name="state">The model's checkedness</param>
		/// <returns>The state given to the method</returns>
		protected virtual CheckState SetPersistentCheckState(object model, CheckState state)
		{
			if (model == null)
				return CheckState.Unchecked;

			CheckStateMap[model] = state;
			return state;
		}

		/// <summary>
		/// Forget any persistent checkbox state
		/// </summary>
		protected virtual void ClearPersistentCheckState()
		{
			CheckStateMap = null;
		}

		#endregion

		#region Implementation variables

		private bool isOwnerOfObjects; // does this ObjectListView own the Objects collection?
		private bool hasIdleHandler; // has an Idle handler already been installed?
		private bool hasResizeColumnsHandler; // has an idle handler been installed which will handle column resizing?
		private bool isInWmPaintEvent; // is a WmPaint event currently being handled?
		private bool shouldDoCustomDrawing; // should the list do its custom drawing?
		private bool isMarqueSelecting; // Is a marque selection in progress?
		private int suspendSelectionEventCount; // How many unmatched SuspendSelectionEvents() calls have been made?

		#endregion
	}
}