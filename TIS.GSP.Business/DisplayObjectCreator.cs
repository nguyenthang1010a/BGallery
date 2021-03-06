using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GalleryServerPro.Business.Interfaces;
using GalleryServerPro.Business.Metadata;
using GalleryServerPro.Events;
using GalleryServerPro.Events.CustomExceptions;

namespace GalleryServerPro.Business
{
	/// <summary>
	///   Provides base functionality for creating and saving the files associated with <see cref="IGalleryObject" /> objects.
	/// </summary>
	public abstract class DisplayObjectCreator : IDisplayObjectCreator
	{
		#region Properties

		/// <summary>
		///   Gets or sets the gallery object this instance applies to.
		/// </summary>
		protected IGalleryObject GalleryObject { get; set; }

		/// <summary>
		///   Gets the gallery settings associated with the <see cref="GalleryObject" />.
		/// </summary>
		protected IGallerySettings GallerySettings
		{
			get { return Factory.LoadGallerySetting(GalleryObject.GalleryId); }
		}

		#endregion

		#region Methods

		/// <summary>
		/// Gets or sets the display object this instance belongs to.
		/// </summary>
		/// <value>The display object this instance belongs to.</value>
		public IDisplayObject Parent { get; set; }

		/// <summary>
		///   Generate the file for this display object and save it to the file system. The routine may decide that
		///   a file does not need to be generated, usually because it already exists. However, it will always be
		///   created if the relevant flag is set on the parent <see cref="IGalleryObject" />. (Example: If
		///   <see cref="IGalleryObject.RegenerateThumbnailOnSave" /> = true, the thumbnail file will always be created.) No data is
		///   persisted to the data store.
		/// </summary>
		/// <exception cref="System.NotImplementedException"></exception>
		public virtual void GenerateAndSaveFile()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Gets the width and height of the specified <paramref name="displayObject" />. The value is calculated from the 
		/// physical file. Returns an empty <see cref="System.Windows.Size" /> instance if the value cannot be computed or 
		/// is not applicable to the object (for example, for audio files and external media objects).
		/// </summary>
		/// <returns><see cref="System.Windows.Size" />.</returns>
		public Size GetSize(IDisplayObject displayObject)
		{
			if (AppSetting.Instance.AppTrustLevel == ApplicationTrustLevel.Full)
			{
				try
				{
					return GetSizeUsingWpf(displayObject);
				}
				catch (NotSupportedException)
				{
					return GetSizeUsingGdi(displayObject);
				}
			}
			else
			{
				return GetSizeUsingGdi(displayObject);
			}
		}

		/// <summary>
		///   Determine name of new JPEG file and ensure it is unique in the directory. (Example: If original = puppy.jpg,
		///   thumbnail = zThumb_puppy.jpg) The new file name's extension will be .jpeg if the original was .jpeg; otherwise it will
		///   be .jpg.
		/// </summary>
		/// <param name="filePath">The path to the directory where the file is to be created.</param>
		/// <param name="fileNamePrefix">The file name prefix. Examples: "zThumb_", "zOpt_"</param>
		/// <returns>Returns the name of the new file name and ensures it is unique in the directory.</returns>
		protected string GenerateJpegFilename(string filePath, string fileNamePrefix)
		{
			string extension = ((Path.GetExtension(GalleryObject.Original.FileInfo.Name) ?? String.Empty).Equals(".jpeg", StringComparison.InvariantCultureIgnoreCase) ? ".jpeg" : ".jpg");
			string nameWithoutExtension = Path.GetFileNameWithoutExtension(GalleryObject.Original.FileInfo.Name);
			string thumbnailFilename = String.Concat(fileNamePrefix, nameWithoutExtension, extension);

			return HelperFunctions.ValidateFileName(filePath, thumbnailFilename);
		}

