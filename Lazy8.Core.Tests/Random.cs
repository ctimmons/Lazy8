/* Unless otherwise noted, this source code is licensed
   under the GNU Public License V3.

   See the LICENSE file in the root folder for details. */

using System;

using NUnit.Framework;

namespace Lazy8.Core.Tests
{
  [TestFixture]
  public class RandomUtilsTests
  {
    [Test]
    public void GetCoinFlipTest()
    {
      // Flip a coin TEST_ITERATION times.  On balance, it should return
      // true ("heads") %50 of the time, + or - %MARGIN_OF_ERROR.

      const Int32 TEST_ITERATIONS = 100000;
      const Double MARGIN_OF_ERROR = 0.01d; // One percent.
      const Double LOWER_ACCEPTABLE_BOUND = 0.5d - MARGIN_OF_ERROR;
      const Double UPPER_ACCEPTABLE_BOUND = 0.5d + MARGIN_OF_ERROR;

      var numberOfHeads = 0;

      for (Int32 i = 1; i <= TEST_ITERATIONS; i++)
        numberOfHeads += (RandomUtils.GetCoinFlip() ? 1 : 0);

      var percentOfHeadsFlips = numberOfHeads / (Double) TEST_ITERATIONS;

      var success = ((LOWER_ACCEPTABLE_BOUND < percentOfHeadsFlips) && (percentOfHeadsFlips < UPPER_ACCEPTABLE_BOUND));

      Assert.That(
        success,
        Is.True,
        $"{numberOfHeads} out of {TEST_ITERATIONS} coin flips ({percentOfHeadsFlips * 100}%) returned true.  Target was 50% (+/- {MARGIN_OF_ERROR * 100}%).");
    }

    [Test]
    public void GetProbabilityCoinFlipTest()
    {
      // Flip a coin TEST_ITERATION times.  On balance, it should return
      // true ("heads") %PROBABILITY of the time, + or - %MARGIN_OF_ERROR.

      const Int32 TEST_ITERATIONS = 100000;
      const Double MARGIN_OF_ERROR = 0.01d; // One percent.
      const Double PROBABILITY = 0.38d; // # between 0 and 1 exclusive.
      const Double LOWER_ACCEPTABLE_BOUND = PROBABILITY - MARGIN_OF_ERROR;
      const Double UPPER_ACCEPTABLE_BOUND = PROBABILITY + MARGIN_OF_ERROR;

      var numberOfHeadsFlips = 0;

      // Count the number of "heads" results.
      for (Int32 i = 1; i <= TEST_ITERATIONS; i++)
        numberOfHeadsFlips += (RandomUtils.GetCoinFlip((Double) PROBABILITY) ? 1 : 0);

      var percentOfHeadsFlips = (Double) numberOfHeadsFlips / (Double) TEST_ITERATIONS;

      var success = (
        (LOWER_ACCEPTABLE_BOUND < percentOfHeadsFlips) &&
        (percentOfHeadsFlips < UPPER_ACCEPTABLE_BOUND));

      Assert.That(
        success,
        Is.True,
        $"{numberOfHeadsFlips} out of {TEST_ITERATIONS} probability coin flips ({percentOfHeadsFlips * 100} percent) returned true.  Target was {PROBABILITY * 100}% (+/- {MARGIN_OF_ERROR * 100}%).");
    }

    [Test]
    public void GetDoubleInRangeTest()
    {
      const Int32 TEST_ITERATIONS = 100000;
      const Double LOWER_BOUND = 5862.0;
      const Double UPPER_BOUND = 5962.0;

      var number = 0.0;
      var noNumbersOutsideOfRangeReturned = true;
      for (Int32 i = 1; i <= TEST_ITERATIONS; i++)
      {
        number = RandomUtils.GetRandomDouble(LOWER_BOUND, UPPER_BOUND);

        if ((number < LOWER_BOUND) || (number > UPPER_BOUND))
        {
          noNumbersOutsideOfRangeReturned = false;
          break;
        }
      }

      Assert.That(
        noNumbersOutsideOfRangeReturned,
        Is.True,
        $"Number = {number}. Lower bound = {LOWER_BOUND}, and upper bound = {UPPER_BOUND}.");
    }

    [Test]
    public void GetStringInRangeTest()
    {
      const Int32 RESULT_LENGTH = 100;
      const Char LO_LOWER_CHAR = 'a';
      const Char HI_LOWER_CHAR = 'n';
      const Char LO_UPPER_CHAR = 'A';
      const Char HI_UPPER_CHAR = 'N';

      // Test all lowercase result.
      String result = RandomUtils.GetRandomString(LO_LOWER_CHAR, HI_LOWER_CHAR,
        RESULT_LENGTH, LetterCaseMix.AllLowerCase);

      Assert.That(
        result.Length == RESULT_LENGTH,
        Is.True,
        $"All Lowercase Test: Incorrect String length. Expected {RESULT_LENGTH} characters, but received {result.Length} characters.");

      var success = true;
      var offendingChar = '+';
      for (Int32 i = 0; i < RESULT_LENGTH; i++)
      {
        // Check each character to make it falls w/i the specified range.
        if ((result[i] < LO_LOWER_CHAR) || (result[i] > HI_LOWER_CHAR))
        {
          success = false;
          offendingChar = result[i];
          break;
        }
      }

      Assert.That(
        success,
        Is.True,
        $"All Lowercase Test: Character not in range. The character '{offendingChar}' is not within the specified range of '{LO_LOWER_CHAR}' and '{HI_LOWER_CHAR}'.");

      // Test all uppercase result.
      result = RandomUtils.GetRandomString(LO_UPPER_CHAR, HI_UPPER_CHAR, RESULT_LENGTH, LetterCaseMix.AllUpperCase);

      Assert.That(
        result.Length == RESULT_LENGTH,
        Is.True,
        $"All Uppercase Test: Incorrect String length. Expected {RESULT_LENGTH} characters, but received {result.Length} characters.");

      success = true;
      offendingChar = '+';
      for (Int32 i = 0; i < RESULT_LENGTH; i++)
      {
        // Check each character to make it falls w/i the specified range.
        if ((result[i] < LO_UPPER_CHAR) || (result[i] > HI_UPPER_CHAR))
        {
          success = false;
          offendingChar = result[i];
          break;
        }
      }

      Assert.That(
        success,
        Is.True,
        $"All Uppercase Test: Character not in range. The character '{offendingChar}' is not within the specified range of '{LO_UPPER_CHAR}' and '{HI_UPPER_CHAR}'.");

      // Test mixed-case result.
      result = RandomUtils.GetRandomString(LO_UPPER_CHAR, HI_UPPER_CHAR,
        RESULT_LENGTH, LetterCaseMix.MixUpperCaseAndLowerCase);

      Assert.That(
        result.Length == RESULT_LENGTH,
        Is.True,
        $"Mixed-case Test: Incorrect String length. Expected {RESULT_LENGTH} characters, but received {result.Length} characters.");

      success = true;
      offendingChar = '+';
      for (Int32 i = 0; i < RESULT_LENGTH; i++)
      {
        // Check each character to make it falls w/i the specified range.
        if ((Char.ToUpper(result[i]) < LO_UPPER_CHAR) || (Char.ToUpper(result[i]) > HI_UPPER_CHAR))
        {
          success = false;
          offendingChar = result[i];
          break;
        }
      }

      Assert.That(
        success,
        Is.True,
        $"Mixed-case Test: Character not in range. The character '{offendingChar}' is not within the specified range of '{LO_UPPER_CHAR}' and '{HI_UPPER_CHAR}'.");
    }
  }
}
