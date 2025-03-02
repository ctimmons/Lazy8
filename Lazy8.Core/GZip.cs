/* Unless otherwise noted, this source code is licensed
   under the GNU Public License V3.

   See the LICENSE file in the root folder for details. */

using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lazy8.Core;

public static class GZipExtensions
{
  /// <summary>
  /// Determine if the file referenced by <paramref name="fi"/> is a GZipped file.
  /// </summary>
  /// <param name="fi">A <see cref="FileInfo"/> instance.</param>
  /// <returns><c>true</c> if the file is a GZipped file, <c>false</c> if it is not, the file does not exist,
  /// or the file's length is less than two. (The first two bytes are checked to see if the file
  /// contains GZip data.)</returns>
  public static Boolean IsGzippedFile(this FileInfo fi)
  {
    /* The first two bytes of a GZipped file are 0x1F and 0x8B.
    
       https://www.ietf.org/rfc/rfc1952.txt */

    if (!fi.Exists || (fi.Length < 2))
      return false;

    using (var fs = fi.OpenRead())
    {
      var byte1 = fs.ReadByte();
      var byte2 = fs.ReadByte();

      return (byte1 == 0x1F) && (byte2 == 0x8B);
    }
  }

  /// <summary>
  /// Determine if the file referenced by <paramref name="filename"/> is a GZipped file.
  /// </summary>
  /// <param name="filename">A <see cref="String"/> containing the fully qualified name of a file,
  /// or the relative file name.  Do not end the path with the directory separator character.</param>
  /// <returns><c>true</c> if the file is a GZipped file, <c>false</c> if it is not, the file does not exist,
  /// or the file's length is less than two. (The first two bytes are checked to see if the file
  /// contains GZip data.)</returns>
  public static Boolean IsGzippedFile(this String filename) => (new FileInfo(filename)).IsGzippedFile();

  /// <summary>
  /// Use GZip to compress the contents of <paramref name="uncompressedFileName"/> to <paramref name="compressedFileName"/>.
  /// <paramref name="compressedFileName"/> will be created if it doesn't exist. The file will be overwritten if it already exists.
  /// </summary>
  /// <param name="uncompressedFileName">A <see cref="String"/> containing a relative or absolute path.</param>
  /// <param name="compressedFileName">A <see cref="String"/> containing a relative or absolute path.</param>
  public static void CompressFile(String uncompressedFileName, String compressedFileName)
  {
    using (FileStream uncompressedFileStream = File.OpenRead(uncompressedFileName))
      using (FileStream compressedFileStream = File.Create(compressedFileName))
        uncompressedFileStream.CompressTo(compressedFileStream);
  }

  /// <summary>
  /// Use GZip to decompress the contents of <paramref name="compressedFileName"/> to <paramref name="decompressedFileName"/>.
  /// <paramref name="decompressedFileName"/> will be created if it doesn't exist. The file will be overwritten if it already exists.
  /// </summary>
  /// <param name="compressedFileName">A <see cref="String"/> containing a relative or absolute path.</param>
  /// <param name="decompressedFileName">A <see cref="String"/> containing a relative or absolute path.</param>
  public static void DecompressFile(String compressedFileName, String decompressedFileName)
  {
    using (FileStream compressedFileStream = File.OpenRead(compressedFileName))
      using (FileStream decompressedFileStream = File.Create(decompressedFileName))
        compressedFileStream.DecompressTo(decompressedFileStream);
  }

  /* Code below this comment for GZipExtensions is from StackOverflow answer
     https://stackoverflow.com/a/64582157/116198 posted by
     Ben Wilde (https://stackoverflow.com/users/2284031/ben-wilde).

     Modifications: 
       - Reformatted some methods
       - Changed parameter names to reflect whether the method inputs are compressed or uncompressed
       - Changed the type names to use System.* types
       - Added matching async methods

     Licensed under CC BY-SA 4.0 (https://creativecommons.org/licenses/by-sa/4.0/)
     See https://stackoverflow.com/help/licensing for more info. */

  /// <summary>
  /// Given a string <paramref name="uncompressedData"/>, compress it using GZip,
  /// and return the GZipped data as a Base64 <see cref="String"/>.
  /// <para><paramref name="uncompressedData"/> is not checked to see if it is already
  /// a Base64 string and/or if it has been compressed with GZip.</para>
  /// </summary>
  /// <param name="uncompressedData">A <see cref="String"/> containing the data to be compressed.</param>
  /// <returns>A Base64 <see cref="String"/> containing the GZipped data for <paramref name="uncompressedData"/>.</returns>
  public static String CompressToBase64(this String uncompressedData) =>
    Convert.ToBase64String(Encoding.UTF8.GetBytes(uncompressedData).Compress());