		/// <summary>
		///   Calculate new width and height values of an existing <paramref name="size" /> instance, making the length
		///   of the longest side equal to <paramref name="maxLength" />. The aspect ratio if preserved. If
		///   <paramref
		///     name="autoEnlarge" />
		///   is <c>true</c>, then increase the size so that the longest side equals <paramref name="maxLength" />
		///   (i.e. enlarge a small image if necessary).
		/// </summary>
		/// <param name="size">The current size of an object.</param>
		/// <param name="maxLength">The target length (in pixels) of the longest side.</param>
		/// <param name="autoEnlarge">
		///   A value indicating whether to enlarge objects that are smaller than the
		///   <paramref name="size" />. If true, the new width and height will be increased if necessary. If false, the original
		///   width and height are returned when their dimensions are smaller than <paramref name="maxLength" />. This
		///   parameter has no effect when <paramref name="maxLength" /> is greater than the width and height of
		///   <paramref
		///     name="size" />
		///   .
		/// </param>
		/// <returns>
		///   Returns a <see cref="Size" /> instance conforming to the requested parameters.
		/// </returns>
		public static Size CalculateWidthAndHeight(Size size, int maxLength, bool autoEnlarge)
		{
			int newWidth, newHeight;

			if (!autoEnlarge && (maxLength > size.Width) && (maxLength > size.Height))
			{
				// Bitmap is smaller than desired thumbnail dimensions but autoEnlarge = false. Don't enlarge thumbnail; 
				// just use original size.
				newWidth = (int)size.Width;
				newHeight = (int)size.Height;
			}
			else if (size.Width > size.Height)
			{
				// Bitmap is in landscape format (width > height). The width will be the longest dimension.
				newWidth = maxLength;
				newHeight = (int)(size.Height * newWidth / size.Width);
			}
			else
			{
				// Bitmap is in portrait format (height > width). The height will be the longest dimension.
				newHeight = maxLength;
				newWidth = (int)(size.Width * newHeight / size.Height);
			}

			return new Size(newWidth, newHeight);
		}

		/// <summary>
		///   Creates an image file having a max length of <paramref name="maxLength" /> and JPEG quality of
		///   <paramref
		///     name="jpegQuality" />
		///   from the original file of <see cref="GalleryObject" />. The file is saved to the location
		///   <paramref
		///     name="newFilePath" />
		///   .
		///   The width and height of the generated image is returned as a <see cref="Size" /> instance.
		/// </summary>
		/// <param name="newFilePath">The full path where the image will be saved.</param>
		/// <param name="maxLength">The maximum length of one side of the image.</param>
		/// <param name="jpegQuality">The JPEG quality.</param>
		/// <returns>
		///   Returns a <see cref="Size" /> instance containing the width and height of the generated image.
		/// </returns>
		/// <exception cref="UnsupportedImageTypeException">
		///   Thrown when Gallery Server Pro cannot process the image,
		///   most likely because it is corrupt or an unsupported image type.
		/// </exception>
		protected Size GenerateImageUsingDotNet(string newFilePath, int maxLength, int jpegQuality)
		{
			if (AppSetting.Instance.AppTrustLevel == ApplicationTrustLevel.Full)
			{
				try
				{
					return GenerateImageUsingWpf(newFilePath, maxLength, jpegQuality);
				}
				catch (UnsupportedImageTypeException)
				{
					// If we can't process an image using WPF, try the older GDI+ technique. For example, WMF images fail with WPF
					// but succeed with GDI+.
					return GenerateImageUsingGdi(newFilePath, maxLength, jpegQuality);
				}
			}
			else
			{
				return GenerateImageUsingGdi(newFilePath, maxLength, jpegQuality);
			}
		}

