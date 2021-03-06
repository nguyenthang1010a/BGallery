using System;
using System.Globalization;
using System.Web;
using GalleryServerPro.Business;
using GalleryServerPro.Business.Interfaces;
using GalleryServerPro.Events.CustomExceptions;

namespace GalleryServerPro.Web
{
	/// <summary>
	/// Provides functionality for generating the HTML that can be sent to a client browser to render a
	/// particular media object. Objects implementing this interface use the HTML templates in the configuration
	/// file. Replaceable parameters in the template are indicated by the open and close brackets, such as 
	/// {Width}. These parameters are replaced with the relevant values.
	/// TODO: Add caching functionality to speed up the ability to generate HTML.
	/// </summary>
	public class MediaObjectHtmlBuilder : IMediaObjectHtmlBuilder
	{
		#region Private Fields

		private readonly int _galleryId;
		private readonly IGalleryObject _galleryObject;
		private int _mediaObjectId;
		private int _albumId;
		private IMimeType _mimeType;
		private string _mediaObjectPhysicalPath;
		private int _width;
		private int _height;
		private string _title;
		private bool? _autoStartMediaObject;
		private readonly Array _browsers;
		private DisplayObjectType _displayType;
		private bool _isPrivate;
		private readonly string _externalHtmlSource;
		private string _uniquePrefixId;

		#endregion

		#region Constructors

		public MediaObjectHtmlBuilder(IGalleryObject mediaObject, IDisplayObject displayObject, Array browsers)
		{
			if (mediaObject == null)
				throw new ArgumentNullException("mediaObject");

			if (displayObject == null)
				throw new ArgumentNullException("displayObject");

			if ((browsers == null) || (browsers.Length < 1))
				throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, Resources.GalleryServerPro.MediaObjectHtmlBuilder_Ctor_InvalidBrowsers_Msg));

