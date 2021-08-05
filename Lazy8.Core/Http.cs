﻿/* Unless otherwise noted, this source code is licensed
   under the GNU Public License V3.

   See the LICENSE file in the root folder for details. */

using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Lazy8.Core
{
  public class Http
  {
    public static readonly HttpClient HttpClientInstance = new();

    /* Note that a "Content-Disposition" header containing a file name will have a value like this:

         attachment; filename=qwerty.xml
    
    */

    private static readonly Regex _filenameRegex = new(@"attachment;\s+filename=(?<filename>.*$)");

    private static String GetFilenameFromHttpResponseMessage(HttpResponseMessage responseMessage)
    {
      if (!responseMessage.Content.Headers.TryGetValues("Content-Disposition", out var values))
        return null;

      foreach (var value in values)
      {
        var match = _filenameRegex.Match(value.Trim());
        if (match.Success)
        {
          var filename = match.Groups["filename"].Value.Trim();
          if (filename.Any())
            return filename;
        }
      }

      return null;
    }

    private static String GetFilenameFromUri(Uri uri) => uri.Segments.Last();

    public static async Task DownloadFileAsync(String sourceUrl, String destinationFolder, String destinationFilename = null) =>
      await DownloadFileAsync(new Uri(sourceUrl), destinationFolder, destinationFilename);

    public static async Task DownloadFileAsync(Uri uri, String destinationFolder, String destinationFilename = null)
    {
      using (var responseMessage = await HttpClientInstance.GetAsync(uri))
      {
        responseMessage.EnsureSuccessStatusCode();

        destinationFilename ??= GetFilenameFromHttpResponseMessage(responseMessage) ?? GetFilenameFromUri(uri);

        using (var destinationStream = File.OpenWrite(Path.Combine(destinationFolder, destinationFilename)))
          await (await responseMessage.Content.ReadAsStreamAsync()).CopyToAsync(destinationStream);
      }
    }
  }
}