		protected Size GenerateImageUsingImageMagick(string newFilePath, int maxLength, int jpegQuality)
		{
			// Generate a temporary filename to store the thumbnail created by ImageMagick.
			string tmpImagePath = Path.Combine(AppSetting.Instance.TempUploadDirectory, String.Concat(Guid.NewGuid().ToString(), ".jpg"));

			if (!String.IsNullOrEmpty(GalleryObject.Original.TempFilePath))
			{
				// Use the image that was created earlier in the thumbnail generator.
				tmpImagePath = GalleryObject.Original.TempFilePath;
			}

			// Request that ImageMagick create the image. If successful, the file will be created. If not, it fails silently.
			if (!File.Exists(tmpImagePath))
			{
				ImageMagick.GenerateImage(GalleryObject.Original.FileNamePhysicalPath, tmpImagePath, GalleryObject.GalleryId);
			}

			if (File.Exists(tmpImagePath))
			{
				// Save the path so it can be used later by the optimized image creator.
				GalleryObject.Original.TempFilePath = tmpImagePath;

				try
				{
					// ImageMagick successfully created an image. Now resize it to the width and height we need.
					// We can safely use the WPF version since we'll only get this far if we're running in Full Trust.
					return GenerateImageUsingWpf(tmpImagePath, newFilePath, maxLength, jpegQuality);
				}
				catch (Exception ex)
				{
					ex.Data.Add("GSP Info", String.Format("This error occurred while trying to process the ImageMagick-generated file {0}. The original file is {1}. The gallery will try to create an image using .NET instead.", tmpImagePath, GalleryObject.Original.FileNamePhysicalPath));
					EventController.RecordError(ex, AppSetting.Instance, GalleryObject.GalleryId, Factory.LoadGallerySettings());

					return Size.Empty;
				}
			}

			return Size.Empty;
		}

		/// <summary>
		/// Rotates the <paramref name="filePath" /> by the amount specified in <see cref="IGalleryObject.Rotation" />.
		/// The rotated file is saved with a JPEG quality of <paramref name="jpegQuality" />. Returns an object indicating
		/// the actual rotation applied to the object and its final size. Some objects may be rotated an amount different
		/// than the requested amount when the displayed orientation is different than the file's actual orientation.
		/// The metadata in the original file is preserved to the extent possible.
		/// </summary>
		/// <param name="filePath">The full path to the file.</param>
		/// <param name="jpegQuality">The JPEG quality.</param>
		/// <returns>Returns a <see cref="Tuple" /> indicating the actual <see cref="MediaObjectRotation" /> and final 
		/// <see cref="Size" /> and of the generated file.</returns>
		protected Tuple<MediaObjectRotation, Size> Rotate(string filePath, int jpegQuality)
		{
			switch (this.GalleryObject.GalleryObjectType)
			{
				case GalleryObjectType.Image:
					return RotateImage(filePath, jpegQuality);

				// Rotate videos only when we're dealing with the thumbnail image. Actual video files will be rotated
				// in MediaConversionQueue/FFmpeg.
				case GalleryObjectType.Video:
					if (this.Parent.DisplayType == DisplayObjectType.Thumbnail)
					{
						return RotateImage(filePath, jpegQuality);
					}
					break;
			}

			return new Tuple<MediaObjectRotation, Size>(MediaObjectRotation.Rotate0, Size.Empty);
		}

		private Tuple<MediaObjectRotation, Size> RotateImage(string filePath, int jpegQuality)
		{
			// Grab a reference to the file's metadata properties so we can add them back after the rotation.
			System.Drawing.Imaging.PropertyItem[] propItems = null;
			if (Parent.DisplayType == DisplayObjectType.Original)
			{
				using (var bmp = new System.Drawing.Bitmap(filePath))
				{
					propItems = bmp.PropertyItems;
				}
			}

			Tuple<MediaObjectRotation, Size> rotateResult;
			if (AppSetting.Instance.AppTrustLevel == ApplicationTrustLevel.Full)
			{
				try
				{
					rotateResult = RotateUsingWpf(filePath, jpegQuality);
				}
				catch (NotSupportedException)
				{
					rotateResult = RotateUsingGdi(filePath, jpegQuality);
				}
			}
			else
			{
				rotateResult = RotateUsingGdi(filePath, jpegQuality);
			}

			if (rotateResult.Item1 > MediaObjectRotation.Rotate0)
			{
				AddMetaValuesBackToRotatedImage(filePath, propItems); // Add meta values back to file
			}

			return rotateResult;
		}

