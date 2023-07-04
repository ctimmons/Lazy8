using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lazy8.Core;

/* Code for GZipExtensions is from StackOverflow answer https://stackoverflow.com/a/64582157/116198
   posted by Ben Wilde (https://stackoverflow.com/users/2284031/ben-wilde).

   Modifications: 
     - Reformatted some methods
     - Changed parameter names to reflect whether the method inputs are compressed or uncompressed
     - Changed the type names to use System.* types
     - Added matching async methods

   Licensed under CC BY-SA 4.0 (https://creativecommons.org/licenses/by-sa/4.0/)
   See https://stackoverflow.com/help/licensing for more info. */

public static class GZipExtensions
{
  public static String CompressToBase64(this String uncompressedData) =>
    Convert.ToBase64String(Encoding.UTF8.GetBytes(uncompressedData).Compress());

  public static String DecompressFromBase64(this String compressedData) =>
    Encoding.UTF8.GetString(Convert.FromBase64String(compressedData).Decompress());

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

  public static void CompressTo(this Stream stream, Stream outputStream)
  {
    using (var gZipStream = new GZipStream(outputStream, CompressionMode.Compress))
      stream.CopyTo(gZipStream);
  }

  public static void DecompressTo(this Stream stream, Stream outputStream)
  {
    using (var gZipStream = new GZipStream(stream, CompressionMode.Decompress))
      gZipStream.CopyTo(outputStream);
  }

  /* Asynchronous Methods */

  public static async Task<String> CompressToBase64Async(this String uncompressedData, CancellationToken token = default) =>
    Convert.ToBase64String(await Encoding.UTF8.GetBytes(uncompressedData).CompressAsync(token));

  public static async Task<String> DecompressFromBase64Async(this String compressedData, CancellationToken token = default) =>
    Encoding.UTF8.GetString(await Convert.FromBase64String(compressedData).DecompressAsync(token));

  public static async Task<Byte[]> CompressAsync(this Byte[] uncompressedData, CancellationToken token = default)
  {
    using (var sourceStream = new MemoryStream(uncompressedData))
    {
      using (var destinationStream = new MemoryStream())
      {
        await sourceStream.CompressToAsync(destinationStream, token);
        return destinationStream.ToArray();
      }
    }
  }

  public static async Task<Byte[]> DecompressAsync(this Byte[] compressedData, CancellationToken token = default)
  {
    using (var sourceStream = new MemoryStream(compressedData))
    {
      using (var destinationStream = new MemoryStream())
      {
        await sourceStream.DecompressToAsync(destinationStream, token);
        return destinationStream.ToArray();
      }
    }
  }

  public static async Task CompressToAsync(this Stream stream, Stream outputStream, CancellationToken token = default)
  {
    using (var gZipStream = new GZipStream(outputStream, CompressionMode.Compress))
      await stream.CopyToAsync(gZipStream, token).ConfigureAwait(false);
  }

  public static async Task DecompressToAsync(this Stream stream, Stream outputStream, CancellationToken token = default)
  {
    using (var gZipStream = new GZipStream(stream, CompressionMode.Decompress))
      await gZipStream.CopyToAsync(outputStream, token).ConfigureAwait(false);
  }
}
