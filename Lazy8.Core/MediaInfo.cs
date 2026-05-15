/* Unless otherwise noted, this source code is licensed
   under the GNU Public License V3.

   See the LICENSE file in the root folder for details. */

using System;
using System.Linq;
using System.Text.RegularExpressions;

using Newtonsoft.Json.Linq;

namespace Lazy8.Core;

public enum ScanType { Unknown, Progressive, Interlaced, TwoThreePulldown }

/* Names and values correspond to FFmpeg's bwdif filter's 'parity' parameter names and values:

     https://ffmpeg.org/ffmpeg-filters.html#bwdif-1 */
public enum ScanOrder { auto = -1, tff = 0, bff = 1 }

public partial class MediaInfo
{
  public String AudioSampleRate { get; init; }
  public String AudioDuration { get; init; }
  public String AudioCodecName { get; init; }
  public String AudioTempo => (Convert.ToDouble(this.VideoDuration) / Convert.ToDouble(this.AudioDuration)).ToString();
  public Boolean IsMono { get; init; }
  public Boolean IsPcm => this.AudioCodecName.StartsWithCI("pcm");

  public String VideoDuration { get; init; }
  public String VideoCodecName { get; init; }
  public String VideoFormatName { get; init; }
  public Boolean IsWmv => this.VideoCodecName.ContainsCI("wmv") || this.VideoFormatName.ContainsCI("wmv");

  public Int32 Width { get; init; }
  public Int32 Height { get; init; }
  public Double PixelAspectRatio { get; init; }
  public Int32 Area => this.Width * this.Height;
  public String AreaAsString => $"{this.Width} x {this.Height}";

  public ScanType ScanType { get; init; }
  public ScanOrder ScanOrder { get; init; }

  /* FFProbe and MediaInfo can differ in what they think the correct
     framerate is.  For example, sometimes MediaInfo will report the
     framerate as 30.0000, but FFProbe shows a framerate numerator/denominator
     of 30000/1001, which is 29.970.  Add the fact that FFProbe always reports these values,
     but MediaInfo sometimes does not.  Therefore, this class reports both the
     ReportedFrameRate (from MediaInfo), and CalculatedFramerate
     (FramerateNumerator / FramerateDenominator from FFProbe). */

  public Int32 FramerateNumerator { get; private set; }
  public Int32 FramerateDenominator { get; private set; }
  public Double ReportedFramerate { get; init; }
  public Double CalculatedFramerate => (this.FramerateNumerator * 1.0) / this.FramerateDenominator;

  private readonly String _ffprobe;
  private readonly String _mediainfo;

  private MediaInfo() : base() { }