			this._galleryObject = mediaObject;
			this._mediaObjectId = mediaObject.Id;
			this._albumId = mediaObject.Parent.Id;
			this._mimeType = displayObject.MimeType;
			this._mediaObjectPhysicalPath = displayObject.FileNamePhysicalPath;
			this._width = displayObject.Width;
			this._height = displayObject.Height;
			this._title = mediaObject.Title;
			this._browsers = browsers;
			this._displayType = displayObject.DisplayType;
			this._isPrivate = mediaObject.IsPrivate;
			this._galleryId = mediaObject.GalleryId;
			this._externalHtmlSource = displayObject.ExternalHtmlSource;
		}

		#endregion

		#region Public Properties

		/// <summary>
		/// Gets the gallery ID.
		/// </summary>
		/// <value>The gallery ID.</value>
		public int GalleryId
		{
			get { return _galleryId; }
		}

		/// <summary>
		/// Gets or sets the unique identifier for this media object.
		/// </summary>
		/// <value>The unique identifier for this media object.</value>
		public int MediaObjectId
		{
			get
			{
				return this._mediaObjectId;
			}
			set
			{
				this._mediaObjectId = value;
			}
		}

		/// <summary>
		/// Gets or sets the unique identifier for album containing the media object.
		/// </summary>
		/// <value>The unique identifier for album containing the media object.</value>
		public int AlbumId
		{
			get
			{
				return this._albumId;
			}
			set
			{
				this._albumId = value;
			}
		}

		/// <summary>
		/// Gets or sets the MIME type of this media object.
		/// </summary>
		/// <value>The MIME type of this media object.</value>
		public IMimeType MimeType
		{
			get
			{
				return this._mimeType;
			}
			set
			{
				this._mimeType = value;
			}
		}

		/// <summary>
		/// Gets or sets the physical path to this media object, including the object's name. Example:
		/// C:\Inetpub\wwwroot\galleryserverpro\mediaobjects\Summer 2005\sunsets\desert sunsets\sonorandesert.jpg
		/// </summary>
		/// <value>
		/// The physical path to this media object, including the object's name.
		/// </value>
		public string MediaObjectPhysicalPath
		{
			get
			{
				return this._mediaObjectPhysicalPath;
			}
			set
			{
				this._mediaObjectPhysicalPath = value;
			}
		}

		/// <summary>
		/// Gets or sets the width of this object, in pixels.
		/// </summary>
		/// <value>The width of this object, in pixels.</value>
		public int Width
		{
			get
			{
				return this._width;
			}
			set
			{
				this._width = value;
			}
		}

		/// <summary>
		/// Gets or sets the height of this object, in pixels.
		/// </summary>
		/// <value>The height of this object, in pixels.</value>
		public int Height
		{
			get
			{
				return this._height;
			}
			set
			{
				this._height = value;
			}
		}

		/// <summary>
		/// Gets or sets the title for this gallery object.
		/// </summary>
		/// <value>The title for this gallery object.</value>
		public string Title
		{
			get
			{
				return this._title;
			}
			set
			{
				this._title = value;
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether to automatically begin playing the media object as soon as
		/// possible in the client browser. This setting is applicable only to objects that can be played, such
		/// as audio and video files.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if Gallery Server Pro is to automatically begin playing the media object as soon as
		/// possible in the client browser; otherwise, <c>false</c>.
		/// </value>
		public bool AutoStartMediaObject
		{
			get
			{
				if (!this._autoStartMediaObject.HasValue)
				{
					this._autoStartMediaObject = Factory.LoadGallerySetting(GalleryId).AutoStartMediaObject;
				}

				return this._autoStartMediaObject.Value;
			}
			set
			{
				this._autoStartMediaObject = value;
			}
		}

		/// <summary>
		/// An <see cref="System.Array"/> of browser ids for the current browser. This is a list of strings,
		/// ordered from most general to most specific, that represent the various categories of browsers the current
		/// browser belongs to. This is typically populated by calling ToArray() on the Request.Browser.Browsers property.
		/// </summary>
		/// <value>
		/// The <see cref="System.Array"/> of browser ids for the current browser.
		/// </value>
		public Array Browsers
		{
			get
			{
				return this._browsers;
			}
			//set
			//{
			//  this._browsers = value;
			//}
		}

		/// <summary>
		/// Gets or sets the type of the display object.
		/// </summary>
		/// <value>The display type.</value>
		public DisplayObjectType DisplayType
		{
			get
			{
				return this._displayType;
			}
			set
			{
				this._displayType = value;
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether the media object is marked as private. Private albums and media
		/// objects are hidden from anonymous (unauthenticated) users.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is private; otherwise, <c>false</c>.
		/// </value>
		public bool IsPrivate
		{
			get
			{
				return this._isPrivate;
			}
			set
			{
				this._isPrivate = value;
			}
		}

		/// <summary>
		/// Generates a string about twelve characters long that can be used as a unique identifier, such as the ID of
		/// an HTML element. The value is generated the first time the property is accessed, and subsequent reads return
		/// the same value. There is currently no support for generating more than one ID during the lifetime of an instance.
		/// Ex: "gsp_1c135176ed"
		/// </summary>
		/// <value>Generates a string about twelve characters long that can be used as a unique identifier.</value>
		public string UniquePrefixId
		{
			get
			{
				if (String.IsNullOrEmpty(_uniquePrefixId))
				{
					_uniquePrefixId = String.Concat("gsp_", Guid.NewGuid().ToString().Replace("-", String.Empty).Substring(0, 10));
				}

				return _uniquePrefixId;
			}
		}

		#endregion

		#region Public Methods

		/// <summary>
		/// Generate the HTML that can be sent to a browser to render the media object.
		/// Guaranteed to not return null.
		/// </summary>
		/// <returns>
		/// Returns a string of valid HTML that can be sent to a browser.
		/// </returns>
		public string GenerateHtml()
		{
			if (this._galleryObject is Album)
				return String.Empty;

			var htmlOutput = GetHtmlTemplate();

			htmlOutput = htmlOutput.Replace("{HostUrl}", Utils.GetHostUrl());
			htmlOutput = htmlOutput.Replace("{MediaObjectUrl}", GetMediaObjectUrl());
			htmlOutput = htmlOutput.Replace("{MimeType}", this.MimeType.BrowserMimeType);
			htmlOutput = htmlOutput.Replace("{Width}", this.Width.ToString(CultureInfo.InvariantCulture));
			htmlOutput = htmlOutput.Replace("{Height}", this.Height.ToString(CultureInfo.InvariantCulture));
			htmlOutput = htmlOutput.Replace("{Title}", this.Title);
			htmlOutput = htmlOutput.Replace("{TitleNoHtml}", Utils.RemoveHtmlTags(this.Title, true));
			htmlOutput = htmlOutput.Replace("{UniqueId}", UniquePrefixId);

			bool autoStartMediaObject = Factory.LoadGallerySetting(GalleryId).AutoStartMediaObject;

			// Replace {AutoStartMediaObjectText} with "true" or "false".
			htmlOutput = htmlOutput.Replace("{AutoStartMediaObjectText}", autoStartMediaObject.ToString().ToLowerInvariant());

			// Replace {AutoStartMediaObjectInt} with "1" or "0".
			htmlOutput = htmlOutput.Replace("{AutoStartMediaObjectInt}", autoStartMediaObject ? "1" : "0");

			// Replace {AutoPlay} with "autoplay" or "".
			htmlOutput = htmlOutput.Replace("{AutoPlay}", autoStartMediaObject ? "autoplay" : String.Empty);

			if (htmlOutput.Contains("{MediaObjectAbsoluteUrlNoHandler}"))
				htmlOutput = ReplaceMediaObjectAbsoluteUrlNoHandlerParameter(htmlOutput);

			if (htmlOutput.Contains("{MediaObjectRelativeUrlNoHandler}"))
				htmlOutput = ReplaceMediaObjectRelativeUrlNoHandlerParameter(htmlOutput);

			if (htmlOutput.Contains("{GalleryPath}"))
				htmlOutput = htmlOutput.Replace("{GalleryPath}", Utils.GalleryRoot);

			return htmlOutput;
		}

		/// <summary>
		/// Gets the HTML template to use for rendering this media object. Guaranteed to not
		/// return null.
		/// </summary>
		/// <returns>Returns a string.</returns>
		private string GetHtmlTemplate()
		{
			if (this._displayType == DisplayObjectType.External)
			{
				return this._externalHtmlSource;
			}

			var isInQueue = (this._displayType == DisplayObjectType.Optimized &&
				(_galleryObject.GalleryObjectType == GalleryObjectType.Audio || _galleryObject.GalleryObjectType == GalleryObjectType.Video) &&
				MediaConversionQueue.Instance.IsWaitingInQueueOrProcessing(MediaObjectId));

			if (isInQueue)
			{
				return String.Format(CultureInfo.CurrentCulture, "<p class='gsp_item_process_msg'>{0}</p>", Resources.GalleryServerPro.UC_MediaObjectView_Media_Object_Being_Processed_Text);
			}

			bool isBrowserIncompatibleImage = (this.MimeType.MajorType.Equals("IMAGE", StringComparison.OrdinalIgnoreCase)) && (IsImageBrowserIncompatible());

			string htmlOutput = GetHtmlOutputFromConfig();

			if (isBrowserIncompatibleImage || String.IsNullOrEmpty(htmlOutput))
			{
				// Either (1) no applicable template exists or (2) this is an image that can't be natively displayed in a 
				// browser (e.g. PSD, ICO, etc). Determine the appropriate message and return that as the HTML template.
				var url = Utils.AddQueryStringParameter(GetMediaObjectUrl(), "sa=1"); // Get URL with the "send as attachment" query string parm
				var msg = String.Format(CultureInfo.InvariantCulture, Resources.GalleryServerPro.UC_MediaObjectView_Browser_Cannot_Display_Media_Object_Text, url);
				return String.Format(CultureInfo.InvariantCulture, "<p class='gsp_msgfriendly'>{0}</p>", msg);
			}

			return htmlOutput;
		}

		/// <summary>
		/// Generate the ECMA script (javascript) that can be sent to a browser to assist with rendering the media object. 
		/// If the configuration file does not specify a scriptOutput template for this MIME type, an empty string is returned.
		/// </summary>
		/// <returns>Returns the ECMA script (javascript) that can be sent to a browser to assist with rendering the media object.</returns>
		public string GenerateScript()
		{
			if (this._galleryObject is Album)
				return String.Empty;

			if ((this.MimeType.MajorType.Equals("IMAGE", StringComparison.OrdinalIgnoreCase)) && (IsImageBrowserIncompatible()))
				return String.Empty; // Browsers can't display this image.

			string scriptOutput = GetScriptOutputFromConfig();
			if (String.IsNullOrEmpty(scriptOutput))
				return String.Empty; // No ECMA script rendering info in config file.

			scriptOutput = scriptOutput.Replace("{HostUrl}", Utils.GetHostUrl());
			scriptOutput = scriptOutput.Replace("{MediaObjectUrl}", GetMediaObjectUrl());
			scriptOutput = scriptOutput.Replace("{MimeType}", this.MimeType.BrowserMimeType);
			scriptOutput = scriptOutput.Replace("{Width}", this.Width.ToString(CultureInfo.InvariantCulture));
			scriptOutput = scriptOutput.Replace("{Height}", this.Height.ToString(CultureInfo.InvariantCulture));
			scriptOutput = scriptOutput.Replace("{Title}", this.Title);
			scriptOutput = scriptOutput.Replace("{TitleNoHtml}", Utils.RemoveHtmlTags(this.Title, true));
			scriptOutput = scriptOutput.Replace("{UniqueId}", UniquePrefixId);

			bool autoStartMediaObject = Factory.LoadGallerySetting(GalleryId).AutoStartMediaObject;

			// Replace {AutoStartMediaObjectText} with "true" or "false".
			scriptOutput = scriptOutput.Replace("{AutoStartMediaObjectText}", autoStartMediaObject.ToString().ToLowerInvariant());

			// Replace {AutoStartMediaObjectInt} with "1" or "0".
			scriptOutput = scriptOutput.Replace("{AutoStartMediaObjectInt}", autoStartMediaObject ? "1" : "0");

			if (scriptOutput.Contains("{MediaObjectAbsoluteUrlNoHandler}"))
				scriptOutput = ReplaceMediaObjectAbsoluteUrlNoHandlerParameter(scriptOutput);

			if (scriptOutput.Contains("{MediaObjectRelativeUrlNoHandler}"))
				scriptOutput = ReplaceMediaObjectRelativeUrlNoHandlerParameter(scriptOutput);

			if (scriptOutput.Contains("{GalleryPath}"))
				scriptOutput = scriptOutput.Replace("{GalleryPath}", Utils.GalleryRoot);

			return scriptOutput;
		}

		/// <summary>
		/// Generate the URL to the media object. For example, for images this url can be assigned to the src attribute of an img tag.
		/// (ex: /galleryserverpro/handler/getmedia.ashx?moid=34&amp;dt=1&amp;g=1)
		/// The query string parameter will be encrypted if that option is enabled.
		/// </summary>
		/// <returns>Gets the URL to the media object.</returns>
		public string GenerateUrl()
		{
			if (this._galleryObject is Album)
				return GetAlbumUrl();
			else
				return GetMediaObjectUrl();
		}

		/// <summary>
		/// Replace the replacement parameter {MediaObjectAbsoluteUrlNoHandler} with an URL that points directly to the media object
		/// (ex: /gallery/videos/birthdayvideo.wmv). A BusinessException is thrown if the media objects directory is not
		/// within the web application directory. Note that using this parameter completely bypasses the HTTP handler that 
		/// normally streams the media object. The consequence is that there is no security check when the media object request
		/// is made and no watermarks are applied, even if watermark functionality is enabled. This option should only be
		/// used when it is not important to restrict access to the media objects.
		/// </summary>
		/// <param name="htmlOutput">A string representing the HTML that will be sent to the browser for the current media object.
		/// It is based on the template stored in the media template table.</param>
		/// <returns>Returns the htmlOutput parameter with the {MediaObjectAbsoluteUrlNoHandler} string replaced by the URL to the media
		/// object.</returns>
		/// <exception cref="GalleryServerPro.Events.CustomExceptions.BusinessException">Thrown when the media objects 
		/// directory is not within the web application directory.</exception>
		private string ReplaceMediaObjectAbsoluteUrlNoHandlerParameter(string htmlOutput)
		{
			string appPath = AppSetting.Instance.PhysicalApplicationPath;

			if (!this.MediaObjectPhysicalPath.StartsWith(appPath, StringComparison.OrdinalIgnoreCase))
				throw new BusinessException(String.Format(CultureInfo.CurrentCulture, "Expected this.MediaObjectPhysicalPath (\"{0}\") to start with AppSetting.Instance.PhysicalApplicationPath (\"{1}\"), but it did not. If the media objects are not stored within the Gallery Server Pro web application, you cannot use the MediaObjectAbsoluteUrlNoHandler replacement parameter. Instead, use MediaObjectRelativeUrlNoHandler and specify the virtual path to your media object directory in the HTML template. For example: HtmlTemplate=\"<a href=\"{{HostUrl}}/media{{MediaObjectRelativeUrlNoHandler}}\">Click to open</a>\"", this.MediaObjectPhysicalPath, appPath));

			string relativePath = this.MediaObjectPhysicalPath.Remove(0, appPath.Length).Trim(new char[] { System.IO.Path.DirectorySeparatorChar });

			relativePath = Utils.UrlEncode(relativePath, '\\');

			string directUrl = String.Concat(Utils.UrlEncode(Utils.AppRoot, '/'), "/", relativePath.Replace("\\", "/"));

			return htmlOutput.Replace("{MediaObjectAbsoluteUrlNoHandler}", directUrl);
		}

		/// <summary>
		/// Replace the replacement parameter {MediaObjectRelativeUrlNoHandler} with an URL that is relative to the media objects
		/// directory and which points directly to the media object (ex: /videos/birthdayvideo.wmv). Note 
		/// that using this parameter completely bypasses the HTTP handler that normally streams the media object. The consequence 
		/// is that there is no security check when the media object request is made and no watermarks are applied, even if 
		/// watermark functionality is enabled. This option should only be used when it is not important to restrict access to 
		/// the media objects.
		/// </summary>
		/// <param name="htmlOutput">A string representing the HTML that will be sent to the browser for the current media object.
		/// It is based on the template stored in the media template table.</param>
		/// <returns>Returns the htmlOutput parameter with the {MediaObjectRelativeUrlNoHandler} string replaced by the URL to the media
		/// object.</returns>
		/// <exception cref="GalleryServerPro.Events.CustomExceptions.BusinessException">Thrown when the current media object's
		/// physical path does not start with the same text as AppSetting.Instance.MediaObjectPhysicalPath.</exception>
		/// <remarks>Typically this parameter is used instead of {MediaObjectAbsoluteUrlNoHandler} when the media objects directory 
		/// is outside of the web application. If the user wants to allow direct access to the media objects using this parameter, 
		/// she must first configure the media objects directory as a virtual directory in IIS. Then the path to this virtual directory 
		/// must be manually entered into one or more HTML templates, so that it prepends the relative url returned from this method.</remarks>
		/// <example>If the media objects directory has been set to D:\media and a virtual directory named gallery has been configured 
		/// in IIS that is accessible via http://yoursite.com/gallery, then you can configure the HTML template like this:
		/// HtmlTemplate="&lt;a href=&quot;http://yoursite.com/gallery{MediaObjectRelativeUrlNoHandler}&quot;&gt;Click to open&lt;/a&gt;"
		/// </example>
		private string ReplaceMediaObjectRelativeUrlNoHandlerParameter(string htmlOutput)
		{
			string moPath = Factory.LoadGallerySetting(_galleryId).FullMediaObjectPath;

			if (!this.MediaObjectPhysicalPath.StartsWith(moPath, StringComparison.OrdinalIgnoreCase))
				throw new BusinessException(String.Format(CultureInfo.CurrentCulture, "Expected this.MediaObjectPhysicalPath (\"{0}\") to start with AppSetting.Instance.MediaObjectPhysicalPath (\"{1}\"), but it did not.", this.MediaObjectPhysicalPath, moPath));

			string relativePath = this.MediaObjectPhysicalPath.Remove(0, moPath.Length).Trim(new char[] { System.IO.Path.DirectorySeparatorChar });

			relativePath = Utils.UrlEncode(relativePath, '\\');

			string relativeUrl = String.Concat("/", relativePath.Replace("\\", "/"));

			return htmlOutput.Replace("{MediaObjectRelativeUrlNoHandler}", relativeUrl);
		}

		/// <summary>
		/// Gets the HTML template information from the configuration file. If the configuration file
		/// does not specify an HTML template for the MIME type of this media object, an empty string is returned.
		/// </summary>
		/// <returns>Returns the HTML template information from the configuration file.</returns>
		private string GetHtmlOutputFromConfig()
		{
			IMediaTemplate browserTemplate = this.MimeType.GetMediaTemplate(this.Browsers);

			return (browserTemplate == null ? String.Empty : browserTemplate.HtmlTemplate);
		}

		/// <summary>
		/// Gets the ECMA script template information from the configuration file. If the configuration file
		/// does not specify an ECMA script template for the MIME type of this media object, an empty string is returned.
		/// </summary>
		/// <returns>Returns the ECMA script template information from the configuration file.</returns>
		private string GetScriptOutputFromConfig()
		{
			IMediaTemplate browserTemplate = this.MimeType.GetMediaTemplate(this.Browsers);

			return (browserTemplate == null ? String.Empty : browserTemplate.ScriptTemplate);
		}

		/// <summary>
		/// Determines if the image can be displayed in a standard web browser. For example, JPG, JPEG, PNG and GIF images can
		/// display, WMF and TIF cannot.
		/// </summary>
		/// <returns>Returns true if the image cannot be displayed in a standard browser (e.g. WMF, TIF); returns false if it can
		/// (e.g. JPG, JPEG, PNG and GIF).</returns>
		private bool IsImageBrowserIncompatible()
		{
			string originalFileExtension = System.IO.Path.GetExtension(this.MediaObjectPhysicalPath).ToLowerInvariant();

			return Array.IndexOf<string>(Factory.LoadGallerySetting(this.GalleryId).ImageTypesStandardBrowsersCanDisplay, originalFileExtension) < 0;
		}

		private string GetAlbumUrl()
		{
			return GetMediaObjectUrl(GalleryId, ((IAlbum)this._galleryObject).ThumbnailMediaObjectId, DisplayType);
		}

		private string GetMediaObjectUrl()
		{
			return GetMediaObjectUrl(GalleryId, MediaObjectId, DisplayType);
		}

		#endregion

		#region Public Static Methods

		/// <summary>
		/// Generate the URL to the media object. For example, for images this url can be assigned to the src attribute of an img tag.
		/// (ex: /galleryserverpro/handler/getmedia.ashx?moid=34&amp;dt=1&amp;g=1)
		/// The query string parameter will be encrypted if that option is enabled.
		/// </summary>
		/// <param name="galleryId">The gallery ID.</param>
		/// <param name="mediaObjectId">The unique identifier for the media object.</param>
		/// <param name="displayType">The type of the display object.</param>
		/// <returns>Gets the URL to the media object.</returns>
		public static string GenerateUrl(int galleryId, int mediaObjectId, DisplayObjectType displayType)
		{
			return GetMediaObjectUrl(galleryId, mediaObjectId, displayType);
		}

		/// <summary>
		/// Generates the HTML to display a nicely formatted thumbnail image of the specified <paramref name="galleryObject"/>, including a
		/// border, shadows and (possibly) rounded corners.
		/// </summary>
		/// <param name="galleryObject">The gallery object to be used as the source for the thumbnail image.</param>
		/// <param name="browserCaps">The browser capabilities. This may be found at Request.Browser.</param>
		/// <param name="includeHyperlinkToObject">if set to <c>true</c> wrap the image tag with a hyperlink so the user can click through
		/// to the media object view of the item.</param>
		/// <returns>
		/// Returns HTML that displays a nicely formatted thumbnail image of the specified <paramref name="galleryObject"/>
		/// </returns>
		public static string GenerateThumbnailHtml(IGalleryObject galleryObject, HttpBrowserCapabilities browserCaps, bool includeHyperlinkToObject)
		{
			if (IsInternetExplorer1To8(browserCaps))
			{
				return GenerateThumbnailHtmlForIE1To8(galleryObject, includeHyperlinkToObject);
			}

			return GenerateThumbnailHtmlForStandardBrowser(galleryObject, includeHyperlinkToObject);
		}

		#endregion

		#region Private Static Methods

		private static string GetMediaObjectUrl(int galleryId, int mediaObjectId, DisplayObjectType displayType)
		{
			//string queryString = String.Format(CultureInfo.InvariantCulture, "moid={1}&aid={2}&mo={3}&mtc={4}&dt={5}&isp={6}", galleryId, mediaObjectId, albumId, Uri.EscapeDataString(mediaObjectPhysicalPath), (int)mimeType.TypeCategory, (int)displayType, isPrivate.ToString());
			string queryString = String.Format(CultureInfo.InvariantCulture, "moid={0}&dt={1}&g={2}", mediaObjectId, (int)displayType, galleryId);

			// If necessary, encrypt, then URL encode the query string.
			if (AppSetting.Instance.EncryptMediaObjectUrlOnClient)
				queryString = Utils.UrlEncode(HelperFunctions.Encrypt(queryString));

			return string.Concat(Utils.GalleryRoot, "/handler/getmedia.ashx?", queryString);
		}

		/// <summary>
		/// Determines whether the browser specified in <paramref name="browserCaps" /> is Internet Explorer 1 to 8.
		/// </summary>
		/// <param name="browserCaps">The browser capabilities. This may be found at Request.Browser.</param>
		/// <returns>
		/// 	<c>true</c> if browser is Internet Explorer 1 to 8; otherwise, <c>false</c>.
		/// </returns>
		private static bool IsInternetExplorer1To8(HttpBrowserCapabilities browserCaps)
		{
			bool isIE1To8 = false;

			if (browserCaps.Browser.Equals("IE", StringComparison.OrdinalIgnoreCase))
			{
				decimal version;
				if (Decimal.TryParse(browserCaps.Version, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out version) && (version < (decimal)9.0))
				{
					isIE1To8 = true;
				}
			}

			return isIE1To8;
		}

		/// <summary>
		/// Generates the HTML to display a nicely formatted thumbnail image of the specified <paramref name="galleryObject" />, including a 
		/// border and shadows. This function generates a drop shadow using the technique described at http://www.positioniseverything.net/articles/dropshadows.html
		/// Since all other modern browsers, including IE9, support box shadows using native CSS commands, this function is only used for
		/// IE 1 to 8.
		/// </summary>
		/// <param name="galleryObject">The gallery object to be used as the source for the thumbnail image.</param>
		/// <param name="includeHyperlinkToObject">if set to <c>true</c> wrap the image tag with a hyperlink so the user can click through
		/// to the media object view of the item.</param>
		/// <returns>Returns HTML that displays a nicely formatted thumbnail image of the specified <paramref name="galleryObject" /></returns>
		private static string GenerateThumbnailHtmlForIE1To8(IGalleryObject galleryObject, bool includeHyperlinkToObject)
		{
			string html = String.Format(CultureInfo.InvariantCulture, @"
				<div class='op0' style='width:{0}px;height:{1}px;'>
					<div class='op1'>
						<div class='op2'>
							<div class='sb'>
								<div class='ib'>
									{2}
										<img src='{3}' title='{4}' alt='{4}' style='width:{5}px;height:{6}px;' />
									{7}
								</div>								
							</div>
						</div>
					</div>
				</div>
			", // 0
																	galleryObject.Thumbnail.Width + 15, // 0
																	galleryObject.Thumbnail.Height + 10, // 1
																	GenerateHyperlinkBegin(galleryObject, includeHyperlinkToObject), // 2
																	GetThumbnailUrl(galleryObject), // 3
																	GetHovertip(galleryObject), // 4
																	galleryObject.Thumbnail.Width, // 5
																	galleryObject.Thumbnail.Height, // 6
																	GenerateHyperlinkEnd(includeHyperlinkToObject) // 7
				);

			return html;
		}

		/// <summary>
		/// Generates the HTML to display a nicely formatted thumbnail image of the specified <paramref name="galleryObject" />, including a 
		/// border, shadows and rounded corners. This function generates a drop shadow using native CSS keywords.
		/// This works for all modern browsers except IE up until version 9, which finally added support.
		/// </summary>
		/// <param name="galleryObject">The gallery object to be used as the source for the thumbnail image.</param>
		/// <param name="includeHyperlinkToObject">if set to <c>true</c> wrap the image tag with a hyperlink so the user can click through
		/// to the media object view of the item.</param>
		/// <returns>Returns HTML that displays a nicely formatted thumbnail image of the specified <paramref name="galleryObject" /></returns>
		private static string GenerateThumbnailHtmlForStandardBrowser(IGalleryObject galleryObject, bool includeHyperlinkToObject)
		{
			string html = String.Format(CultureInfo.InvariantCulture, @"
		<div class='gsp_i_c'>
			{1}
				<img src='{2}' title='{3}' alt='{3}' style='width:{0}px;height:{4}px;' class='gsp_thmb_img' />
			{5}
		</div>
", // 0
																	galleryObject.Thumbnail.Width, // 0
																	GenerateHyperlinkBegin(galleryObject, includeHyperlinkToObject), // 1
																	GetThumbnailUrl(galleryObject), // 2
																	GetHovertip(galleryObject), // 3
																	galleryObject.Thumbnail.Height, // 4
																	GenerateHyperlinkEnd(includeHyperlinkToObject) // 5
				);

			return html;
		}

		private static string GenerateHyperlinkBegin(IGalleryObject galleryObject, bool generateHyperlink)
		{
			string html = String.Empty;

			if (generateHyperlink)
			{
				html = String.Format(CultureInfo.InvariantCulture, @"<a href='{0}' class='gsp_thmbLink' title='{1}'>",
														 GenerateUrl(galleryObject), // 0
														 GetHovertip(galleryObject)); // 1
			}

			return html;
		}

		private static string GenerateHyperlinkEnd(bool generateHyperlink)
		{
			return (generateHyperlink ? "</a>" : String.Empty);
		}

		/// <summary>
		/// Generates an URL to the current web page that points to the specified <paramref name="galleryObject" /> in the query string.
		/// Examples: /dev/gs/default.aspx?moid=2, /dev/gs/default.aspx?aid=5
		/// </summary>
		/// <param name="galleryObject">The gallery object to be linked to.</param>
		/// <returns>Returns an URL to the current web page that points to the gallery object.</returns>
		private static string GenerateUrl(IGalleryObject galleryObject)
		{
			string rv;

			if (galleryObject is Album)
			{
				// We have an album.
				rv = String.Concat(Utils.GetUrl(PageId.album, "aid={0}", galleryObject.Id));
			}
			else
			{
				rv = String.Concat(Utils.GetUrl(PageId.mediaobject, "moid={0}", galleryObject.Id));
			}

			if (String.IsNullOrEmpty(rv))
				throw new WebException("Unsupported media object type: " + galleryObject.GetType());

			return rv;
		}

		/// <summary>
		/// Return a string representing the title of the album. It is truncated and purged of
		/// HTML tags if necessary.  Returns an empty string if the gallery object is not an album
		/// (<paramref name="galleryObjectType"/> != typeof(<see cref="Album"/>))
		/// </summary>
		/// <param name="title">The title of the album as stored in the data store.</param>
		/// <param name="galleryObjectType">The type of the object to which the title belongs.</param>
		/// <param name="gallerySettings">The gallery settings.</param>
		/// <param name="allowAlbumTextWrapping">Indicates whether to allow the album title to wrap to a new line when required. When false,
		/// the CSS class "gsp_nowrap" is specified to prevent wrapping.</param>
		/// <returns>
		/// Returns a string representing the title of the album. It is truncated (if necessary)
		/// and purged of HTML tags.
		/// </returns>
		private static string GetAlbumText(string title, Type galleryObjectType, IGallerySettings gallerySettings, bool allowAlbumTextWrapping)
		{
			if (galleryObjectType != typeof(Album))
				return String.Empty;

			int maxLength = gallerySettings.MaxThumbnailTitleDisplayLength;
			string truncatedText = Utils.TruncateTextForWeb(title, maxLength);
			string nowrap = (allowAlbumTextWrapping ? String.Empty : " gsp_nowrap");

			if (truncatedText.Length != title.Length)
				return String.Format(CultureInfo.CurrentCulture, "<p class=\"albumtitle {0}\"><b>{1}</b> {2}...</p>", nowrap, Resources.GalleryServerPro.UC_ThumbnailView_Album_Title_Prefix_Text, truncatedText);
			else
				return String.Format(CultureInfo.CurrentCulture, "<p class=\"albumtitle {0}\"><b>{1}</b> {2}</p>", nowrap, Resources.GalleryServerPro.UC_ThumbnailView_Album_Title_Prefix_Text, truncatedText);
		}

		private static string GetHovertip(IGalleryObject galleryObject)
		{
			// Return the text to be used as the hovertip in standards compliant browsers. This is the
			// summary text for albums, and the title text for objects.
			string hoverTip = galleryObject.Title;

			IAlbum album = galleryObject as IAlbum;
			if (album != null)
			{
				if (album.Caption.Trim().Length > 0)
					hoverTip = album.Caption;
			}

			string hoverTipClean = Utils.HtmlEncode(Utils.RemoveHtmlTags(hoverTip));

			return hoverTipClean;
		}

		/// <summary>
		/// Get the URL to the thumbnail image of the specified gallery object. Either a media object or album may be specified. Example:
		/// /dev/gs/handler/getmedia.ashx?moid=34&amp;dt=1&amp;g=1
		/// The URL can be used to assign to the src attribute of an image tag (&lt;img src='...' /&gt;).
		/// </summary>
		/// <param name="galleryObject">The gallery object for which an URL to its thumbnail image is to be generated.
		/// Either a media object or album may be specified.</param>
		/// <returns>Returns the URL to the thumbnail image of the specified gallery object.</returns>
		private static string GetThumbnailUrl(IGalleryObject galleryObject)
		{
			if (galleryObject is Album)
				return GetAlbumThumbnailUrl(galleryObject);
			else
				return GenerateUrl(galleryObject.GalleryId, galleryObject.Id, DisplayObjectType.Thumbnail);
		}

		private static string GetAlbumThumbnailUrl(IGalleryObject galleryObject)
		{
			// Get a reference to the path to the thumbnail. If the user is anonymous and the thumbnail is from a private
			// media object or album, then specify 0 for the media object ID. This will be interpreted
			// by the image handler to generate a default, empty thumbnail image.
			int mediaObjectId = galleryObject.Thumbnail.MediaObjectId;

			if (!Utils.IsAuthenticated && (galleryObject.Thumbnail.MediaObjectId > 0))
			{
				try
				{
					IGalleryObject mediaObject = Factory.LoadMediaObjectInstance(galleryObject.Thumbnail.MediaObjectId);
					if (mediaObject.Parent.IsPrivate || mediaObject.IsPrivate)
					{
						mediaObjectId = 0;
					}
				}
				catch (InvalidMediaObjectException)
				{
					// We'll get here if the ID for the thumbnail doesn't represent an existing media object.
					mediaObjectId = 0;
				}
			}

			return GenerateUrl(galleryObject.GalleryId, mediaObjectId, DisplayObjectType.Thumbnail);
		}

		#endregion

	}
}
