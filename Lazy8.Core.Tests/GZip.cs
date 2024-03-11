/* Unless otherwise noted, this source code is licensed
   under the GNU Public License V3.

   See the LICENSE file in the root folder for details. */

using System;
using System.Text;
using System.Threading.Tasks;

using NUnit.Framework;

namespace Lazy8.Core.Tests;

[TestFixture]
public class GZipTests
{
  private static readonly String _source = "Hello, world! åäö";

  [Test]
  public void Base64RoundTripTest()
  {
    Assert.That(_source.CompressToBase64().DecompressFromBase64() == _source);
  }

  [Test]
  public async Task Base64RoundTripTestAsync()
  {
    var compressedSource = await _source.CompressToBase64Async().ConfigureAwait(false);
    var decompressedSource = await compressedSource.DecompressFromBase64Async().ConfigureAwait(false);
    Assert.That(_source == decompressedSource);
  }

  [Test]
  public void ByteArrayRoundTripTest()
  {
    var compressedSource = Encoding.UTF8.GetBytes(_source).Compress();
    var decompressedSource = Encoding.UTF8.GetString(compressedSource.Decompress());
    Assert.That(_source == decompressedSource);
  }

  [Test]
  public async Task ByteArrayRoundTripTestAsync()
  {
    var compressedSource = await Encoding.UTF8.GetBytes(_source).CompressAsync().ConfigureAwait(false);
    var decompressedSource = Encoding.UTF8.GetString(await compressedSource.DecompressAsync().ConfigureAwait(false));
    Assert.That(_source == decompressedSource);
  }
}