		/// <summary>
		/// Check the orientation meta value of the original media object. If the orientation is anything other than
		/// normal (0 degrees), rotate <paramref name="newFilePath" /> to be in the correct orientation. Returns
		/// <see cref="Size.Empty" /> if no rotation is performed.
		/// </summary>
		/// <param name="newFilePath">The full path of the file to rotate.</param>
		/// <param name="jpegQuality">The JPEG quality.</param>
		/// <returns>Returns a <see cref="Size" /> instance containing the width and height of the generated image.</returns>
		protected Size ExecuteAutoRotation(string newFilePath, int jpegQuality)
		{
			// Check for need to rotate and rotate if necessary.
			if (GalleryObject.Rotation != MediaObjectRotation.NotSpecified)
			{
				// When a rotation is explicitly being performed, we don't want to do an auto-rotation.
				return Size.Empty;
			}

			switch (GalleryObject.GetOrientation())
			{
				case Orientation.Rotated90:
				case Orientation.Rotated180:
				case Orientation.Rotated270:
					var rotateResult = Rotate(newFilePath, jpegQuality);
					return rotateResult.Item2;

				default:
					return Size.Empty;
			}
		}

		#endregion

		#region Functions

		/// <overloads>
		///   Creates an image file using WPF.
		/// </overloads>
		/// <summary>
		///   Creates an image file having a max length of <paramref name="maxLength" /> and JPEG quality of
		///   <paramref
		///     name="jpegQuality" />
		///   from the original file of <see cref="GalleryObject" />. The file is saved to the location
		///   <paramref
		///     name="newFilePath" />
		///   .
		///   The width and height of the generated image is returned as a <see cref="Size" /> instance. The WPF classes
		///   are used to create the image, which are faster than the GDI classes. The caller must verify application is running in Full Trust.
		/// </summary>
		/// <param name="newFilePath">The full path where the image will be saved.</param>
		/// <param name="maxLength">The maximum length of one side of the image.</param>
		/// <param name="jpegQuality">The JPEG quality.</param>
		/// <returns>
		///   Returns a <see cref="Size" /> instance containing the width and height of the generated image.
		/// </returns>
		/// <exception cref="UnsupportedImageTypeException">
		///   Thrown when Gallery Server Pro cannot process the image,
		///   most likely because it is corrupt or an unsupported image type.
		/// </exception>
		private Size GenerateImageUsingWpf(string newFilePath, int maxLength, int jpegQuality)
		{
			return GenerateImageUsingWpf(GalleryObject.Original.FileNamePhysicalPath, newFilePath, maxLength, jpegQuality);
		}

		/// <summary>
		///   Creates an image file having a max length of <paramref name="maxLength" /> and JPEG quality of
		///   <paramref
		///     name="jpegQuality" />
		///   from <paramref name="sourceFilePath" />. The file is saved to the location <paramref name="newFilePath" />.
		///   The width and height of the generated image is returned as a <see cref="Size" /> instance. The WPF classes
		///   are used to create the image, which are faster than the GDI classes. The caller must verify application is running in Full Trust.
		/// </summary>
		/// <param name="sourceFilePath">The full path of the source image.</param>
		/// <param name="newFilePath">The full path where the image will be saved.</param>
		/// <param name="maxLength">The maximum length of one side of the image.</param>
		/// <param name="jpegQuality">The JPEG quality.</param>
		/// <returns>
		///   Returns a <see cref="Size" /> instance containing the width and height of the generated image.
		/// </returns>
		/// <exception cref="UnsupportedImageTypeException">
		///   Thrown when Gallery Server Pro cannot process the image, most likely because it is corrupt or an unsupported image type.
		/// </exception>
		private Size GenerateImageUsingWpf(string sourceFilePath, string newFilePath, int maxLength, int jpegQuality)
		{
			try
			{
				// Technique adapted from http://weblogs.asp.net/bleroy/archive/2009/12/10/resizing-images-from-the-server-using-wpf-wic-instead-of-gdi.aspx
				var photoBytes = File.ReadAllBytes(sourceFilePath);
				using (var photoStream = new MemoryStream(photoBytes))
				{
					var photo = ReadBitmapFrame(photoStream);

					var newSize = CalculateWidthAndHeight(new Size(photo.PixelWidth, photo.PixelHeight), maxLength, false);

					var bmpFrame = FastResize(photo, newSize.Width, newSize.Height);
					var resizedBytes = GenerateJpegByteArray(bmpFrame, jpegQuality);

					File.WriteAllBytes(newFilePath, resizedBytes);

					var rotatedSize = ExecuteAutoRotation(newFilePath, jpegQuality);

					return (rotatedSize.IsEmpty ? newSize : rotatedSize);
				}
			}
			catch (NotSupportedException ex)
			{
				throw new UnsupportedImageTypeException(GalleryObject, ex);
			}
		}

