using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace BrightIdeasSoftware
{
	/// <summary>
	/// Wrapper for all native method calls on ListView controls
	/// </summary>
	internal static class NativeMethods
	{
		#region Constants

		private const int LVM_FIRST = 0x1000;
		private const int LVM_GETCOLUMN = LVM_FIRST + 95;
		private const int LVM_GETCOUNTPERPAGE = LVM_FIRST + 40;
		private const int LVM_GETGROUPINFO = LVM_FIRST + 149;
		private const int LVM_GETGROUPSTATE = LVM_FIRST + 92;
		private const int LVM_GETHEADER = LVM_FIRST + 31;
		private const int LVM_GETTOOLTIPS = LVM_FIRST + 78;
		private const int LVM_GETTOPINDEX = LVM_FIRST + 39;
		private const int LVM_HITTEST = LVM_FIRST + 18;
		private const int LVM_INSERTGROUP = LVM_FIRST + 145;
		private const int LVM_REMOVEALLGROUPS = LVM_FIRST + 160;
		private const int LVM_SCROLL = LVM_FIRST + 20;
		private const int LVM_SETBKIMAGE = LVM_FIRST + 0x8A;
		private const int LVM_SETCOLUMN = LVM_FIRST + 96;
		private const int LVM_SETEXTENDEDLISTVIEWSTYLE = LVM_FIRST + 54;
		private const int LVM_SETGROUPINFO = LVM_FIRST + 147;
		private const int LVM_SETGROUPMETRICS = LVM_FIRST + 155;
		private const int LVM_SETIMAGELIST = LVM_FIRST + 3;
		private const int LVM_SETITEM = LVM_FIRST + 76;
		private const int LVM_SETITEMCOUNT = LVM_FIRST + 47;
		private const int LVM_SETITEMSTATE = LVM_FIRST + 43;
		private const int LVM_SETSELECTEDCOLUMN = LVM_FIRST + 140;
		private const int LVM_SETTOOLTIPS = LVM_FIRST + 74;
		private const int LVM_SUBITEMHITTEST = LVM_FIRST + 57;
		private const int LVS_EX_SUBITEMIMAGES = 0x0002;

		private const int LVIF_TEXT = 0x0001;
		private const int LVIF_IMAGE = 0x0002;
		private const int LVIF_PARAM = 0x0004;
		private const int LVIF_STATE = 0x0008;
		private const int LVIF_INDENT = 0x0010;
		private const int LVIF_NORECOMPUTE = 0x0800;

		private const int LVIS_SELECTED = 2;

		private const int LVCF_FMT = 0x0001;
		private const int LVCF_WIDTH = 0x0002;
		private const int LVCF_TEXT = 0x0004;
		private const int LVCF_SUBITEM = 0x0008;
		private const int LVCF_IMAGE = 0x0010;
		private const int LVCF_ORDER = 0x0020;
		private const int LVCFMT_LEFT = 0x0000;
		private const int LVCFMT_RIGHT = 0x0001;
		private const int LVCFMT_CENTER = 0x0002;
		private const int LVCFMT_JUSTIFYMASK = 0x0003;

		private const int LVCFMT_IMAGE = 0x0800;
		private const int LVCFMT_BITMAP_ON_RIGHT = 0x1000;
		private const int LVCFMT_COL_HAS_IMAGES = 0x8000;

		private const int LVBKIF_SOURCE_NONE = 0x0;
		private const int LVBKIF_SOURCE_HBITMAP = 0x1;
		private const int LVBKIF_SOURCE_URL = 0x2;
		private const int LVBKIF_SOURCE_MASK = 0x3;
		private const int LVBKIF_STYLE_NORMAL = 0x0;
		private const int LVBKIF_STYLE_TILE = 0x10;
		private const int LVBKIF_STYLE_MASK = 0x10;
		private const int LVBKIF_FLAG_TILEOFFSET = 0x100;
		private const int LVBKIF_TYPE_WATERMARK = 0x10000000;
		private const int LVBKIF_FLAG_ALPHABLEND = 0x20000000;

		private const int LVSICF_NOINVALIDATEALL = 1;
		private const int LVSICF_NOSCROLL = 2;

		private const int HDM_FIRST = 0x1200;
		private const int HDM_HITTEST = HDM_FIRST + 6;
		private const int HDM_GETITEMRECT = HDM_FIRST + 7;
		private const int HDM_GETITEM = HDM_FIRST + 11;
		private const int HDM_SETITEM = HDM_FIRST + 12;

		private const int HDI_WIDTH = 0x0001;
		private const int HDI_TEXT = 0x0002;
		private const int HDI_FORMAT = 0x0004;
		private const int HDI_BITMAP = 0x0010;
		private const int HDI_IMAGE = 0x0020;

		private const int HDF_LEFT = 0x0000;
		private const int HDF_RIGHT = 0x0001;
		private const int HDF_CENTER = 0x0002;
		private const int HDF_JUSTIFYMASK = 0x0003;
		private const int HDF_RTLREADING = 0x0004;
		private const int HDF_STRING = 0x4000;
		private const int HDF_BITMAP = 0x2000;
		private const int HDF_BITMAP_ON_RIGHT = 0x1000;
		private const int HDF_IMAGE = 0x0800;
		private const int HDF_SORTUP = 0x0400;
		private const int HDF_SORTDOWN = 0x0200;

		private const int SB_HORZ = 0;
		private const int SB_VERT = 1;
		private const int SB_CTL = 2;
		private const int SB_BOTH = 3;

		private const int SIF_RANGE = 0x0001;
		private const int SIF_PAGE = 0x0002;
		private const int SIF_POS = 0x0004;
		private const int SIF_DISABLENOSCROLL = 0x0008;
		private const int SIF_TRACKPOS = 0x0010;
		private const int SIF_ALL = (SIF_RANGE | SIF_PAGE | SIF_POS | SIF_TRACKPOS);

		private const int ILD_NORMAL = 0x0;
		private const int ILD_TRANSPARENT = 0x1;
		private const int ILD_MASK = 0x10;
		private const int ILD_IMAGE = 0x20;
		private const int ILD_BLEND25 = 0x2;
		private const int ILD_BLEND50 = 0x4;

		const int SWP_NOSIZE = 1;
		const int SWP_NOMOVE = 2;
		const int SWP_NOZORDER = 4;
		const int SWP_NOREDRAW = 8;
		const int SWP_NOACTIVATE = 16;
		public const int SWP_FRAMECHANGED = 32;

		const int SWP_ZORDERONLY = SWP_NOSIZE | SWP_NOMOVE | SWP_NOREDRAW | SWP_NOACTIVATE;
		const int SWP_SIZEONLY = SWP_NOMOVE | SWP_NOREDRAW | SWP_NOZORDER | SWP_NOACTIVATE;
		const int SWP_UPDATE_FRAME = SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE | SWP_NOZORDER | SWP_FRAMECHANGED;

		#endregion

		#region Structures

		[StructLayout(LayoutKind.Sequential)]
		public struct HDITEM
		{
			public int mask;
			public int cxy;
			public IntPtr pszText;
			public IntPtr hbm;
			public int cchTextMax;
			public int fmt;
			public IntPtr lParam;
			public int iImage;
			public int iOrder;
			//if (_WIN32_IE >= 0x0500)
			public int type;
			public IntPtr pvFilter;
		}

		[StructLayout(LayoutKind.Sequential)]
		public class HDHITTESTINFO
		{
			public int pt_x;
			public int pt_y;
			public int flags;
			public int iItem;
		}

		[StructLayout(LayoutKind.Sequential)]
		public class HDLAYOUT
		{
			public IntPtr prc;
			public IntPtr pwpos;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct IMAGELISTDRAWPARAMS
		{
			public int cbSize;
			public IntPtr himl;
			public int i;
			public IntPtr hdcDst;
			public int x;
			public int y;
			public int cx;
			public int cy;
			public int xBitmap;
			public int yBitmap;
			public uint rgbBk;
			public uint rgbFg;
			public uint fStyle;
			public uint dwRop;
			public uint fState;
			public uint Frame;
			public uint crEffect;
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
		public struct LVBKIMAGE
		{
			public int ulFlags;
			public IntPtr hBmp;
			[MarshalAs(UnmanagedType.LPTStr)]
			public string pszImage;
			public int cchImageMax;
			public int xOffset;
			public int yOffset;
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
		public struct LVHITTESTINFO
		{
			public int pt_x;
			public int pt_y;
			public int flags;
			public int iItem;
			public int iSubItem;
			public int iGroup;
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
		public struct LVITEM
		{
			public int mask;
			public int iItem;
			public int iSubItem;
			public int state;
			public int stateMask;
			[MarshalAs(UnmanagedType.LPTStr)]
			public string pszText;
			public int cchTextMax;
			public int iImage;
			public IntPtr lParam;
			// These are available in Common Controls >= 0x0300
			public int iIndent;
			// These are available in Common Controls >= 0x056
			public int iGroupId;
			public int cColumns;
			public IntPtr puColumns;
		};

		[StructLayout(LayoutKind.Sequential)]
		public struct NMHDR
		{
			public IntPtr hwndFrom;
			public IntPtr idFrom;
			public int code;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct NMCUSTOMDRAW
		{
			public NativeMethods.NMHDR nmcd;
			public int dwDrawStage;
			public IntPtr hdc;
			public NativeMethods.RECT rc;
			public IntPtr dwItemSpec;
			public int uItemState;
			public IntPtr lItemlParam;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct NMHEADER
		{
			public NMHDR nhdr;
			public int iItem;
			public int iButton;
			public IntPtr pHDITEM;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct NMLISTVIEW
		{
			public NativeMethods.NMHDR hdr;
			public int iItem;
			public int iSubItem;
			public int uNewState;
			public int uOldState;
			public int uChanged;
			public IntPtr lParam;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct NMLVCUSTOMDRAW
		{
			public NativeMethods.NMCUSTOMDRAW nmcd;
			public int clrText;
			public int clrTextBk;
			public int iSubItem;
			public int dwItemType;
			public int clrFace;
			public int iIconEffect;
			public int iIconPhase;
			public int iPartId;
			public int iStateId;
			public NativeMethods.RECT rcText;
			public uint uAlign;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct NMLVGETINFOTIP
		{
			public NativeMethods.NMHDR hdr;
			public int dwFlags;
			public string pszText;
			public int cchTextMax;
			public int iItem;
			public int iSubItem;
			public IntPtr lParam;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct NMLVSCROLL
		{
			public NativeMethods.NMHDR hdr;
			public int dx;
			public int dy;
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
		public struct NMTTDISPINFO
		{
			public NativeMethods.NMHDR hdr;
			[MarshalAs(UnmanagedType.LPTStr)]
			public string lpszText;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
			public string szText;
			public IntPtr hinst;
			public int uFlags;
			public IntPtr lParam;
			//public int hbmp; This is documented but doesn't work
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct RECT
		{
			public int left;
			public int top;
			public int right;
			public int bottom;
		}

		[StructLayout(LayoutKind.Sequential)]
		public class SCROLLINFO
		{
			public int cbSize = Marshal.SizeOf(typeof(NativeMethods.SCROLLINFO));
			public int fMask;
			public int nMin;
			public int nMax;
			public int nPage;
			public int nPos;
			public int nTrackPos;
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
		public class TOOLINFO
		{
			public int cbSize = Marshal.SizeOf(typeof(NativeMethods.TOOLINFO));
			public int uFlags;
			public IntPtr hwnd;
			public IntPtr uId;
			public NativeMethods.RECT rect;
			public IntPtr hinst = IntPtr.Zero;
			public IntPtr lpszText;
			public IntPtr lParam = IntPtr.Zero;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct WINDOWPOS
		{
			public IntPtr hwnd;
			public IntPtr hwndInsertAfter;
			public int x;
			public int y;
			public int cx;
			public int cy;
			public int flags;
		}

		#endregion

		#region Entry points

		// Various flavours of SendMessage: plain vanilla, and passing references to various structures
		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);
		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, int lParam);
		[DllImport("user32.dll", EntryPoint = "SendMessage", CharSet = CharSet.Auto)]
		public static extern IntPtr SendMessageLVItem(IntPtr hWnd, int msg, int wParam, ref LVITEM lvi);
		[DllImport("user32.dll", EntryPoint = "SendMessage", CharSet = CharSet.Auto)]
		public static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, ref LVHITTESTINFO ht);
		[DllImport("user32.dll", EntryPoint = "SendMessage", CharSet = CharSet.Auto)]
		public static extern IntPtr SendMessageRECT(IntPtr hWnd, int msg, int wParam, ref RECT r);
		[DllImport("user32.dll", EntryPoint = "SendMessage", CharSet = CharSet.Auto)]
		private static extern IntPtr SendMessageHDItem(IntPtr hWnd, int msg, int wParam, ref HDITEM hdi);
		[DllImport("user32.dll", EntryPoint = "SendMessage", CharSet = CharSet.Auto)]
		public static extern IntPtr SendMessageHDHITTESTINFO(IntPtr hWnd, int Msg, IntPtr wParam, [In, Out] HDHITTESTINFO lParam);
		[DllImport("user32.dll", EntryPoint = "SendMessage", CharSet = CharSet.Auto)]
		public static extern IntPtr SendMessageTOOLINFO(IntPtr hWnd, int Msg, int wParam, NativeMethods.TOOLINFO lParam);
		[DllImport("user32.dll", EntryPoint = "SendMessage", CharSet = CharSet.Auto)]
		public static extern IntPtr SendMessageLVBKIMAGE(IntPtr hWnd, int Msg, int wParam, ref NativeMethods.LVBKIMAGE lParam);
		[DllImport("user32.dll", EntryPoint = "SendMessage", CharSet = CharSet.Auto)]
		public static extern IntPtr SendMessageString(IntPtr hWnd, int Msg, int wParam, string lParam);

		[DllImport("gdi32.dll")]
		public static extern bool DeleteObject(IntPtr objectHandle);

		[DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
		public static extern bool GetClientRect(IntPtr hWnd, ref Rectangle r);

		[DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
		public static extern bool GetScrollInfo(IntPtr hWnd, int fnBar, SCROLLINFO scrollInfo);

		[DllImport("comctl32.dll", CharSet = CharSet.Auto)]
		private static extern bool ImageList_DrawIndirect(ref IMAGELISTDRAWPARAMS parms);

		[DllImport("user32.dll")]
		public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

		[DllImport("user32.dll", EntryPoint = "GetWindowLong", CharSet = CharSet.Auto)]
		public static extern IntPtr GetWindowLong32(IntPtr hWnd, int nIndex);

		[DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", CharSet = CharSet.Auto)]
		public static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

		[DllImport("user32.dll", EntryPoint = "SetWindowLong", CharSet = CharSet.Auto)]
		public static extern IntPtr SetWindowLongPtr32(IntPtr hWnd, int nIndex, int dwNewLong);

		[DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", CharSet = CharSet.Auto)]
		public static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, int dwNewLong);

		#endregion

		public static bool DrawImageList(Graphics g, ImageList il, int index, int x, int y, bool isSelected, bool isDisabled)
		{
			ImageListDrawItemConstants flags = (isSelected ? ImageListDrawItemConstants.ILD_SELECTED : ImageListDrawItemConstants.ILD_NORMAL) | ImageListDrawItemConstants.ILD_TRANSPARENT;
			ImageListDrawStateConstants state = isDisabled ? ImageListDrawStateConstants.ILS_SATURATE : ImageListDrawStateConstants.ILS_NORMAL;
			try
			{
				IntPtr hdc = g.GetHdc();
				return DrawImage(il, hdc, index, x, y, flags, 0, 0, state);
			}
			finally
			{
				g.ReleaseHdc();
			}
		}

		/// <summary>
		/// Flags controlling how the Image List item is 
		/// drawn
		/// </summary>
		[Flags]
		public enum ImageListDrawItemConstants
		{
			/// <summary>
			/// Draw item normally.
			/// </summary>
			ILD_NORMAL = 0x0,
			/// <summary>
			/// Draw item transparently.
			/// </summary>
			ILD_TRANSPARENT = 0x1,
			/// <summary>
			/// Draw item blended with 25% of the specified foreground colour
			/// or the Highlight colour if no foreground colour specified.
			/// </summary>
			ILD_BLEND25 = 0x2,
			/// <summary>
			/// Draw item blended with 50% of the specified foreground colour
			/// or the Highlight colour if no foreground colour specified.
			/// </summary>
			ILD_SELECTED = 0x4,
			/// <summary>
			/// Draw the icon's mask
			/// </summary>
			ILD_MASK = 0x10,
			/// <summary>
			/// Draw the icon image without using the mask
			/// </summary>
			ILD_IMAGE = 0x20,
			/// <summary>
			/// Draw the icon using the ROP specified.
			/// </summary>
			ILD_ROP = 0x40,
			/// <summary>
			/// Preserves the alpha channel in dest. XP only.
			/// </summary>
			ILD_PRESERVEALPHA = 0x1000,
			/// <summary>
			/// Scale the image to cx, cy instead of clipping it. XP only.
			/// </summary>
			ILD_SCALE = 0x2000,
			/// <summary>
			/// Scale the image to the current DPI of the display. XP only.
			/// </summary>
			ILD_DPISCALE = 0x4000
		}

		/// <summary>
		/// Enumeration containing XP ImageList Draw State options
		/// </summary>
		[Flags]
		public enum ImageListDrawStateConstants
		{
			/// <summary>
			/// The image state is not modified. 
			/// </summary>
			ILS_NORMAL = (0x00000000),
			/// <summary>
			/// Adds a glow effect to the icon, which causes the icon to appear to glow 
			/// with a given color around the edges. (Note: does not appear to be implemented)
			/// </summary>
			ILS_GLOW = (0x00000001), //The color for the glow effect is passed to the IImageList::Draw method in the crEffect member of IMAGELISTDRAWPARAMS. 
									 /// <summary>
									 /// Adds a drop shadow effect to the icon. (Note: does not appear to be implemented)
									 /// </summary>
			ILS_SHADOW = (0x00000002), //The color for the drop shadow effect is passed to the IImageList::Draw method in the crEffect member of IMAGELISTDRAWPARAMS. 
									   /// <summary>
									   /// Saturates the icon by increasing each color component 
									   /// of the RGB triplet for each pixel in the icon. (Note: only ever appears to result in a completely unsaturated icon)
									   /// </summary>
			ILS_SATURATE = (0x00000004), // The amount to increase is indicated by the frame member in the IMAGELISTDRAWPARAMS method. 
										 /// <summary>
										 /// Alpha blends the icon. Alpha blending controls the transparency 
										 /// level of an icon, according to the value of its alpha channel. 
										 /// (Note: does not appear to be implemented).
										 /// </summary>
			ILS_ALPHA = (0x00000008) //The value of the alpha channel is indicated by the frame member in the IMAGELISTDRAWPARAMS method. The alpha channel can be from 0 to 255, with 0 being completely transparent, and 255 being completely opaque. 
		}

		private const uint CLR_DEFAULT = 0xFF000000;

		/// <summary>
		/// Draws an image using the specified flags and state on XP systems.
		/// </summary>
		/// <param name="il">The image list from which an item will be drawn</param>
		/// <param name="hdc">Device context to draw to</param>
		/// <param name="index">Index of image to draw</param>
		/// <param name="x">X Position to draw at</param>
		/// <param name="y">Y Position to draw at</param>
		/// <param name="flags">Drawing flags</param>
		/// <param name="cx">Width to draw</param>
		/// <param name="cy">Height to draw</param>
		/// <param name="stateFlags">State flags</param>
		public static bool DrawImage(ImageList il, IntPtr hdc, int index, int x, int y, ImageListDrawItemConstants flags, int cx, int cy, ImageListDrawStateConstants stateFlags)
		{
			IMAGELISTDRAWPARAMS pimldp = new IMAGELISTDRAWPARAMS();
			pimldp.hdcDst = hdc;
			pimldp.cbSize = Marshal.SizeOf(pimldp.GetType());
			pimldp.i = index;
			pimldp.x = x;
			pimldp.y = y;
			pimldp.cx = cx;
			pimldp.cy = cy;
			pimldp.rgbFg = CLR_DEFAULT;
			pimldp.fStyle = (uint)flags;
			pimldp.fState = (uint)stateFlags;
			pimldp.himl = il.Handle;
			return ImageList_DrawIndirect(ref pimldp);
		}

		/// <summary>
		/// Make sure the ListView has the extended style that says to display subitem images.
		/// </summary>
		/// <remarks>This method must be called after any .NET call that update the extended styles
		/// since they seem to erase this setting.</remarks>
		/// <param name="list">The listview to send a m to</param>
		public static void ForceSubItemImagesExStyle(ListView list)
		{
			SendMessage(list.Handle, LVM_SETEXTENDEDLISTVIEWSTYLE, LVS_EX_SUBITEMIMAGES, LVS_EX_SUBITEMIMAGES);
		}

		/// <summary>
		/// Change the virtual list size of the given ListView (which must be in virtual mode)
		/// </summary>
		/// <remarks>This will not change the scroll position</remarks>
		/// <param name="list">The listview to send a message to</param>
		/// <param name="count">How many rows should the list have?</param>
		public static void SetItemCount(ListView list, int count)
		{
			SendMessage(list.Handle, LVM_SETITEMCOUNT, count, LVSICF_NOSCROLL);
		}

		/// <summary>
		/// Make sure the ListView has the extended style that says to display subitem images.
		/// </summary>
		/// <remarks>This method must be called after any .NET call that update the extended styles
		/// since they seem to erase this setting.</remarks>
		/// <param name="list">The listview to send a m to</param>
		/// <param name="style"></param>
		/// <param name="styleMask"></param>
		public static void SetExtendedStyle(ListView list, int style, int styleMask)
		{
			SendMessage(list.Handle, LVM_SETEXTENDEDLISTVIEWSTYLE, styleMask, style);
		}

		/// <summary>
		/// Calculates the number of items that can fit vertically in the visible area of a list-view (which
		/// must be in details or list view.
		/// </summary>
		/// <param name="list">The listView</param>
		/// <returns>Number of visible items per page</returns>
		public static int GetCountPerPage(ListView list)
		{
			return (int)SendMessage(list.Handle, LVM_GETCOUNTPERPAGE, 0, 0);
		}
		/// <summary>
		/// For the given item and subitem, make it display the given image
		/// </summary>
		/// <param name="list">The listview to send a m to</param>
		/// <param name="itemIndex">row number (0 based)</param>
		/// <param name="subItemIndex">subitem (0 is the item itself)</param>
		/// <param name="imageIndex">index into the image list</param>
		public static void SetSubItemImage(ListView list, int itemIndex, int subItemIndex, int imageIndex)
		{
			LVITEM lvItem = new LVITEM();
			lvItem.mask = LVIF_IMAGE;
			lvItem.iItem = itemIndex;
			lvItem.iSubItem = subItemIndex;
			lvItem.iImage = imageIndex;
			SendMessageLVItem(list.Handle, LVM_SETITEM, 0, ref lvItem);
		}

		/// <summary>
		/// Setup the given column of the listview to show the given image to the right of the text.
		/// If the image index is -1, any previous image is cleared
		/// </summary>
		/// <param name="list">The listview to send a m to</param>
		/// <param name="columnIndex">Index of the column to modifiy</param>
		/// <param name="order"></param>
		/// <param name="imageIndex">Index into the small image list</param>
		public static void SetColumnImage(ListView list, int columnIndex, SortOrder order, int imageIndex)
		{
			IntPtr hdrCntl = NativeMethods.GetHeaderControl(list);
			if (hdrCntl.ToInt32() == 0)
				return;

			HDITEM item = new HDITEM();
			item.mask = HDI_FORMAT;
			IntPtr result = SendMessageHDItem(hdrCntl, HDM_GETITEM, columnIndex, ref item);

			item.fmt &= ~(HDF_SORTUP | HDF_SORTDOWN | HDF_IMAGE | HDF_BITMAP_ON_RIGHT);

			if (NativeMethods.HasBuiltinSortIndicators())
			{
				if (order == SortOrder.Ascending)
					item.fmt |= HDF_SORTUP;
				if (order == SortOrder.Descending)
					item.fmt |= HDF_SORTDOWN;
			}
			else
			{
				item.mask |= HDI_IMAGE;
				item.fmt |= (HDF_IMAGE | HDF_BITMAP_ON_RIGHT);
				item.iImage = imageIndex;
			}

			result = SendMessageHDItem(hdrCntl, HDM_SETITEM, columnIndex, ref item);
		}

		/// <summary>
		/// Does this version of the operating system have builtin sort indicators?
		/// </summary>
		/// <returns>Are there builtin sort indicators</returns>
		/// <remarks>XP and later have these</remarks>
		public static bool HasBuiltinSortIndicators()
		{
			return OSFeature.Feature.GetVersionPresent(OSFeature.Themes) != null;
		}

		/// <summary>
		/// Deselect a single row
		/// </summary>
		/// <param name="list"></param>
		/// <param name="index"></param>
		public static void DeselectOneItem(ListView list, int index)
		{
			NativeMethods.SetItemState(list, index, LVIS_SELECTED, 0);
		}

		/// <summary>
		/// Set the item state on the given item
		/// </summary>
		/// <param name="list">The listview whose item's state is to be changed</param>
		/// <param name="itemIndex">The index of the item to be changed</param>
		/// <param name="mask">Which bits of the value are to be set?</param>
		/// <param name="value">The value to be set</param>
		public static void SetItemState(ListView list, int itemIndex, int mask, int value)
		{
			LVITEM lvItem = new LVITEM();
			lvItem.stateMask = mask;
			lvItem.state = value;
			SendMessageLVItem(list.Handle, LVM_SETITEMSTATE, itemIndex, ref lvItem);
		}

		/// <summary>
		/// Scroll the given listview by the given deltas
		/// </summary>
		/// <param name="list"></param>
		/// <param name="dx"></param>
		/// <param name="dy"></param>
		/// <returns>true if the scroll succeeded</returns>
		public static bool Scroll(ListView list, int dx, int dy)
		{
			return SendMessage(list.Handle, LVM_SCROLL, dx, dy) != IntPtr.Zero;
		}

		/// <summary>
		/// Return the handle to the header control on the given list
		/// </summary>
		/// <param name="list">The listview whose header control is to be returned</param>
		/// <returns>The handle to the header control</returns>
		public static IntPtr GetHeaderControl(ListView list)
		{
			return SendMessage(list.Handle, LVM_GETHEADER, 0, 0);
		}

		/// <summary>
		/// Return the edges of the given column.
		/// </summary>
		/// <param name="lv"></param>
		/// <param name="columnIndex"></param>
		/// <returns>A Point holding the left and right co-ords of the column.
		/// -1 means that the sides could not be retrieved.</returns>
		public static Point GetScrolledColumnSides(ListView lv, int columnIndex)
		{
			IntPtr hdr = NativeMethods.GetHeaderControl(lv);
			if (hdr == IntPtr.Zero)
				return new Point(-1, -1);

			RECT r = new RECT();
			IntPtr result = NativeMethods.SendMessageRECT(hdr, HDM_GETITEMRECT, columnIndex, ref r);
			int scrollH = NativeMethods.GetScrollPosition(lv, true);
			return new Point(r.left - scrollH, r.right - scrollH);
		}

		/// <summary>
		/// Return the index of the column of the header that is under the given point.
		/// Return -1 if no column is under the pt
		/// </summary>
		/// <param name="handle">The list we are interested in</param>
		/// <param name="pt">The client co-ords</param>
		/// <returns>The index of the column under the point, or -1 if no column header is under that point</returns>
		public static int GetColumnUnderPoint(IntPtr handle, Point pt)
		{
			const int HHT_ONHEADER = 2;
			const int HHT_ONDIVIDER = 4;
			return NativeMethods.HeaderControlHitTest(handle, pt, HHT_ONHEADER | HHT_ONDIVIDER);
		}

		private static int HeaderControlHitTest(IntPtr handle, Point pt, int flag)
		{
			HDHITTESTINFO testInfo = new HDHITTESTINFO();
			testInfo.pt_x = pt.X;
			testInfo.pt_y = pt.Y;
			IntPtr result = NativeMethods.SendMessageHDHITTESTINFO(handle, HDM_HITTEST, IntPtr.Zero, testInfo);
			if ((testInfo.flags & flag) != 0)
				return testInfo.iItem;
			else
				return -1;
		}

		/// <summary>
		/// Return the index of the divider under the given point. Return -1 if no divider is under the pt
		/// </summary>
		/// <param name="handle">The list we are interested in</param>
		/// <param name="pt">The client co-ords</param>
		/// <returns>The index of the divider under the point, or -1 if no divider is under that point</returns>
		public static int GetDividerUnderPoint(IntPtr handle, Point pt)
		{
			const int HHT_ONDIVIDER = 4;
			return NativeMethods.HeaderControlHitTest(handle, pt, HHT_ONDIVIDER);
		}

		/// <summary>
		/// Get the scroll position of the given scroll bar
		/// </summary>
		/// <param name="lv"></param>
		/// <param name="horizontalBar"></param>
		/// <returns></returns>
		public static int GetScrollPosition(ListView lv, bool horizontalBar)
		{
			int fnBar = (horizontalBar ? SB_HORZ : SB_VERT);

			SCROLLINFO scrollInfo = new SCROLLINFO();
			scrollInfo.fMask = SIF_POS;
			if (GetScrollInfo(lv.Handle, fnBar, scrollInfo))
				return scrollInfo.nPos;
			else
				return -1;
		}

		/// <summary>
		/// Make the given control/window a topmost window
		/// </summary>
		/// <param name="toBeMoved"></param>
		/// <returns></returns>
		public static bool MakeTopMost(IWin32Window toBeMoved)
		{
			IntPtr HWND_TOPMOST = (IntPtr)(-1);
			return NativeMethods.SetWindowPos(toBeMoved.Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_ZORDERONLY);
		}

		static public int GetTopIndex(ListView lv)
		{
			return (int)SendMessage(lv.Handle, LVM_GETTOPINDEX, 0, 0);
		}

		static public IntPtr GetTooltipControl(ListView lv)
		{
			return SendMessage(lv.Handle, LVM_GETTOOLTIPS, 0, 0);
		}

		public static int GetWindowLong(IntPtr hWnd, int nIndex)
		{
			if (IntPtr.Size == 4)
				return (int)GetWindowLong32(hWnd, nIndex);
			else
				return (int)(long)GetWindowLongPtr64(hWnd, nIndex);
		}

		public static int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong)
		{
			if (IntPtr.Size == 4)
				return (int)SetWindowLongPtr32(hWnd, nIndex, dwNewLong);
			else
				return (int)(long)SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
		}

		[DllImport("gdi32.dll", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
		public static extern IntPtr SelectObject(IntPtr hdc, IntPtr obj);

		[DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
		public static extern bool InvalidateRect(IntPtr hWnd, int ignored, bool erase);

		public static int HitTest(ObjectListView olv, ref LVHITTESTINFO hittest)
		{
			return (int)NativeMethods.SendMessage(olv.Handle, olv.View == View.Details ? LVM_SUBITEMHITTEST : LVM_HITTEST, -1, ref hittest);
		}
	}
}