  /// <summary>
  /// The input string <paramref name="compressedData"/> is assumed to be Base64 encoded,
  /// and its contents compressed with the GZip algorithm.  The string is first decoded
  /// from Base64, then decompressed with GZip.
  /// </summary>
  /// <param name="compressedData">A Base64 encoded <see cref="String"/> containing GZipped data.</param>
  /// <returns>A <see cref="String"/> containing the un-GZipped data.</returns>
  public static String DecompressFromBase64(this String compressedData) =>
    Encoding.UTF8.GetString(Convert.FromBase64String(compressedData).Decompress());

  /// <summary>
  /// Return the bytes in the <paramref name="uncompressedData"/> <see cref="T:Byte[]"/> 
  /// as a GZipped <see cref="T:Byte[]"/>.
  /// </summary>
  /// <param name="uncompressedData">A <see cref="T:Byte[]"/>.</param>
  /// <returns>A GZipped <see cref="T:Byte[]"/>.</returns>
  public static Byte[] Compress(this Byte[] uncompressedData)
  {
    using (var sourceStream = new MemoryStream(uncompressedData))
    {
      using (var destinationStream = new MemoryStream())
      {
        sourceStream.CompressTo(destinationStream);
        return destinationStream.ToArray();
      }
    }
  }

  /// <summary>
  /// Return the bytes in the <paramref name="compressedData"/> <see cref="T:Byte[]"/>
  /// as an un-GZipped <see cref="T:Byte[]"/>.
  /// </summary>
  /// <param name="compressedData">A <see cref="T:Byte[]"/>.</param>
  /// <returns>An un-GZipped <see cref="T:Byte[]"/>.</returns>
  public static Byte[] Decompress(this Byte[] compressedData)
  {
    using (var sourceStream = new MemoryStream(compressedData))
    {
      using (var destinationStream = new MemoryStream())
      {
        sourceStream.DecompressTo(destinationStream);
        return destinationStream.ToArray();
      }
    }
  }

  /// <summary>
  /// Data in the <paramref name="inputStream"/> is compressed with the GZip algorithm
  /// and copied to the <paramref name="outputStream"/>.
  /// </summary>
  /// <param name="inputStream">A <see cref="Stream"/>.</param>
  /// <param name="outputStream">The <see cref="Stream"/> to which the GZipped contents of <paramref name="inputStream"/> will be copied.</param>
  public static void CompressTo(this Stream inputStream, Stream outputStream)
  {
    using (var gZipStream = new GZipStream(outputStream, CompressionMode.Compress))
      inputStream.CopyTo(gZipStream);
  }

  /// <summary>
  /// Data in the <paramref name="inputStream"/> is assumed to contain data compressed with the GZip algorithm.
  /// This data will be decompressed and copied to the <paramref name="outputStream"/>.
  /// </summary>
  /// <param name="inputStream">A <see cref="Stream"/> containing GZipped data.</param>
  /// <param name="outputStream">The <see cref="Stream"/> to which the un-GZipped contents of <paramref name="inputStream"/> will be copied.</param>
  public static void DecompressTo(this Stream inputStream, Stream outputStream)
  {
    using (var gZipStream = new GZipStream(inputStream, CompressionMode.Decompress))
      gZipStream.CopyTo(outputStream);
  }

  /* Asynchronous Methods */

  /// <summary>
  /// Given a string <paramref name="uncompressedData"/>, compress it using GZip,
  /// and return the GZipped data as a Base64 <see cref="String"/>.
  /// <para><paramref name="uncompressedData"/> is not checked to see if it is already
  /// a Base64 string and/or if it has been compressed with GZip.</para>
  /// </summary>
  /// <param name="uncompressedData">A <see cref="String"/> containing the data to be compressed.</param>
  /// <param name="token">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default value is None.</param>
  /// <returns>A <see cref="Task"/> that represents the asynchronous compression operation.
  /// The task's result contains a Base64 <see cref="String"/> containing the GZipped data for <paramref name="uncompressedData"/>.</returns>
  public static async Task<String> CompressToBase64Async(this String uncompressedData, CancellationToken token = default) =>
    Convert.ToBase64String(await Encoding.UTF8.GetBytes(uncompressedData).CompressAsync(token).ConfigureAwait(false));