		private Tuple<MediaObjectRotation, Size> RotateUsingWpf(string filePath, int jpegQuality)
		{
			var actualRotation = GalleryObject.CalculateNeededRotation();

			if (actualRotation <= MediaObjectRotation.Rotate0)
			{
				return new Tuple<MediaObjectRotation, Size>(actualRotation, Size.Empty);
			}

			var bytes = File.ReadAllBytes(filePath);
			using (var stream = new MemoryStream(bytes))
			{
				var image = new TransformedBitmap(ReadBitmapFrame(stream), new RotateTransform(GetRotationInDegrees(actualRotation)));

				var rotatedImg = BitmapFrame.Create(image);

				var rotatedBytes = GenerateJpegByteArray(rotatedImg, jpegQuality);

				File.WriteAllBytes(filePath, rotatedBytes);

				return new Tuple<MediaObjectRotation, Size>(actualRotation, new Size(rotatedImg.PixelWidth, rotatedImg.PixelHeight));
			}
		}

		private Tuple<MediaObjectRotation, Size> RotateUsingGdi(string filePath, int jpegQuality)
		{
			var actualRotation = GalleryObject.CalculateNeededRotation();

			if (actualRotation <= MediaObjectRotation.Rotate0)
			{
				return new Tuple<MediaObjectRotation, Size>(actualRotation, Size.Empty);
			}

			// Get reference to the bitmap from which the optimized image will be generated.
			using (var originalBitmap = new System.Drawing.Bitmap(filePath))
			{
				var imgFormat = originalBitmap.RawFormat; // Need to grab the format before we rotate or else we lose it (it changes to MemoryBmp)

				try
				{
					originalBitmap.RotateFlip(GetRotateFlipType());
				}
				catch (System.Runtime.InteropServices.ExternalException)
				{
					throw new UnsupportedImageTypeException();
				}

				ImageHelper.SaveImageToDisk(originalBitmap, GalleryObject.Original.FileNamePhysicalPath, imgFormat, jpegQuality);

				return new Tuple<MediaObjectRotation, Size>(actualRotation, new Size(originalBitmap.Width, originalBitmap.Height));
			}
		}