  public MediaInfo(String filename)
    : this()
  {
    this._ffprobe = FileUtils.Where("ffprobe") ?? throw new Exception("ffprobe not found.");
    this.SetFrameratePropertiesFromFFProbe(filename);

    this._mediainfo = FileUtils.Where("mediainfo") ?? throw new Exception("mediainfo not found.");
    var mediainfoParameters = $"--output=JSON {filename.DoubleQuote()}";
    var mediainfoProcessOutput = GeneralUtils.RunProcess(this._mediainfo, mediainfoParameters);

    var mediainfoJson = JObject.Parse(mediainfoProcessOutput.StdOutput);
    var mediainfoTracks = (JArray) mediainfoJson["media"]["track"];

    JToken getTrack(String trackType) => mediainfoTracks.FirstOrDefault(s => s["@type"].Value<String>() == trackType);

    String getDuration(JToken stream)
    {
      if (stream["Duration"] is not null)
        return stream["Duration"].Value<String>();
      else
        return null;
    }

    String getAudioCodecName(JToken audioStream)
    {
      if (audioStream["Encoded_Library"] is not null)
      {
        var parts = audioStream["Encoded_Library"].Value<String>().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return
          (parts.Length == 1)
          ? parts[0].Trim()  /* Contains something like "LAME3.99.5UUUU" */
          : parts[1].Trim(); /* Contains something like "Lavc62.11.100 pcm_s16le" */
      }
      else if (audioStream["Format"] is not null)
      {
        if (audioStream["Format"].Value<String>().ToUpper() == "PCM")
          return "pcm_s16le";
        else
          return audioStream["Format"].Value<String>();
      }
      
      throw new Exception("Could not find audio codec name.");
    }

    String getVideoFormatName(JToken videoStream)
    {
      return
        (videoStream["Format"] is null)
        ? ""
        : videoStream["Format"].Value<String>();
    }

    var generalStream = getTrack("General") ?? throw new Exception("General track not found.");
    var videoStream = getTrack("Video") ?? throw new Exception("Video track not found.");
    var audioStream = getTrack("Audio") ?? throw new Exception("Audio track not found.");

    this.AudioSampleRate = audioStream["SamplingRate"].Value<String>();
    this.VideoDuration = getDuration(videoStream) ?? getDuration(generalStream) ?? throw new Exception($"Could not find duration for video stream.");
    this.AudioDuration = getDuration(audioStream) ?? getDuration(generalStream) ?? throw new Exception($"Could not find duration for audio stream.");
    this.AudioCodecName = getAudioCodecName(audioStream);
    this.IsMono = audioStream["Channels"].Value<String>() == "1";
    this.Width = videoStream["Width"].Value<Int32>();
    this.Height = videoStream["Height"].Value<Int32>();
    this.PixelAspectRatio = videoStream["PixelAspectRatio"].Value<Double>();
    this.VideoCodecName = videoStream["CodecID"].Value<String>();
    this.VideoFormatName = getVideoFormatName(videoStream);

    var frameRate = videoStream["FrameRate"] ?? videoStream["FrameRate_Nominal"] ?? videoStream["FrameRate_Original"];
    this.ReportedFramerate = (frameRate is null) ? this.CalculatedFramerate : frameRate.Value<Double>();

    var scanType = videoStream["ScanType"];
    if (scanType is null)
      this.ScanType = ScanType.Unknown;
    else if (scanType.Value<String>() == "Progressive")
      this.ScanType = ScanType.Progressive;
    else if (scanType.Value<String>() == "Interlaced")
      this.ScanType = ScanType.Interlaced;
    else if (scanType.Value<String>() == "2:3 Pulldown")
      this.ScanType = ScanType.TwoThreePulldown;
    else
      this.ScanType = ScanType.Unknown;

    var scanOrder = videoStream["ScanOrder"];
    if (scanOrder is null)
      this.ScanOrder = ScanOrder.auto;
    else if (scanOrder.Value<String>() == "TFF")
      this.ScanOrder = ScanOrder.tff;
    else if (scanOrder.Value<String>() == "BFF")
      this.ScanOrder = ScanOrder.bff;
    else
      this.ScanOrder = ScanOrder.auto;
  }

  [GeneratedRegex(@"r_frame_rate=(?<framerateNumerator>\d+)\/(?<framerateDenominator>\d+)")]
  private static partial Regex FramerateRegex();

  private void SetFrameratePropertiesFromFFProbe(String filename)
  {
    var ffprobeParameters = $"-v error -select_streams v:0 -show_entries stream=r_frame_rate -of default=nw=1 {filename.DoubleQuote()}";
    var ffprobeProcessOutput = GeneralUtils.RunProcess(this._ffprobe, ffprobeParameters);

    var match = FramerateRegex().Match(ffprobeProcessOutput.StdOutput);
    if (!match.Success)
      throw new Exception($"Could not find r_frame_rate values in ffprobe standard output '{ffprobeProcessOutput.StdOutput}'.");

    this.FramerateNumerator = Convert.ToInt32(match.Groups["framerateNumerator"].Value);
    this.FramerateDenominator = Convert.ToInt32(match.Groups["framerateDenominator"].Value);
  }
}