  /// <summary>
  /// The input string <paramref name="compressedData"/> is assumed to be Base64 encoded,
  /// and its contents compressed with the GZip algorithm.  The string is first decoded
  /// from Base64, then decompressed with GZip.
  /// </summary>
  /// <param name="compressedData">A Base64 encoded <see cref="String"/> containing GZipped data.</param>
  /// <param name="token">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default value is None.</param>
  /// <returns>A <see cref="Task"/> that represents the asynchronous decompression operation.
  /// The task's result contains a Base64 <see cref="String"/> containing the un-GZipped data for <paramref name="uncompressedData"/>.</returns>
  public static async Task<String> DecompressFromBase64Async(this String compressedData, CancellationToken token = default) =>
    Encoding.UTF8.GetString(await Convert.FromBase64String(compressedData).DecompressAsync(token).ConfigureAwait(false));

  /// <summary>
  /// Return the bytes in the <paramref name="uncompressedData"/> <see cref="T:Byte[]"/> 
  /// as a GZipped <see cref="T:Byte[]"/>.
  /// </summary>
  /// <param name="uncompressedData">A <see cref="T:Byte[]"/>.</param>
  /// <param name="token">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default value is None.</param>
  /// <returns>A <see cref="Task"/> that represents the asynchronous compression operation.
  /// The task's result contains a GZipped <see cref="T:Byte[]"/>.</returns>
  public static async Task<Byte[]> CompressAsync(this Byte[] uncompressedData, CancellationToken token = default)
  {
    using (var sourceStream = new MemoryStream(uncompressedData))
    {
      using (var destinationStream = new MemoryStream())
      {
        await sourceStream.CompressToAsync(destinationStream, token).ConfigureAwait(false);
        return destinationStream.ToArray();
      }
    }
  }

  /// <summary>
  /// Return the bytes in the <paramref name="compressedData"/> <see cref="T:Byte[]"/>
  /// as an un-GZipped <see cref="T:Byte[]"/>.
  /// </summary>
  /// <param name="compressedData">A <see cref="T:Byte[]"/>.</param>
  /// <param name="token">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default value is None.</param>
  /// <returns>A <see cref="Task"/> that represents the asynchronous decompression operation.
  /// The task's result contains an un-GZipped <see cref="T:Byte[]"/>.</returns>
  public static async Task<Byte[]> DecompressAsync(this Byte[] compressedData, CancellationToken token = default)
  {
    using (var sourceStream = new MemoryStream(compressedData))
    {
      using (var destinationStream = new MemoryStream())
      {
        await sourceStream.DecompressToAsync(destinationStream, token).ConfigureAwait(false);
        return destinationStream.ToArray();
      }
    }
  }

  /// <summary>
  /// Data in the <paramref name="inputStream"/> is compressed with the GZip algorithm
  /// and copied to the <paramref name="outputStream"/>.
  /// </summary>
  /// <param name="inputStream">A <see cref="Stream"/>.</param>
  /// <param name="outputStream">The <see cref="Stream"/> to which the GZipped contents of <paramref name="inputStream"/> will be copied.</param>
  /// <param name="token">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default value is None.</param>
  /// <returns>A <see cref="Task"/> that represents the asynchronous compression operation.</returns>
  public static async Task CompressToAsync(this Stream inputStream, Stream outputStream, CancellationToken token = default)
  {
    using (var gZipStream = new GZipStream(outputStream, CompressionMode.Compress))
      await inputStream.CopyToAsync(gZipStream, token).ConfigureAwait(false);
  }

  /// <summary>
  /// Data in the <paramref name="inputStream"/> is assumed to contain data compressed with the GZip algorithm.
  /// This data will be decompressed and copied to the <paramref name="outputStream"/>.
  /// </summary>
  /// <param name="inputStream">A <see cref="Stream"/> containing GZipped data.</param>
  /// <param name="outputStream">The <see cref="Stream"/> to which the un-GZipped contents of <paramref name="inputStream"/> will be copied.</param>
  /// <param name="token">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default value is None.</param>
  /// <returns>A <see cref="Task"/> that represents the asynchronous decompression operation.</returns>
  public static async Task DecompressToAsync(this Stream inputStream, Stream outputStream, CancellationToken token = default)
  {
    using (var gZipStream = new GZipStream(inputStream, CompressionMode.Decompress))
      await gZipStream.CopyToAsync(outputStream, token).ConfigureAwait(false);
  }
}