		/// <summary>
		///   Create an image file having a max length of <paramref name="maxLength" /> and JPEG quality of
		///   <paramref
		///     name="jpegQuality" />
		///   from the original file of <see cref="GalleryObject" />. The file is saved to the location
		///   <paramref
		///     name="newFilePath" />
		///   .
		///   The width and height of the generated image is returned as a <see cref="Size" /> instance. The GDI+ classes
		///   are used to create the image, which are slower than WPF but run in Medium Trust.
		/// </summary>
		/// <param name="newFilePath">The full path where the image will be saved.</param>
		/// <param name="maxLength">The maximum length of one side of the image.</param>
		/// <param name="jpegQuality">The JPEG quality.</param>
		/// <returns>
		///   Returns a <see cref="Size" /> instance containing the width and height of the generated image.
		/// </returns>
		/// <exception cref="UnsupportedImageTypeException">
		///   Thrown when Gallery Server Pro cannot process the image,
		///   most likely because it is corrupt or an unsupported image type.
		/// </exception>
		private Size GenerateImageUsingGdi(string newFilePath, int maxLength, int jpegQuality)
		{
			try
			{
				using (var source = new System.Drawing.Bitmap(GalleryObject.Original.FileInfo.FullName))
				{
					var newSize = CalculateWidthAndHeight(new Size(source.Width, source.Height), maxLength, false);

					// Generate the new image and save to disk.
					newSize = ImageHelper.SaveImageFile(source, newFilePath, System.Drawing.Imaging.ImageFormat.Jpeg, newSize.Width, newSize.Height, jpegQuality);

					var rotatedSize = ExecuteAutoRotation(newFilePath, jpegQuality);

					return rotatedSize.IsEmpty ? newSize : rotatedSize;
				}
			}
			catch (ArgumentException ex)
			{
				throw new UnsupportedImageTypeException(GalleryObject, ex);
			}
			catch (ExternalException ex)
			{
				throw new UnsupportedImageTypeException(GalleryObject, ex);
			}
			catch (OutOfMemoryException ex)
			{
				throw new UnsupportedImageTypeException(GalleryObject, ex);
			}
		}

		private static BitmapFrame ReadBitmapFrame(MemoryStream photoStream)
		{
			var photoDecoder = BitmapDecoder.Create(photoStream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.None);

			return photoDecoder.Frames[0];
		}

		private static BitmapFrame FastResize(BitmapFrame photo, double width, double height)
		{
			var dpiXFactor = (photo.DpiX > 0 ? 96/ photo.DpiX : 1);
			var dpiYFactor = (photo.DpiY > 0 ? 96 / photo.DpiY : 1);

			var target = new TransformedBitmap(
				photo,
				new ScaleTransform(
					width / photo.Width * dpiXFactor,
					height / photo.Height * dpiYFactor,
					0, 0));

			return BitmapFrame.Create(target);
		}

		private static BitmapFrame Resize(BitmapFrame photo, int width, int height, BitmapScalingMode scalingMode = BitmapScalingMode.Fant)
		{
			// This is a more flexible, albiet slower alternative to FastResize. For more info:
			// http://weblogs.asp.net/bleroy/archive/2009/12/10/resizing-images-from-the-server-using-wpf-wic-instead-of-gdi.aspx
			var group = new DrawingGroup();
			RenderOptions.SetBitmapScalingMode(group, scalingMode);
			group.Children.Add(new ImageDrawing(photo, new Rect(0, 0, width, height)));
			var targetVisual = new DrawingVisual();
			DrawingContext targetContext = targetVisual.RenderOpen();
			targetContext.DrawDrawing(group);
			var target = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Default);
			targetContext.Close();
			target.Render(targetVisual);

			return BitmapFrame.Create(target);
		}

		private static byte[] GenerateJpegByteArray(BitmapFrame targetFrame, int quality)
		{
			byte[] targetBytes;
			using (var memoryStream = new MemoryStream())
			{
				var targetEncoder = new JpegBitmapEncoder
															{
																QualityLevel = quality
															};

				targetEncoder.Frames.Add(targetFrame);
				targetEncoder.Save(memoryStream);
				targetBytes = memoryStream.ToArray();
			}

			return targetBytes;
		}

		private Size GetSizeUsingWpf(IDisplayObject displayObject)
		{
			try
			{
				var photoBytes = File.ReadAllBytes(displayObject.FileNamePhysicalPath);
				using (var photoStream = new MemoryStream(photoBytes))
				{
					var photo = ReadBitmapFrame(photoStream);
					return new Size(photo.Width, photo.Height);
				}
			}
			catch (NotSupportedException)
			{
				return GetSizeUsingGdi(displayObject);
			}
			catch (Exception ex)
			{
				if (!ex.Data.Contains("SizeMsg"))
				{
					ex.Data.Add("SizeMsg", String.Format("Unable to get the width and height of media object {0} ({1}). Display Type {2}", GalleryObject.Id, displayObject.FileNamePhysicalPath, displayObject.DisplayType));
				}

				EventController.RecordError(ex, AppSetting.Instance, GalleryObject.GalleryId, Factory.LoadGallerySettings());

				return Size.Empty;
			}
		}

		private static Size GetSizeUsingGdi(IDisplayObject displayObject)
		{
			try
			{
				using (var source = new System.Drawing.Bitmap(displayObject.FileNamePhysicalPath))
				{
					return new Size(source.Width, source.Height);
				}
			}
			catch (ArgumentException)
			{
				return Size.Empty;
			}
			catch (ExternalException ex)
			{
				return Size.Empty;
			}
			catch (OutOfMemoryException ex)
			{
				return Size.Empty;
			}
		}

		private static double GetRotationInDegrees(MediaObjectRotation rotation)
		{
			switch (rotation)
			{
				case MediaObjectRotation.Rotate0:
					return 0;
				case MediaObjectRotation.Rotate90:
					return 90;
				case MediaObjectRotation.Rotate180:
					return 180;
				case MediaObjectRotation.Rotate270:
					return 270;
				default:
					return 0;
			}
		}

		private System.Drawing.RotateFlipType GetRotateFlipType()
		{
			switch (GalleryObject.Rotation)
			{
				case MediaObjectRotation.Rotate0:
					return System.Drawing.RotateFlipType.RotateNoneFlipNone;
				case MediaObjectRotation.Rotate90:
					return System.Drawing.RotateFlipType.Rotate90FlipNone;
				case MediaObjectRotation.Rotate180:
					return System.Drawing.RotateFlipType.Rotate180FlipNone;
				case MediaObjectRotation.Rotate270:
					return System.Drawing.RotateFlipType.Rotate270FlipNone;
				default:
					return System.Drawing.RotateFlipType.RotateNoneFlipNone;
			}
		}

		/// <summary>
		/// Add the <paramref name="metaValues" /> back to the <paramref name="filePath" />.
		/// </summary>
		/// <param name="filePath">The full path to the file.</param>
		/// <param name="metaValues">The property items. If null, no action is taken.</param>
		private void AddMetaValuesBackToRotatedImage(string filePath, System.Drawing.Imaging.PropertyItem[] metaValues)
		{
			if (metaValues == null)
				return;

			// Create a copy of the file and add the metadata to it.
			string tmpImagePath;
			using (var targetImage = new System.Drawing.Bitmap(filePath))
			{
				foreach (var propertyItem in metaValues)
				{
					// Don't copy width, height or orientation meta items.
					var metasToNotCopy = new[]
						                     {
																	 RawMetadataItemName.ImageWidth,
																	 RawMetadataItemName.ImageHeight,
																	 RawMetadataItemName.ExifPixXDim,
																	 RawMetadataItemName.ExifPixYDim,
																	 RawMetadataItemName.Orientation
						                     };

					if (Array.IndexOf(metasToNotCopy, (RawMetadataItemName)propertyItem.Id) >= 0)
						continue;

					targetImage.SetPropertyItem(propertyItem);
				}

				// Save image to temporary location. We can't overwrite the original path because the Bitmap has a lock on it.
				tmpImagePath = Path.Combine(AppSetting.Instance.TempUploadDirectory, String.Concat(Guid.NewGuid().ToString(), ".jpg"));
				ImageHelper.SaveImageToDisk(targetImage, tmpImagePath, System.Drawing.Imaging.ImageFormat.Jpeg, GallerySettings.OriginalImageJpegQuality);
			}

			// Now that the original file is freed up, delete it and move the temp file into its place.
			if (File.Exists(filePath))
				File.Delete(filePath);

			File.Move(tmpImagePath, filePath);
		}

		#endregion
	}
}