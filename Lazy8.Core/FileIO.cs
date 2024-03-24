/* Unless otherwise noted, this source code is licensed
   under the GNU Public License V3.

   See the LICENSE file in the root folder for details. */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Lazy8.Core;

public enum DirectoryWalkerErrorHandling { ContinueAndAccumulateExceptions, StopOnFirstException }
[Flags]
public enum FileSystemTypes { Files = 1, Directories = 2, All = Files | Directories }
public enum OverwriteFile { Yes, No }

public static partial class FileUtils
{
  public static readonly Char[] DirectorySeparators = [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar];

  /// <summary>
  /// Same behavior as <see cref="System.IO.Directory.Delete"/>(<paramref name="directory"/>, true), except this method will
  /// also delete read-only files.
  /// </summary>
  /// <param name="directory">A <see cref="String"/> representing an existing directory.</param>
  public static void DeleteDirectory(String directory)
  {
    directory.Name(nameof(directory)).NotNullEmptyOrOnlyWhitespace().DirectoryExists();

    DeleteDirectory(new DirectoryInfo(directory));
  }

  /// <summary>
  /// Same behavior as <see cref="System.IO.Directory.Delete"/>(new DirectoryInfo(<paramref name="directory"/>), true), except this method will
  /// also delete read-only files.
  /// </summary>
  /// <param name="directoryInfo">A <see cref="DirectoryInfo"/> representing an existing directory.</param>
  public static void DeleteDirectory(DirectoryInfo directoryInfo)
  {
    directoryInfo.Name(nameof(directoryInfo)).NotNull().DirectoryExists();

    foreach (var file in directoryInfo.EnumerateFiles("*", SearchOption.TopDirectoryOnly))
    {
      file.Attributes &= ~FileAttributes.ReadOnly;
      file.Delete();
    }

    foreach (var subdirectory in directoryInfo.EnumerateDirectories("*", SearchOption.TopDirectoryOnly))
      DeleteDirectory(subdirectory);

    directoryInfo.Delete(recursive: false);
  }

  /// <summary>
  /// Recursively walk <paramref name="rootDirectory"/>, applying <paramref name="action"/> to every
  /// file and directory. Any exceptions are accumulated and returned by the method in a list.
  /// </summary>
  /// <remarks>
  /// .Net's <see cref="DirectoryInfo.EnumerateFileSystemInfos"/> is a great method for
  /// getting file and directory info.  Unfortunately, that
  /// method can't be used to easily modify those files and directories, much less do multiple
  /// modifying operations on one traversal of the directory tree.
  /// <para>That's what DirectoryWalker is for.  It recurses down the tree
  /// doing a depth-first traversal until it reaches the "leaves" (i.e. directories
  /// that don't contain any subdirectories).  Then, as the recursion unwinds,
  /// the provided action lambda is given a <see cref="FileSystemInfo"/> representing either the
  /// current file or directory that's being enumerated.  Because the lambda is applied during recursion unwinding,
  /// the lambda can do anything it wants with the <see cref="FileSystemInfo"/>, including deleting the file or directory
  /// that's passed to the lambda.
  /// <para>One neat trick this allows, and the reason I wrote this method in the first place,
  /// is that the lambda can delete files, and then delete any directories if they're empty.
  /// Both of those operations occur safely in one traversal of the directory hierarchy.
  /// (See the unit tests in Lazy8.Core.Tests::FileIO.cs for an example of this and
  /// several other uses of DirectoryWalker).</para>
  /// <para>In this overload of the method, the <paramref name="action"/> delegate is applied to
  /// all files and directories under <paramref name="rootDirectory"/>, and any exceptions that occur are stored in the return value of <see cref="List&lt;Exception&gt;"/>.</para>
  /// </remarks>
  /// <param name="rootDirectory">An existing directory from which to start recursing.</param>
  /// <param name="action">A delegate that takes a <see cref="FileSystemInfo"/>.  This delegate will be called for
  /// each file and/or directory (as specified by <paramref name="fileSystemTypes"/>).</param>
  /// <returns>A <see cref="List&lt;Exception&gt;"/> containing any exceptions that were thrown.  This method does not
  /// have to be wrapped in a try/catch block - any exceptions that occur during execution will be contained
  /// in this return value.</returns>
  public static List<Exception> DirectoryWalker(String rootDirectory, Action<FileSystemInfo> action) =>
    DirectoryWalker(rootDirectory, action, FileSystemTypes.All, DirectoryWalkerErrorHandling.ContinueAndAccumulateExceptions);

  /// <summary>
  /// Recursively walk <paramref name="rootDirectory"/>, applying <paramref name="action"/> to every
  /// file and/or directory (as specified by <paramref name="fileSystemTypes"/>).
  /// Any exceptions are accumulated and returned by the method in a list.
  /// </summary>
  /// <remarks>
  /// .Net's <see cref="DirectoryInfo.EnumerateFileSystemInfos"/> is a great method for
  /// getting file and directory info.  Unfortunately, that
  /// method can't be used to easily modify those files and directories, much less do multiple
  /// modifying operations on one traversal of the directory tree.
  /// <para>That's what DirectoryWalker is for.  It recurses down the tree
  /// doing a depth-first traversal until it reaches the "leaves" (i.e. directories
  /// that don't contain any subdirectories).  Then, as the recursion unwinds,
  /// the provided action lambda is given a <see cref="FileSystemInfo"/> representing either the
  /// current file or directory that's being enumerated.  The lambda can do anything
  /// it wants with the <see cref="FileSystemInfo"/>, including deleting the file or directory it represents.
  /// This is a safe operation since it happens on the way back "up" the tree, so having <paramref name="action"/> delete
  /// a directory won't cause an error.</para>
  /// <para>One neat trick this allows, and the reason I wrote this method in the first place,
  /// is that the lambda can delete files, and then delete any directories if they're empty.
  /// Both of those operations occur safely in one traversal of the directory hierarchy.
  /// (See the unit tests in Lazy8.Core.Tests::FileIO.cs for an example of this and
  /// several other uses of DirectoryWalker).</para>
  /// <para>Since this method allows file operations, there's always the chance of an
  /// exception occurring.  Should the method stop on the first exception, or store it
  /// for later perusal and continue?  The answer I decided on is "both".</para>
  /// <para>In this overload of the method, any exceptions that occur are stored in the return value of <see cref="List&lt;Exception&gt;"/>.</para>
  /// </remarks>
  /// <param name="rootDirectory">An existing directory from which to start recursing.</param>
  /// <param name="action">A delegate that takes a <see cref="FileSystemInfo"/>.  This delegate will be called for
  /// each file and/or directory (as specified by <paramref name="fileSystemTypes"/>).</param>
  /// <param name="fileSystemTypes">An enum value that specifies if the <paramref name="action"/> delegate will be
  /// called for files, directories, or both.</param>
  /// <returns>A <see cref="List&lt;Exception&gt;"/> containing any exceptions that were thrown.  This method does not
  /// have to be wrapped in a try/catch block - any exceptions that occur during execution will be contained
  /// in this return value.</returns>
  public static List<Exception> DirectoryWalker(String rootDirectory, Action<FileSystemInfo> action, FileSystemTypes fileSystemTypes) =>
    DirectoryWalker(rootDirectory, action, fileSystemTypes, DirectoryWalkerErrorHandling.ContinueAndAccumulateExceptions);

  /// <summary>
  /// Recursively walk <paramref name="rootDirectory"/>, applying <paramref name="action"/> to every
  /// file and directory. Errors are either accumulated or cause the method to stop on the first error, depending on the setting in
  /// <paramref name="directoryWalkerErrorHandling"/>.
  /// </summary>
  /// <remarks>
  /// .Net's <see cref="DirectoryInfo.EnumerateFileSystemInfos"/> is a great method for
  /// getting file and directory info.  Unfortunately, that
  /// method can't be used to easily modify those files and directories, much less do multiple
  /// modifying operations on one traversal of the directory tree.
  /// <para>That's what DirectoryWalker is for.  It recurses down the tree
  /// doing a depth-first traversal until it reaches the "leaves" (i.e. directories
  /// that don't contain any subdirectories).  Then, as the recursion unwinds,
  /// the provided action lambda is given a <see cref="FileSystemInfo"/> representing either the
  /// current file or directory that's being enumerated.  The lambda can do anything
  /// it wants with the <see cref="FileSystemInfo"/>, including deleting the file or directory it represents.
  /// This is a safe operation since it happens on the way back "up" the tree, so having <paramref name="action"/> delete
  /// a directory won't cause an error.</para>
  /// <para>One neat trick this allows, and the reason I wrote this method in the first place,
  /// is that the lambda can delete files, and then delete any directories if they're empty.
  /// Both of those operations occur safely in one traversal of the directory hierarchy.
  /// (See the unit tests in Lazy8.Core.Tests::FileIO.cs for an example of this and
  /// several other uses of DirectoryWalker).</para>
  /// <para>Since this method allows file operations, there's always the chance of an
  /// exception occurring.  Should the method stop on the first exception, or store it
  /// for later perusal and continue?  The answer I decided on is "both".</para>
  /// <para>The <paramref name="directoryWalkerErrorHandling"/> parameter allows the caller to select what
  /// exception handling behavior DirectoryWalker should exhibit.  The exceptions are
  /// not thrown, so this method doesn't need to be wrapped in a try/catch handler.
  /// Any exceptions that do occur are stored in the return value of <see cref="List&lt;Exception&gt;"/>.</para>
  /// <para>When called with a setting of DirectoryWalkerErrorHandling.Accumulate, DirectoryWalker
  /// will process all files and directories, storing all exception objects in the return value.</para>
  /// </remarks>
  /// <param name="rootDirectory">An existing directory from which to start recursing.</param>
  /// <param name="action">A delegate that takes a <see cref="FileSystemInfo"/>.  This delegate will be called for
  /// each file and/or directory (as specified by <paramref name="fileSystemTypes"/>).</param>
  /// <param name="directoryWalkerErrorHandling">An enum value that specifies if processing should stop on the
  /// first exception that occurs, or if processing should continue regardless of exceptions (if it can).</param>
  /// <returns>A <see cref="List&lt;Exception&gt;"/> containing any exceptions that were thrown.  This method does not
  /// have to be wrapped in a try/catch block - any exceptions that occur during execution will be contained
  /// in this return value.</returns>
  public static List<Exception> DirectoryWalker(String rootDirectory, Action<FileSystemInfo> action, DirectoryWalkerErrorHandling directoryWalkerErrorHandling) =>
    DirectoryWalker(rootDirectory, action, FileSystemTypes.All, directoryWalkerErrorHandling);

  /// <summary>
  /// Recursively walk <paramref name="rootDirectory"/>, applying <paramref name="action"/> to every
  /// file and/or directory (as specified by <paramref name="fileSystemTypes"/>).  Errors are either
  /// accumulated or cause the method to stop on the first error, depending on the setting in
  /// <paramref name="directoryWalkerErrorHandling"/>.
  /// </summary>
  /// <remarks>
  /// .Net's <see cref="DirectoryInfo.EnumerateFileSystemInfos"/> is a great method for
  /// getting file and directory info.  Unfortunately, that
  /// method can't be used to easily modify those files and directories, much less do multiple
  /// modifying operations on one traversal of the directory tree.
  /// <para>That's what DirectoryWalker is for.  It recurses down the tree
  /// doing a depth-first traversal until it reaches the "leaves" (i.e. directories
  /// that don't contain any subdirectories).  Then, as the recursion unwinds,
  /// the provided action lambda is given a <see cref="FileSystemInfo"/> representing either the
  /// current file or directory that's being enumerated.  The lambda can do anything
  /// it wants with the <see cref="FileSystemInfo"/>, including deleting the file or directory it represents.
  /// This is a safe operation since it happens on the way back "up" the tree, so having <paramref name="action"/> delete
  /// a directory won't cause an error.</para>
  /// <para>One neat trick this allows, and the reason I wrote this method in the first place,
  /// is that the lambda can delete files, and then delete any directories if they're empty.
  /// Both of those operations occur safely in one traversal of the directory hierarchy.
  /// (See the unit tests in Lazy8.Core.Tests::FileIO.cs for an example of this and
  /// several other uses of DirectoryWalker).</para>
  /// <para>Since this method allows file operations, there's always the chance of an
  /// exception occurring.  Should the method stop on the first exception, or store it
  /// for later perusal and continue?  The answer I decided on is "both".</para>
  /// <para>The <paramref name="directoryWalkerErrorHandling"/> parameter allows the caller to select what
  /// exception handling behavior DirectoryWalker should exhibit.  The exceptions are
  /// not thrown, so this method doesn't need to be wrapped in a try/catch handler.
  /// Any exceptions that do occur are stored in the return value of <see cref="List&lt;Exception&gt;"/>.</para>
  /// <para>When called with a setting of DirectoryWalkerErrorHandling.Accumulate, DirectoryWalker
  /// will process all files and directories, storing all exception objects in the return value.</para>
  /// </remarks>
  /// <param name="rootDirectory">An existing directory from which to start recursing.</param>
  /// <param name="action">A delegate that takes a <see cref="FileSystemInfo"/>.  This delegate will be called for
  /// each file and/or directory (as specified by <paramref name="fileSystemTypes"/>).</param>
  /// <param name="fileSystemTypes">An enum value that specifies if the <paramref name="action"/> delegate will be
  /// called for files, directories, or both.</param>
  /// <param name="directoryWalkerErrorHandling">An enum value that specifies if processing should stop on the
  /// first exception that occurs, or if processing should continue regardless of exceptions (if it can).</param>
  /// <returns>A <see cref="List&lt;Exception&gt;"/> containing any exceptions that were thrown.  This method does not
  /// have to be wrapped in a try/catch block - any exceptions that occur during execution will be contained
  /// in this return value.</returns>
  public static List<Exception> DirectoryWalker(String rootDirectory, Action<FileSystemInfo> action, FileSystemTypes fileSystemTypes, DirectoryWalkerErrorHandling directoryWalkerErrorHandling)
  {
    rootDirectory.Name(nameof(rootDirectory)).NotNullEmptyOrOnlyWhitespace().DirectoryExists();
    action.Name(nameof(action)).NotNull();

    var exceptions = new List<Exception>();

    void rec(String directory)
    {
      if ((directoryWalkerErrorHandling == DirectoryWalkerErrorHandling.StopOnFirstException) && exceptions.Any())
        return;

      try
      {
        var di = new DirectoryInfo(directory);
        foreach (var fsi in di.EnumerateFileSystemInfos())
        {
          if (fsi is DirectoryInfo)
            rec(fsi.FullName);

          if ((directoryWalkerErrorHandling == DirectoryWalkerErrorHandling.StopOnFirstException) && exceptions.Any())
            return;

          try
          {
            if ((fileSystemTypes.HasFlag(FileSystemTypes.Files) && (fsi is FileInfo)) ||
               ((fileSystemTypes.HasFlag(FileSystemTypes.Directories) && (fsi is DirectoryInfo))))
              action(fsi);
          }
          catch (Exception ex)
          {
            exceptions.Add(ex);
          }
        }
      }
      catch (Exception ex)
      {
        exceptions.Add(ex);
      }
    }

    rec(rootDirectory);

    return exceptions;
  }

  /// <summary>
  /// This method is a more forgiving replacement for <see cref="DirectoryInfo.EnumerateFileSystemInfos"/>.
  /// Instead of throwing an exception and stopping execution on the first error, this method calls the <paramref name="errorHandler"/> delegate
  /// each time an error is encountered.
  /// </summary>
  /// <remarks>
  /// <see cref="DirectoryInfo.EnumerateFileSystemInfos"/> has, in my opinion,
  /// a serious design flaw. When enumerating directories, if one of those
  /// directories cannot be accessed, the method throws an exception and all
  /// enumeration stops.
  /// <para>This method is meant to act as a replacement for <see cref="DirectoryInfo.EnumerateFileSystemInfos"/>.
  /// When an exception occurs in this method, the error handler is called, and processing continues
  /// with the next file or directory.</para>
  /// <para>Like the <see cref="DirectoryWalker"/> method, this method does a depth-first tree
  /// traversal. Unlike <see cref="DirectoryWalker"/>, this method is meant to be used in read-only situations.
  /// <see cref="DirectoryWalker"/> is best used if modification is necessary while traversing
  /// the directory tree.</para>
  /// </remarks>
  /// <param name="directory">A <see cref="String"/> containing a valid path.</param>
  /// <param name="filemask">A <see cref="String"/> containing a file mask.  See the <see cref="DirectoryInfo.EnumerateFileSystemInfos"/> <a href="https://docs.microsoft.com/en-us/dotnet/api/system.io.directoryinfo.enumeratefilesysteminfos">MSDN help entry</a> for more info on file masks.</param>
  /// <param name="searchOption">A <see cref="SearchOption"/> indicating whether to only process the top directory, or recursively enumerate all directories.</param>
  /// <param name="errorHandler">An <see cref="Action"/> that takes a <see cref="String"/> and an <see cref="Exception"/> as parameters
  /// <para>The string is the path that was being processed when the exception was thrown.</para></param>
  /// <returns>An <see cref="IEnumerable&lt;FileSystemInfo&gt;"/> containing the matching <see cref="FileSystemInfo"/>s.</returns>
  public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(String directory, String filemask, SearchOption searchOption, Action<String, Exception>? errorHandler)
  {
    directory.Name(nameof(directory)).NotNullEmptyOrOnlyWhitespace().DirectoryExists();
    filemask.Name(nameof(filemask)).NotNullEmptyOrOnlyWhitespace();
    errorHandler!.Name(nameof(errorHandler)).NotNull();

    /* Yield statements cannot appear anywhere inside of a 'try/catch' statement, or in a 'finally' block.
       (See https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/statements#1315-the-yield-statement).
       That's why the di and diEnumerator variables, and the associated if/then logic, are necessary. */

    DirectoryInfo? di = null;
    try
    {
      di = new DirectoryInfo(directory);
    }
    catch (Exception ex)
    {
      errorHandler?.Invoke(directory, ex);
    }

    if (di is null)
    {
      yield break;
    }
    else
    {
      yield return di;

      IEnumerable<FileSystemInfo>? diEnumerator = null;
      try
      {
        diEnumerator = di.EnumerateFileSystemInfos(filemask);
      }
      catch (Exception ex)
      {
        errorHandler?.Invoke(directory, ex);
      }

      if (diEnumerator is null)
      {
        yield break;
      }
      else
      {
        foreach (var fsi in diEnumerator)
        {
          if ((fsi is DirectoryInfo) && (searchOption == SearchOption.AllDirectories))
          {
            foreach (var fsi2 in EnumerateFileSystemInfos(fsi.FullName, filemask, searchOption, errorHandler))
              yield return fsi2;
          }
          else if (fsi is FileInfo)
          {
            yield return fsi;
          }
        }
      }
    }
  }

  /// <summary>
  /// Only delete the directory if it doesn't contain any files or sub-directories.
  /// <para>If the directory is not empty, this method does nothing.</para>
  /// </summary>
  /// <param name="directoryInfo">A <see cref="DirectoryInfo"/> indicating the directory to delete.</param>
  public static void DeleteIfEmpty(this DirectoryInfo directoryInfo)
  {
    directoryInfo.Name(nameof(directoryInfo)).NotNull().DirectoryExists();

    if (directoryInfo.IsDirectoryEmpty())
      directoryInfo.Delete();
  }

  /// <summary>
  /// Create a new empty file.  If a file with the same name already exists, this method does nothing.
  /// </summary>
  /// <param name="filename">A <see cref="String"/> containing a valid filename.</param>
  public static void SafelyCreateEmptyFile(String filename)
  {
    filename.Name(nameof(filename)).NotNullEmptyOrOnlyWhitespace();

    CreateEmptyFile(filename, OverwriteFile.No);
  }

  /// <summary>
  /// Create a new empty file.  If a file with the same name already exists, this method's behavior depends on the <paramref name="overwrite"/> value..
  /// </summary>
  /// <param name="filename">A <see cref="String"/> containing a valid filename.</param>
  /// <param name="overwrite">An enumeration value specifying if an already existing file with the same name should be overwritten.</param>
  public static void CreateEmptyFile(String filename, OverwriteFile overwrite)
  {
    filename.Name(nameof(filename)).NotNullEmptyOrOnlyWhitespace();

    if ((overwrite == OverwriteFile.Yes) || !File.Exists(filename))
      File.WriteAllText(filename, "");
  }

  /// <summary>
  /// Sets a file's timestamp.
  /// </summary>
  /// <param name="filename">A <see cref="String"/> containing an existing filename.</param>
  public static void Touch(String filename)
  {
    filename.Name(nameof(filename)).NotNullEmptyOrOnlyWhitespace();

    Touch(filename, DateTime.Now);
  }

  /// <summary>
  /// Sets a file's timestamp.
  /// </summary>
  /// <param name="filename">A <see cref="String"/> containing an existing filename.</param>
  /// <param name="timestamp">A <see cref="DateTime"/> value.</param>
  public static void Touch(String filename, DateTime timestamp)
  {
    filename.Name(nameof(filename)).NotNullEmptyOrOnlyWhitespace();

    Touch(new FileInfo(filename), timestamp);
  }

  /// <summary>
  /// Sets a file's timestamp.
  /// </summary>
  /// <param name="fileInfo">A <see cref="FileInfo"/> instance.</param>
  public static void Touch(FileInfo fileInfo)
  {
    fileInfo.Name(nameof(fileInfo)).NotNull().FileExists();

    Touch(fileInfo, DateTime.Now);
  }

  /// <summary>
  /// Sets a file's timestamp.
  /// </summary>
  /// <param name="fileInfo">A <see cref="FileInfo"/> instance.</param>
  /// <param name="timestamp">A <see cref="DateTime"/> value.</param>
  public static void Touch(FileInfo fileInfo, DateTime timestamp)
  {
    fileInfo.Name(nameof(fileInfo)).NotNull().FileExists();

    fileInfo.CreationTime = timestamp;
    fileInfo.LastAccessTime = timestamp;
    fileInfo.LastWriteTime = timestamp;
  }

  /// <summary>
  /// Write the contents of <paramref name="ms"/> to <paramref name="filename"/>. <paramref name="filename"/> is created
  /// if it doesn't already exist, otherwise it is overwritten.
  /// </summary>
  /// <param name="filename">A <see cref="String"/> containing a filename.</param>
  /// <param name="ms">A <see cref="MemoryStream"/> instance.</param>
  public static void WriteMemoryStreamToFile(String filename, MemoryStream ms)
  {
    filename.Name(nameof(filename)).NotNullEmptyOrOnlyWhitespace();
    ms.Name(nameof(ms)).NotNull();

    using (var fs = File.Create(filename))
      ms.WriteTo(fs);
  }

  /// <summary>
  /// Delete each of the files contained in <paramref name="filenames"/>.
  /// </summary>
  /// <param name="filenames">An <see cref="IEnumerable&lt;String&gt;"/> containing zero or more filenames.</param>
  public static void DeleteFiles(IEnumerable<String> filenames)
  {
    filenames.Name(nameof(filenames)).NotNull();

    foreach (var filename in filenames)
      File.Delete(filename);
  }

  /// <summary>
  /// Recursively delete empty subdirectories.  Non-empty subdirectories are not deleted.
  /// </summary>
  /// <param name="directory">An existing directory path.</param>
  public static void DeleteEmptyDirectories(String directory)
  {
    directory.Name(nameof(directory)).NotNullEmptyOrOnlyWhitespace().DirectoryExists();

    DeleteEmptyDirectories(new DirectoryInfo(directory));
  }

  /// <summary>
  /// Recursively delete empty subdirectories.  Non-empty subdirectories are not deleted.
  /// </summary>
  /// <param name="directoryInfo">A <see cref="DirectoryInfo"/> value.</param>
  public static void DeleteEmptyDirectories(DirectoryInfo directoryInfo)
  {
    directoryInfo.Name(nameof(directoryInfo)).NotNull().DirectoryExists();

    foreach (var subDirectory in directoryInfo.EnumerateDirectories())
      DeleteEmptyDirectories(subDirectory);

    if (IsDirectoryEmpty(directoryInfo))
      directoryInfo.Delete(false /* Don't recurse. */);
  }

  /// <summary>
  /// Returns true if the <paramref name="directory"/> contains no files or subdirectories.  False otherwise.
  /// </summary>
  /// <param name="directory">An existing directory path.</param>
  /// <returns>A <see cref="Boolean"/> value.</returns>
  public static Boolean IsDirectoryEmpty(String directory)
  {
    directory.Name(nameof(directory)).NotNullEmptyOrOnlyWhitespace().DirectoryExists();

    return IsDirectoryEmpty(new DirectoryInfo(directory));
  }

  /// <summary>
  /// Returns true if <paramref name="directoryInfo"/> contains no files or subdirectories.  False otherwise.
  /// </summary>
  /// <param name="directoryInfo">A <see cref="DirectoryInfo"/> value.</param>
  /// <returns>A <see cref="Boolean"/> value.</returns>
  public static Boolean IsDirectoryEmpty(this DirectoryInfo directoryInfo)
  {
    directoryInfo.Name(nameof(directoryInfo)).NotNull().DirectoryExists();

    return !directoryInfo.EnumerateFileSystemInfos().Any();
  }

  /// <summary>
  /// Delete all files and folders from directory, while not deleting directory itself.
  /// </summary>
  /// <param name="directory">A valid path.</param>
  public static void EmptyDirectory(String directory)
  {
    directory.Name(nameof(directory)).NotNullEmptyOrOnlyWhitespace().DirectoryExists();

    static void rec(String subDirectory)
    {
      DirectoryInfo dir = new(subDirectory);

      foreach (FileInfo fi in dir.GetFiles())
        fi.Delete();

      foreach (DirectoryInfo di in dir.GetDirectories())
      {
        EmptyDirectory(di.FullName);
        di.Delete();
      }
    }

    rec(directory);
  }

  private static readonly MD5 _md5 = MD5.Create();

  /// <summary>
  /// Return the MD5 checksum for the contents of <paramref name="filename"/>.
  /// </summary>
  /// <param name="filename">An existing filename.</param>
  /// <returns>A <see cref="String"/> containing the MD5 checksum.</returns>
  public static String GetMD5Checksum(String filename)
  {
    filename.Name(nameof(filename)).NotNullEmptyOrOnlyWhitespace().FileExists();

    using (var fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
      return fs.MD5Checksum();
  }

  /// <summary>
  /// Return the MD5 checksum for the contents of <paramref name="stream"/>.
  /// </summary>
  /// <param name="stream">An instance of an object derived from <see cref="Stream"/>.</param>
  /// <returns>A <see cref="String"/> containing the MD5 checksum.</returns>
  public static String MD5Checksum(this Stream stream)
  {
    stream.Name(nameof(stream)).NotNull();

    return stream.MD5ChecksumAsByteArray().Select(c => c.ToString("X2")).Join("");
  }

  /// <summary>
  /// Return the MD5 checksum for the contents of <paramref name="stream"/>.
  /// </summary>
  /// <param name="stream">An instance of an object derived from <see cref="Stream"/>.</param>
  /// <returns>A <see cref="Byte[]"/> containing the MD5 checksum.</returns>
  public static Byte[] MD5ChecksumAsByteArray(this Stream stream)
  {
    stream.Name(nameof(stream)).NotNull();

    return _md5.ComputeHash(stream);
  }

  /// <summary>
  /// Removes the last (rightmost) file extension from <paramref name="filename"/>.
  /// </summary>
  /// <param name="filename">A <see cref="String"/> containing a filename.  The file does not have to exist.</param>
  /// <returns>A <see cref="String"/> containing the filename with the last extension removed.</returns>
  public static String RemoveFileExtension(this String filename)
  {
    filename.Name(nameof(filename)).NotNullEmptyOrOnlyWhitespace();

    return Path.ChangeExtension(filename, null);
  }

  /// <summary>
  /// Returns the path of the currently executing assembly.
  /// </summary>
  /// <returns>A <see cref="String"/> containing the directory of the currently executing assembly.</returns>
  public static String GetExecutablePath() => Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!.AddTrailingSeparator();

  [GeneratedRegex(@"\\+", RegexOptions.Singleline)]
  private static partial Regex MultipleBackslashesRegex();

  /// <summary>
  /// Given a string that contains backslash characters, return the same string but with those backslash characters duplicated.
  /// </summary>
  /// <param name="directory"></param>
  /// <returns>A <see cref="String"/> containing a copy of the modified original string.</returns>
  public static String DuplicateBackslashes(this String directory)
  {
    directory.Name(nameof(directory)).NotNull();

    return MultipleBackslashesRegex().Replace(directory, @"\\");
  }

  /// <summary>
  /// Return a new unique directory path located under the system's temporary folder.  This method does NOT create the directory.
  /// </summary>
  /// <returns>A <see cref="String"/> containing a valid directory path.</returns>
  public static String GetTemporarySubfolder() =>
    Path.Combine(Path.GetTempPath(), Path.ChangeExtension(Path.GetRandomFileName(), null)) + Path.DirectorySeparatorChar;

  /// <summary>
  /// Returns <paramref name="directory"/>, guaranteeing the last character is a platform-specific directory separator.  If <paramref name="directory"/>
  /// already ends with one or more directory separator characters, they are all replaced with one directory separator character.
  /// </summary>
  /// <param name="directory">A non-null <see cref="String"/> representing a directory.  The directory does not have to exist.</param>
  /// <returns>The <paramref name="directory"/> parameter, with one guaranteed trailing directory separator at the end of the string.</returns>
  public static String AddTrailingSeparator(this String directory)
  {
    directory.Name(nameof(directory)).NotNull();

    return String.Concat(directory.RemoveTrailingSeparator(), Path.DirectorySeparatorChar);
  }

  /// <summary>
  /// Returns <paramref name="directory"/> with all of the trailing whitespace and directory separators removed.
  /// </summary>
  /// <param name="directory">A non-null <see cref="String"/> representing a directory.  The directory does not have to exist.</param>
  /// <returns>The <paramref name="directory"/> parameter, with all of the trailing whitespace and directory separators removed.</returns>
  public static String RemoveTrailingSeparator(this String directory)
  {
    directory.Name(nameof(directory)).NotNull();

    /* Windows allows the use of both / and \ as directory separators in pathnames.
       Remove both just to be safe. */
    return
      directory
      .TrimEnd()
      .TrimEnd(DirectorySeparators);
  }

  /// <summary>
  /// Given two filenames (either relative or absolute paths), perform a case-insensitive comparison of the filenames and return
  /// a <see cref="Boolean"/> result.  Relative paths are converted to absolute paths prior to the comparison.
  /// <para>Neither filename has to exist.</para>
  /// </summary>
  /// <param name="filename1">A filename.</param>
  /// <param name="filename2">A filename.</param>
  /// <returns>A <see cref="Boolean"/> indicating whether the absolute filenames are equal.</returns>
  public static Boolean AreFilenamesEqual(String filename1, String filename2)
  {
    filename1.Name(nameof(filename1)).NotNullEmptyOrOnlyWhitespace();
    filename2.Name(nameof(filename2)).NotNullEmptyOrOnlyWhitespace();

    return Path.GetFullPath(filename1).EqualsCI(Path.GetFullPath(filename2));
  }

  /// <summary>
  /// Given two existing filenames, compare the file contents and return a <see cref="Boolean"/>
  /// indicating if the files' contents are equal to each other.
  /// </summary>
  /// <param name="filename1">An existing filename.</param>
  /// <param name="filename2">An existing filename.</param>
  /// <returns>A <see cref="Boolean"/> indicating whether the files' contents are equal to each other.</returns>
  public static Boolean AreFilesEqual(String filename1, String filename2)
  {
    filename1.Name(nameof(filename1)).NotNullEmptyOrOnlyWhitespace().FileExists();
    filename2.Name(nameof(filename2)).NotNullEmptyOrOnlyWhitespace().FileExists();

    if (AreFilenamesEqual(filename1, filename2))
      return true;

    using (FileStream fs1 = new(filename1, FileMode.Open),
                      fs2 = new(filename2, FileMode.Open))
    {
      if (fs1.Length != fs2.Length)
        return false;

      /* .Net oddity:  The ReadByte() method does actually read a byte (8 bits), but casts
         the byte to an Int32 (32 bits) and returns that, instead of just returning
         the byte it read. */
      Int32 fb1, fb2;
      do
      {
        fb1 = fs1.ReadByte();
        fb2 = fs2.ReadByte();
      } while ((fb1 == fb2) && (fb1 != -1));

      return (fb1 == fb2);
    }
  }

  /* These are complementary methods to the BCL System.IO.Directory.Get*, System.IO.Directory.Enumerate*,
     System.IO.DirectoryInfo.Get*, and System.IO.DirectoryInfo.Enumerate* methods.

     The BCL methods take a 'String searchPattern' parameter, whereas the methods below
     take a either a Regex instance, an IEnumerable of Regex instances, or an IEnumerable of search pattern strings.

     The latter two are processed in parallel during the search. */

  private static readonly EnumerationOptions _recursiveEnumerationOptions = new() { MatchType = MatchType.Win32, AttributesToSkip = 0, IgnoreInaccessible = true, RecurseSubdirectories = true };
  private static readonly EnumerationOptions _nonRecursiveEnumerationOptions = new() { MatchType = MatchType.Win32, AttributesToSkip = 0, IgnoreInaccessible = true };

  private static EnumerationOptions GetEnumerationOptions(SearchOption searchOption) =>
    searchOption == SearchOption.AllDirectories
    ? _recursiveEnumerationOptions
    : _nonRecursiveEnumerationOptions;

  /// <summary>
  /// Given an existing directory and regex, enumerate all of directory's subdirectories, applying regex to each subdirectory name.
  /// All matching subdirectory names will be returned in a String array.
  /// </summary>
  /// <param name="directory">A String containing an existing directory.</param>
  /// <param name="regex">A Regex instance.</param>
  /// <returns>A String array containing the paths of all directories that matched the given regex.
  /// <para>Note that directory itself - even if it matches the regex - will not be in the result array.</para></returns>
  public static String[] GetDirectories(String directory, Regex regex)
  {
    directory.Name(nameof(directory)).NotNullEmptyOrOnlyWhitespace().DirectoryExists();
    regex.Name(nameof(regex)).NotNull();

    return GetDirectories(directory, regex, SearchOption.AllDirectories);
  }

  public static String[] GetDirectories(String directory, Regex regex, SearchOption searchOption)
  {
    directory.Name(nameof(directory)).NotNullEmptyOrOnlyWhitespace().DirectoryExists();
    regex.Name(nameof(regex)).NotNull();

    return GetDirectories(directory, regex, GetEnumerationOptions(searchOption));
  }

  public static String[] GetDirectories(String directory, Regex regex, EnumerationOptions enumerationOptions)
  {
    directory.Name(nameof(directory)).NotNullEmptyOrOnlyWhitespace().DirectoryExists();
    regex.Name(nameof(regex)).NotNull();

    return
      Directory
      .GetDirectories(directory, "*", enumerationOptions)
      .Where(p => regex.IsMatch(p))
      .ToArray();
  }

  public static String[] GetDirectories(String directory, IEnumerable<Regex> regexes)
  {
    directory.Name(nameof(directory)).NotNullEmptyOrOnlyWhitespace().DirectoryExists();
    regexes.Name(nameof(regexes)).NotNull().NotEmpty();

    return GetDirectories(directory, regexes, SearchOption.AllDirectories);
  }

  public static String[] GetDirectories(String directory, IEnumerable<Regex> regexes, SearchOption searchOption)
  {
    directory.Name(nameof(directory)).NotNullEmptyOrOnlyWhitespace().DirectoryExists();
    regexes.Name(nameof(regexes)).NotNull().NotEmpty();

    return GetDirectories(directory, regexes, GetEnumerationOptions(searchOption));
  }

  public static String[] GetDirectories(String directory, IEnumerable<Regex> regexes, EnumerationOptions enumerationOptions)
  {
    directory.Name(nameof(directory)).NotNullEmptyOrOnlyWhitespace().DirectoryExists();
    regexes.Name(nameof(regexes)).NotNull().NotEmpty();

    return
      regexes
      .AsParallel()
      .SelectMany(regex => GetDirectories(directory, regex, enumerationOptions))
      .ToArray();
  }

  public static String[] GetDirectories(String directory, IEnumerable<String> searchPatterns)
  {
    directory.Name(nameof(directory)).NotNullEmptyOrOnlyWhitespace().DirectoryExists();
    searchPatterns.Name(nameof(searchPatterns)).NotNull().NotEmpty().NoStringsAreNullOrWhiteSpace();

    return GetDirectories(directory, searchPatterns, SearchOption.AllDirectories);
  }

  public static String[] GetDirectories(String directory, IEnumerable<String> searchPatterns, SearchOption searchOption)
  {
    directory.Name(nameof(directory)).NotNullEmptyOrOnlyWhitespace().DirectoryExists();
    searchPatterns.Name(nameof(searchPatterns)).NotNull().NotEmpty().NoStringsAreNullOrWhiteSpace();

    return GetDirectories(directory, searchPatterns, GetEnumerationOptions(searchOption));
  }

  public static String[] GetDirectories(String directory, IEnumerable<String> searchPatterns, EnumerationOptions enumerationOptions)
  {
    directory.Name(nameof(directory)).NotNullEmptyOrOnlyWhitespace().DirectoryExists();
    searchPatterns.Name(nameof(searchPatterns)).NotNull().NotEmpty().NoStringsAreNullOrWhiteSpace();

    return
      searchPatterns
      .AsParallel()
      .SelectMany(searchPattern => Directory.GetDirectories(directory, searchPattern, enumerationOptions))
      .ToArray();
  }

  public static String[] GetFiles(String directory, Regex regex)
  {
    directory.Name(nameof(directory)).NotNullEmptyOrOnlyWhitespace().DirectoryExists();
    regex.Name(nameof(regex)).NotNull();

    return GetFiles(directory, regex, SearchOption.AllDirectories);
  }

  public static String[] GetFiles(String directory, Regex regex, SearchOption searchOption)
  {
    directory.Name(nameof(directory)).NotNullEmptyOrOnlyWhitespace().DirectoryExists();
    regex.Name(nameof(regex)).NotNull();

    return GetFiles(directory, regex, GetEnumerationOptions(searchOption));
  }

  public static String[] GetFiles(String directory, Regex regex, EnumerationOptions enumerationOptions)
  {
    directory.Name(nameof(directory)).NotNullEmptyOrOnlyWhitespace().DirectoryExists();
    regex.Name(nameof(regex)).NotNull();

    return
      Directory
      .GetFiles(directory, "*", enumerationOptions)
      .Where(p => regex.IsMatch(p))
      .ToArray();
  }

  public static String[] GetFiles(String directory, IEnumerable<Regex> regexes)
  {
    directory.Name(nameof(directory)).NotNullEmptyOrOnlyWhitespace().DirectoryExists();
    regexes.Name(nameof(regexes)).NotNull().NotEmpty();

    return GetFiles(directory, regexes, SearchOption.AllDirectories);
  }

  public static String[] GetFiles(String directory, IEnumerable<Regex> regexes, SearchOption searchOption)
  {
    directory.Name(nameof(directory)).NotNullEmptyOrOnlyWhitespace().DirectoryExists();
    regexes.Name(nameof(regexes)).NotNull().NotEmpty();

    return GetFiles(directory, regexes, GetEnumerationOptions(searchOption));
  }

  public static String[] GetFiles(String directory, IEnumerable<Regex> regexes, EnumerationOptions enumerationOptions)
  {
    directory.Name(nameof(directory)).NotNullEmptyOrOnlyWhitespace().DirectoryExists();
    regexes.Name(nameof(regexes)).NotNull().NotEmpty();

    return
      regexes
      .AsParallel()
      .SelectMany(regex => GetFiles(directory, regex, enumerationOptions))
      .ToArray();
  }

  public static String[] GetFiles(String directory, IEnumerable<String> searchPatterns)
  {
    directory.Name(nameof(directory)).NotNullEmptyOrOnlyWhitespace().DirectoryExists();
    searchPatterns.Name(nameof(searchPatterns)).NotNull().NotEmpty().NoStringsAreNullOrWhiteSpace();

    return GetFiles(directory, searchPatterns, SearchOption.AllDirectories);
  }

  public static String[] GetFiles(String directory, IEnumerable<String> searchPatterns, SearchOption searchOption)
  {
    directory.Name(nameof(directory)).NotNullEmptyOrOnlyWhitespace().DirectoryExists();
    searchPatterns.Name(nameof(searchPatterns)).NotNull().NotEmpty().NoStringsAreNullOrWhiteSpace();

    return GetFiles(directory, searchPatterns, GetEnumerationOptions(searchOption));
  }

  public static String[] GetFiles(String directory, IEnumerable<String> searchPatterns, EnumerationOptions enumerationOptions)
  {
    directory.Name(nameof(directory)).NotNullEmptyOrOnlyWhitespace().DirectoryExists();
    searchPatterns.Name(nameof(searchPatterns)).NotNull().NotEmpty().NoStringsAreNullOrWhiteSpace();

    return
      searchPatterns
      .AsParallel()
      .SelectMany(searchPattern => Directory.GetFiles(directory, searchPattern, enumerationOptions))
      .ToArray();
  }

  public static String[] GetFileSystemEntries(String directory, Regex regex)
  {
    directory.Name(nameof(directory)).NotNullEmptyOrOnlyWhitespace().DirectoryExists();
    regex.Name(nameof(regex)).NotNull();

    return GetFileSystemEntries(directory, regex, SearchOption.AllDirectories);
  }

  public static String[] GetFileSystemEntries(String directory, Regex regex, SearchOption searchOption)
  {
    directory.Name(nameof(directory)).NotNullEmptyOrOnlyWhitespace().DirectoryExists();
    regex.Name(nameof(regex)).NotNull();

    return GetFileSystemEntries(directory, regex, GetEnumerationOptions(searchOption));
  }

  public static String[] GetFileSystemEntries(String directory, Regex regex, EnumerationOptions enumerationOptions)
  {
    directory.Name(nameof(directory)).NotNullEmptyOrOnlyWhitespace().DirectoryExists();
    regex.Name(nameof(regex)).NotNull();

    return
      Directory
      .GetFileSystemEntries(directory, "*", enumerationOptions)
      .Where(p => regex.IsMatch(p))
      .ToArray();
  }

  public static String[] GetFileSystemEntries(String directory, IEnumerable<Regex> regexes)
  {
    directory.Name(nameof(directory)).NotNullEmptyOrOnlyWhitespace().DirectoryExists();
    regexes.Name(nameof(regexes)).NotNull().NotEmpty();

    return GetFileSystemEntries(directory, regexes, SearchOption.AllDirectories);
  }

  public static String[] GetFileSystemEntries(String directory, IEnumerable<Regex> regexes, SearchOption searchOption)
  {
    directory.Name(nameof(directory)).NotNullEmptyOrOnlyWhitespace().DirectoryExists();
    regexes.Name(nameof(regexes)).NotNull().NotEmpty();

    return GetFileSystemEntries(directory, regexes, GetEnumerationOptions(searchOption));
  }

  public static String[] GetFileSystemEntries(String directory, IEnumerable<Regex> regexes, EnumerationOptions enumerationOptions)
  {
    directory.Name(nameof(directory)).NotNullEmptyOrOnlyWhitespace().DirectoryExists();
    regexes.Name(nameof(regexes)).NotNull().NotEmpty();

    return
      regexes
      .AsParallel()
      .SelectMany(regex => GetFileSystemEntries(directory, regex, enumerationOptions))
      .ToArray();
  }

  public static String[] GetFileSystemEntries(String directory, IEnumerable<String> searchPatterns)
  {
    directory.Name(nameof(directory)).NotNullEmptyOrOnlyWhitespace().DirectoryExists();
    searchPatterns.Name(nameof(searchPatterns)).NotNull().NotEmpty().NoStringsAreNullOrWhiteSpace();

    return GetFileSystemEntries(directory, searchPatterns, SearchOption.AllDirectories);
  }

  public static String[] GetFileSystemEntries(String directory, IEnumerable<String> searchPatterns, SearchOption searchOption)
  {
    directory.Name(nameof(directory)).NotNullEmptyOrOnlyWhitespace().DirectoryExists();
    searchPatterns.Name(nameof(searchPatterns)).NotNull().NotEmpty().NoStringsAreNullOrWhiteSpace();

    return GetFileSystemEntries(directory, searchPatterns, GetEnumerationOptions(searchOption));
  }

  public static String[] GetFileSystemEntries(String directory, IEnumerable<String> searchPatterns, EnumerationOptions enumerationOptions)
  {
    directory.Name(nameof(directory)).NotNullEmptyOrOnlyWhitespace().DirectoryExists();
    searchPatterns.Name(nameof(searchPatterns)).NotNull().NotEmpty().NoStringsAreNullOrWhiteSpace();

    return
      searchPatterns
      .AsParallel()
      .SelectMany(searchPattern => Directory.GetFileSystemEntries(directory, searchPattern, enumerationOptions))
      .ToArray();
  }

  //////////////////

  public static IEnumerable<String> EnumerateDirectories(String directory, Regex regex)
  {
    directory.Name(nameof(directory)).NotNullEmptyOrOnlyWhitespace().DirectoryExists();
    regex.Name(nameof(regex)).NotNull();

    return EnumerateDirectories(directory, regex, SearchOption.AllDirectories);
  }

  public static IEnumerable<String> EnumerateDirectories(String directory, Regex regex, SearchOption searchOption)
  {
    directory.Name(nameof(directory)).NotNullEmptyOrOnlyWhitespace().DirectoryExists();
    regex.Name(nameof(regex)).NotNull();

    return EnumerateDirectories(directory, regex, GetEnumerationOptions(searchOption));
  }

  public static IEnumerable<String> EnumerateDirectories(String directory, Regex regex, EnumerationOptions enumerationOptions)
  {
    directory.Name(nameof(directory)).NotNullEmptyOrOnlyWhitespace().DirectoryExists();
    regex.Name(nameof(regex)).NotNull();

    return
      Directory
      .EnumerateDirectories(directory, "*", enumerationOptions)
      .Where(p => regex.IsMatch(p));
  }

  public static IEnumerable<String> EnumerateDirectories(String directory, IEnumerable<Regex> regexes)
  {
    directory.Name(nameof(directory)).NotNullEmptyOrOnlyWhitespace().DirectoryExists();
    regexes.Name(nameof(regexes)).NotNull().NotEmpty();

    return EnumerateDirectories(directory, regexes, SearchOption.AllDirectories);
  }

  public static IEnumerable<String> EnumerateDirectories(String directory, IEnumerable<Regex> regexes, SearchOption searchOption)
  {
    directory.Name(nameof(directory)).NotNullEmptyOrOnlyWhitespace().DirectoryExists();
    regexes.Name(nameof(regexes)).NotNull().NotEmpty();

    return EnumerateDirectories(directory, regexes, GetEnumerationOptions(searchOption));
  }

  public static IEnumerable<String> EnumerateDirectories(String directory, IEnumerable<Regex> regexes, EnumerationOptions enumerationOptions)
  {
    directory.Name(nameof(directory)).NotNullEmptyOrOnlyWhitespace().DirectoryExists();
    regexes.Name(nameof(regexes)).NotNull().NotEmpty();

    return
      regexes
      .AsParallel()
      .SelectMany(regex => EnumerateDirectories(directory, regex, enumerationOptions));
  }

  public static IEnumerable<String> EnumerateDirectories(String directory, IEnumerable<String> searchPatterns)
  {
    directory.Name(nameof(directory)).NotNullEmptyOrOnlyWhitespace().DirectoryExists();
    searchPatterns.Name(nameof(searchPatterns)).NotNull().NotEmpty().NoStringsAreNullOrWhiteSpace();

    return EnumerateDirectories(directory, searchPatterns, SearchOption.AllDirectories);
  }

  public static IEnumerable<String> EnumerateDirectories(String directory, IEnumerable<String> searchPatterns, SearchOption searchOption)
  {
    directory.Name(nameof(directory)).NotNullEmptyOrOnlyWhitespace().DirectoryExists();
    searchPatterns.Name(nameof(searchPatterns)).NotNull().NotEmpty().NoStringsAreNullOrWhiteSpace();

    return EnumerateDirectories(directory, searchPatterns, GetEnumerationOptions(searchOption));
  }

  public static IEnumerable<String> EnumerateDirectories(String directory, IEnumerable<String> searchPatterns, EnumerationOptions enumerationOptions)
  {
    directory.Name(nameof(directory)).NotNullEmptyOrOnlyWhitespace().DirectoryExists();
    searchPatterns.Name(nameof(searchPatterns)).NotNull().NotEmpty().NoStringsAreNullOrWhiteSpace();

    return
      searchPatterns
      .AsParallel()
      .SelectMany(searchPattern => Directory.EnumerateDirectories(directory, searchPattern, enumerationOptions));
  }

  public static IEnumerable<String> EnumerateFiles(String directory, Regex regex)
  {
    directory.Name(nameof(directory)).NotNullEmptyOrOnlyWhitespace().DirectoryExists();
    regex.Name(nameof(regex)).NotNull();

    return EnumerateFiles(directory, regex, SearchOption.AllDirectories);
  }

  public static IEnumerable<String> EnumerateFiles(String directory, Regex regex, SearchOption searchOption)
  {
    directory.Name(nameof(directory)).NotNullEmptyOrOnlyWhitespace().DirectoryExists();
    regex.Name(nameof(regex)).NotNull();

    return EnumerateFiles(directory, regex, GetEnumerationOptions(searchOption));
  }

  public static IEnumerable<String> EnumerateFiles(String directory, Regex regex, EnumerationOptions enumerationOptions)
  {
    directory.Name(nameof(directory)).NotNullEmptyOrOnlyWhitespace().DirectoryExists();
    regex.Name(nameof(regex)).NotNull();

    return
      Directory
      .EnumerateFiles(directory, "*", enumerationOptions)
      .Where(p => regex.IsMatch(p));
  }

  public static IEnumerable<String> EnumerateFiles(String directory, IEnumerable<Regex> regexes)
  {
    directory.Name(nameof(directory)).NotNullEmptyOrOnlyWhitespace().DirectoryExists();
    regexes.Name(nameof(regexes)).NotNull().NotEmpty();

    return EnumerateFiles(directory, regexes, SearchOption.AllDirectories);
  }

  public static IEnumerable<String> EnumerateFiles(String directory, IEnumerable<Regex> regexes, SearchOption searchOption)
  {
    directory.Name(nameof(directory)).NotNullEmptyOrOnlyWhitespace().DirectoryExists();
    regexes.Name(nameof(regexes)).NotNull().NotEmpty();

    return EnumerateFiles(directory, regexes, GetEnumerationOptions(searchOption));
  }

  public static IEnumerable<String> EnumerateFiles(String directory, IEnumerable<Regex> regexes, EnumerationOptions enumerationOptions)
  {
    directory.Name(nameof(directory)).NotNullEmptyOrOnlyWhitespace().DirectoryExists();
    regexes.Name(nameof(regexes)).NotNull().NotEmpty();

    return
      regexes
      .AsParallel()
      .SelectMany(regex => EnumerateFiles(directory, regex, enumerationOptions));
  }

  public static IEnumerable<String> EnumerateFiles(String directory, IEnumerable<String> searchPatterns)
  {
    directory.Name(nameof(directory)).NotNullEmptyOrOnlyWhitespace().DirectoryExists();
    searchPatterns.Name(nameof(searchPatterns)).NotNull().NotEmpty().NoStringsAreNullOrWhiteSpace();

    return EnumerateFiles(directory, searchPatterns, SearchOption.AllDirectories);
  }

  public static IEnumerable<String> EnumerateFiles(String directory, IEnumerable<String> searchPatterns, SearchOption searchOption)
  {
    directory.Name(nameof(directory)).NotNullEmptyOrOnlyWhitespace().DirectoryExists();
    searchPatterns.Name(nameof(searchPatterns)).NotNull().NotEmpty().NoStringsAreNullOrWhiteSpace();

    return EnumerateFiles(directory, searchPatterns, GetEnumerationOptions(searchOption));
  }

  public static IEnumerable<String> EnumerateFiles(String directory, IEnumerable<String> searchPatterns, EnumerationOptions enumerationOptions)
  {
    directory.Name(nameof(directory)).NotNullEmptyOrOnlyWhitespace().DirectoryExists();
    searchPatterns.Name(nameof(searchPatterns)).NotNull().NotEmpty().NoStringsAreNullOrWhiteSpace();

    return
      searchPatterns
      .AsParallel()
      .SelectMany(searchPattern => Directory.EnumerateFiles(directory, searchPattern, enumerationOptions));
  }

  public static IEnumerable<String> EnumerateFileSystemEntries(String directory, Regex regex)
  {
    directory.Name(nameof(directory)).NotNullEmptyOrOnlyWhitespace().DirectoryExists();
    regex.Name(nameof(regex)).NotNull();

    return EnumerateFileSystemEntries(directory, regex, SearchOption.AllDirectories);
  }

  public static IEnumerable<String> EnumerateFileSystemEntries(String directory, Regex regex, SearchOption searchOption)
  {
    directory.Name(nameof(directory)).NotNullEmptyOrOnlyWhitespace().DirectoryExists();
    regex.Name(nameof(regex)).NotNull();

    return EnumerateFileSystemEntries(directory, regex, GetEnumerationOptions(searchOption));
  }

  public static IEnumerable<String> EnumerateFileSystemEntries(String directory, Regex regex, EnumerationOptions enumerationOptions)
  {
    directory.Name(nameof(directory)).NotNullEmptyOrOnlyWhitespace().DirectoryExists();
    regex.Name(nameof(regex)).NotNull();

    return
      Directory
      .EnumerateFileSystemEntries(directory, "*", enumerationOptions)
      .Where(p => regex.IsMatch(p));
  }

  public static IEnumerable<String> EnumerateFileSystemEntries(String directory, IEnumerable<Regex> regexes)
  {
    directory.Name(nameof(directory)).NotNullEmptyOrOnlyWhitespace().DirectoryExists();
    regexes.Name(nameof(regexes)).NotNull().NotEmpty();

    return EnumerateFileSystemEntries(directory, regexes, SearchOption.AllDirectories);
  }

  public static IEnumerable<String> EnumerateFileSystemEntries(String directory, IEnumerable<Regex> regexes, SearchOption searchOption)
  {
    directory.Name(nameof(directory)).NotNullEmptyOrOnlyWhitespace().DirectoryExists();
    regexes.Name(nameof(regexes)).NotNull().NotEmpty();

    return EnumerateFileSystemEntries(directory, regexes, GetEnumerationOptions(searchOption));
  }

  public static IEnumerable<String> EnumerateFileSystemEntries(String directory, IEnumerable<Regex> regexes, EnumerationOptions enumerationOptions)
  {
    directory.Name(nameof(directory)).NotNullEmptyOrOnlyWhitespace().DirectoryExists();
    regexes.Name(nameof(regexes)).NotNull().NotEmpty();

    return
      regexes
      .AsParallel()
      .SelectMany(regex => EnumerateFileSystemEntries(directory, regex, enumerationOptions));
  }

  public static IEnumerable<String> EnumerateFileSystemEntries(String directory, IEnumerable<String> searchPatterns)
  {
    directory.Name(nameof(directory)).NotNullEmptyOrOnlyWhitespace().DirectoryExists();
    searchPatterns.Name(nameof(searchPatterns)).NotNull().NotEmpty().NoStringsAreNullOrWhiteSpace();

    return EnumerateFileSystemEntries(directory, searchPatterns, SearchOption.AllDirectories);
  }

  public static IEnumerable<String> EnumerateFileSystemEntries(String directory, IEnumerable<String> searchPatterns, SearchOption searchOption)
  {
    directory.Name(nameof(directory)).NotNullEmptyOrOnlyWhitespace().DirectoryExists();
    searchPatterns.Name(nameof(searchPatterns)).NotNull().NotEmpty().NoStringsAreNullOrWhiteSpace();

    return EnumerateFileSystemEntries(directory, searchPatterns, GetEnumerationOptions(searchOption));
  }

  public static IEnumerable<String> EnumerateFileSystemEntries(String directory, IEnumerable<String> searchPatterns, EnumerationOptions enumerationOptions)
  {
    directory.Name(nameof(directory)).NotNullEmptyOrOnlyWhitespace().DirectoryExists();
    searchPatterns.Name(nameof(searchPatterns)).NotNull().NotEmpty().NoStringsAreNullOrWhiteSpace();

    return
      searchPatterns
      .AsParallel()
      .SelectMany(searchPattern => Directory.EnumerateFileSystemEntries(directory, searchPattern, enumerationOptions));
  }

  public static DirectoryInfo[] GetDirectories(this DirectoryInfo directoryInfo, Regex regex)
  {
    directoryInfo.Name(nameof(directoryInfo)).NotNull().DirectoryExists();
    regex.Name(nameof(regex)).NotNull();

    return directoryInfo.GetDirectories(regex, SearchOption.AllDirectories);
  }

  public static DirectoryInfo[] GetDirectories(this DirectoryInfo directoryInfo, Regex regex, SearchOption searchOption)
  {
    directoryInfo.Name(nameof(directoryInfo)).NotNull().DirectoryExists();
    regex.Name(nameof(regex)).NotNull();

    return directoryInfo.GetDirectories(regex, GetEnumerationOptions(searchOption));
  }

  public static DirectoryInfo[] GetDirectories(this DirectoryInfo directoryInfo, Regex regex, EnumerationOptions enumerationOptions)
  {
    directoryInfo.Name(nameof(directoryInfo)).NotNull().DirectoryExists();
    regex.Name(nameof(regex)).NotNull();

    return
      directoryInfo
      .GetDirectories("*", enumerationOptions)
      .Where(p => regex.IsMatch(p.FullName))
      .ToArray();
  }

  public static DirectoryInfo[] GetDirectories(this DirectoryInfo directoryInfo, IEnumerable<Regex> regexes)
  {
    directoryInfo.Name(nameof(directoryInfo)).NotNull().DirectoryExists();
    regexes.Name(nameof(regexes)).NotNull().NotEmpty();

    return directoryInfo.GetDirectories(regexes, SearchOption.AllDirectories);
  }

  public static DirectoryInfo[] GetDirectories(this DirectoryInfo directoryInfo, IEnumerable<Regex> regexes, SearchOption searchOption)
  {
    directoryInfo.Name(nameof(directoryInfo)).NotNull().DirectoryExists();
    regexes.Name(nameof(regexes)).NotNull().NotEmpty();

    return directoryInfo.GetDirectories(regexes, GetEnumerationOptions(searchOption));
  }

  public static DirectoryInfo[] GetDirectories(this DirectoryInfo directoryInfo, IEnumerable<Regex> regexes, EnumerationOptions enumerationOptions)
  {
    directoryInfo.Name(nameof(directoryInfo)).NotNull().DirectoryExists();
    regexes.Name(nameof(regexes)).NotNull().NotEmpty();

    return
      regexes
      .AsParallel()
      .SelectMany(regex => directoryInfo.GetDirectories(regex, enumerationOptions))
      .ToArray();
  }

  public static DirectoryInfo[] GetDirectories(this DirectoryInfo directoryInfo, IEnumerable<String> searchPatterns)
  {
    directoryInfo.Name(nameof(directoryInfo)).NotNull().DirectoryExists();
    searchPatterns.Name(nameof(searchPatterns)).NotNull().NotEmpty().NoStringsAreNullOrWhiteSpace();

    return directoryInfo.GetDirectories(searchPatterns, SearchOption.AllDirectories);
  }

  public static DirectoryInfo[] GetDirectories(this DirectoryInfo directoryInfo, IEnumerable<String> searchPatterns, SearchOption searchOption)
  {
    directoryInfo.Name(nameof(directoryInfo)).NotNull().DirectoryExists();
    searchPatterns.Name(nameof(searchPatterns)).NotNull().NotEmpty().NoStringsAreNullOrWhiteSpace();

    return directoryInfo.GetDirectories(searchPatterns, GetEnumerationOptions(searchOption));
  }

  public static DirectoryInfo[] GetDirectories(this DirectoryInfo directoryInfo, IEnumerable<String> searchPatterns, EnumerationOptions enumerationOptions)
  {
    directoryInfo.Name(nameof(directoryInfo)).NotNull().DirectoryExists();
    searchPatterns.Name(nameof(searchPatterns)).NotNull().NotEmpty().NoStringsAreNullOrWhiteSpace();

    return
      searchPatterns
      .AsParallel()
      .SelectMany(searchPattern => directoryInfo.GetDirectories(searchPattern, enumerationOptions))
      .ToArray();
  }

  public static FileInfo[] GetFiles(this DirectoryInfo directoryInfo, Regex regex)
  {
    directoryInfo.Name(nameof(directoryInfo)).NotNull().DirectoryExists();
    regex.Name(nameof(regex)).NotNull();

    return directoryInfo.GetFiles(regex, SearchOption.AllDirectories);
  }

  public static FileInfo[] GetFiles(this DirectoryInfo directoryInfo, Regex regex, SearchOption searchOption)
  {
    directoryInfo.Name(nameof(directoryInfo)).NotNull().DirectoryExists();
    regex.Name(nameof(regex)).NotNull();

    return directoryInfo.GetFiles(regex, GetEnumerationOptions(searchOption));
  }

  public static FileInfo[] GetFiles(this DirectoryInfo directoryInfo, Regex regex, EnumerationOptions enumerationOptions)
  {
    directoryInfo.Name(nameof(directoryInfo)).NotNull().DirectoryExists();
    regex.Name(nameof(regex)).NotNull();

    return
      directoryInfo
      .GetFiles("*", enumerationOptions)
      .Where(p => regex.IsMatch(p.FullName))
      .ToArray();
  }

  public static FileInfo[] GetFiles(this DirectoryInfo directoryInfo, IEnumerable<Regex> regexes)
  {
    directoryInfo.Name(nameof(directoryInfo)).NotNull().DirectoryExists();
    regexes.Name(nameof(regexes)).NotNull().NotEmpty();

    return directoryInfo.GetFiles(regexes, SearchOption.AllDirectories);
  }

  public static FileInfo[] GetFiles(this DirectoryInfo directoryInfo, IEnumerable<Regex> regexes, SearchOption searchOption)
  {
    directoryInfo.Name(nameof(directoryInfo)).NotNull().DirectoryExists();
    regexes.Name(nameof(regexes)).NotNull().NotEmpty();

    return directoryInfo.GetFiles(regexes, GetEnumerationOptions(searchOption));
  }

  public static FileInfo[] GetFiles(this DirectoryInfo directoryInfo, IEnumerable<Regex> regexes, EnumerationOptions enumerationOptions)
  {
    directoryInfo.Name(nameof(directoryInfo)).NotNull().DirectoryExists();
    regexes.Name(nameof(regexes)).NotNull().NotEmpty();

    return
      regexes
      .AsParallel()
      .SelectMany(regex => directoryInfo.GetFiles(regex, enumerationOptions))
      .ToArray();
  }

  public static FileInfo[] GetFiles(this DirectoryInfo directoryInfo, IEnumerable<String> searchPatterns)
  {
    directoryInfo.Name(nameof(directoryInfo)).NotNull().DirectoryExists();
    searchPatterns.Name(nameof(searchPatterns)).NotNull().NotEmpty().NoStringsAreNullOrWhiteSpace();

    return directoryInfo.GetFiles(searchPatterns, SearchOption.AllDirectories);
  }

  public static FileInfo[] GetFiles(this DirectoryInfo directoryInfo, IEnumerable<String> searchPatterns, SearchOption searchOption)
  {
    directoryInfo.Name(nameof(directoryInfo)).NotNull().DirectoryExists();
    searchPatterns.Name(nameof(searchPatterns)).NotNull().NotEmpty().NoStringsAreNullOrWhiteSpace();

    return directoryInfo.GetFiles(searchPatterns, GetEnumerationOptions(searchOption));
  }

  public static FileInfo[] GetFiles(this DirectoryInfo directoryInfo, IEnumerable<String> searchPatterns, EnumerationOptions enumerationOptions)
  {
    directoryInfo.Name(nameof(directoryInfo)).NotNull().DirectoryExists();
    searchPatterns.Name(nameof(searchPatterns)).NotNull().NotEmpty().NoStringsAreNullOrWhiteSpace();

    return
      searchPatterns
      .AsParallel()
      .SelectMany(searchPattern => directoryInfo.GetFiles(searchPattern, enumerationOptions))
      .ToArray();
  }

  public static FileSystemInfo[] GetFileSystemInfos(this DirectoryInfo directoryInfo, Regex regex)
  {
    directoryInfo.Name(nameof(directoryInfo)).NotNull().DirectoryExists();
    regex.Name(nameof(regex)).NotNull();

    return directoryInfo.GetFileSystemInfos(regex, SearchOption.AllDirectories);
  }

  public static FileSystemInfo[] GetFileSystemInfos(this DirectoryInfo directoryInfo, Regex regex, SearchOption searchOption)
  {
    directoryInfo.Name(nameof(directoryInfo)).NotNull().DirectoryExists();
    regex.Name(nameof(regex)).NotNull();

    return directoryInfo.GetFileSystemInfos(regex, GetEnumerationOptions(searchOption));
  }

  public static FileSystemInfo[] GetFileSystemInfos(this DirectoryInfo directoryInfo, Regex regex, EnumerationOptions enumerationOptions)
  {
    directoryInfo.Name(nameof(directoryInfo)).NotNull().DirectoryExists();
    regex.Name(nameof(regex)).NotNull();

    return
      directoryInfo
      .GetFileSystemInfos("*", enumerationOptions)
      .Where(p => regex.IsMatch(p.FullName))
      .ToArray();
  }

  public static FileSystemInfo[] GetFileSystemInfos(this DirectoryInfo directoryInfo, IEnumerable<Regex> regexes)
  {
    directoryInfo.Name(nameof(directoryInfo)).NotNull().DirectoryExists();
    regexes.Name(nameof(regexes)).NotNull().NotEmpty();

    return directoryInfo.GetFileSystemInfos(regexes, SearchOption.AllDirectories);
  }

  public static FileSystemInfo[] GetFileSystemInfos(this DirectoryInfo directoryInfo, IEnumerable<Regex> regexes, SearchOption searchOption)
  {
    directoryInfo.Name(nameof(directoryInfo)).NotNull().DirectoryExists();
    regexes.Name(nameof(regexes)).NotNull().NotEmpty();

    return directoryInfo.GetFileSystemInfos(regexes, GetEnumerationOptions(searchOption));
  }

  public static FileSystemInfo[] GetFileSystemInfos(this DirectoryInfo directoryInfo, IEnumerable<Regex> regexes, EnumerationOptions enumerationOptions)
  {
    directoryInfo.Name(nameof(directoryInfo)).NotNull().DirectoryExists();
    regexes.Name(nameof(regexes)).NotNull().NotEmpty();

    return
      regexes
      .AsParallel()
      .SelectMany(regex => directoryInfo.GetFileSystemInfos(regex, enumerationOptions))
      .ToArray();
  }

  public static FileSystemInfo[] GetFileSystemInfos(this DirectoryInfo directoryInfo, IEnumerable<String> searchPatterns)
  {
    directoryInfo.Name(nameof(directoryInfo)).NotNull().DirectoryExists();
    searchPatterns.Name(nameof(searchPatterns)).NotNull().NotEmpty().NoStringsAreNullOrWhiteSpace();

    return directoryInfo.GetFileSystemInfos(searchPatterns, SearchOption.AllDirectories);
  }

  public static FileSystemInfo[] GetFileSystemInfos(this DirectoryInfo directoryInfo, IEnumerable<String> searchPatterns, SearchOption searchOption)
  {
    directoryInfo.Name(nameof(directoryInfo)).NotNull().DirectoryExists();
    searchPatterns.Name(nameof(searchPatterns)).NotNull().NotEmpty().NoStringsAreNullOrWhiteSpace();

    return directoryInfo.GetFileSystemInfos(searchPatterns, GetEnumerationOptions(searchOption));
  }

  public static FileSystemInfo[] GetFileSystemInfos(this DirectoryInfo directoryInfo, IEnumerable<String> searchPatterns, EnumerationOptions enumerationOptions)
  {
    directoryInfo.Name(nameof(directoryInfo)).NotNull().DirectoryExists();
    searchPatterns.Name(nameof(searchPatterns)).NotNull().NotEmpty().NoStringsAreNullOrWhiteSpace();

    return
      searchPatterns
      .AsParallel()
      .SelectMany(searchPattern => directoryInfo.GetFileSystemInfos(searchPattern, enumerationOptions))
      .ToArray();
  }

  public static IEnumerable<DirectoryInfo> EnumerateDirectories(this DirectoryInfo directoryInfo, Regex regex)
  {
    directoryInfo.Name(nameof(directoryInfo)).NotNull().DirectoryExists();
    regex.Name(nameof(regex)).NotNull();

    return directoryInfo.EnumerateDirectories(regex, SearchOption.AllDirectories);
  }

  public static IEnumerable<DirectoryInfo> EnumerateDirectories(this DirectoryInfo directoryInfo, Regex regex, SearchOption searchOption)
  {
    directoryInfo.Name(nameof(directoryInfo)).NotNull().DirectoryExists();
    regex.Name(nameof(regex)).NotNull();

    return directoryInfo.EnumerateDirectories(regex, GetEnumerationOptions(searchOption));
  }

  public static IEnumerable<DirectoryInfo> EnumerateDirectories(this DirectoryInfo directoryInfo, Regex regex, EnumerationOptions enumerationOptions)
  {
    directoryInfo.Name(nameof(directoryInfo)).NotNull().DirectoryExists();
    regex.Name(nameof(regex)).NotNull();

    return
      directoryInfo
      .EnumerateDirectories("*", enumerationOptions)
      .Where(p => regex.IsMatch(p.FullName))
      .ToArray();
  }

  public static IEnumerable<DirectoryInfo> EnumerateDirectories(this DirectoryInfo directoryInfo, IEnumerable<Regex> regexes)
  {
    directoryInfo.Name(nameof(directoryInfo)).NotNull().DirectoryExists();
    regexes.Name(nameof(regexes)).NotNull().NotEmpty();

    return directoryInfo.EnumerateDirectories(regexes, SearchOption.AllDirectories);
  }

  public static IEnumerable<DirectoryInfo> EnumerateDirectories(this DirectoryInfo directoryInfo, IEnumerable<Regex> regexes, SearchOption searchOption)
  {
    directoryInfo.Name(nameof(directoryInfo)).NotNull().DirectoryExists();
    regexes.Name(nameof(regexes)).NotNull().NotEmpty();

    return directoryInfo.EnumerateDirectories(regexes, GetEnumerationOptions(searchOption));
  }

  public static IEnumerable<DirectoryInfo> EnumerateDirectories(this DirectoryInfo directoryInfo, IEnumerable<Regex> regexes, EnumerationOptions enumerationOptions)
  {
    directoryInfo.Name(nameof(directoryInfo)).NotNull().DirectoryExists();
    regexes.Name(nameof(regexes)).NotNull().NotEmpty();

    return
      regexes
      .AsParallel()
      .SelectMany(regex => directoryInfo.EnumerateDirectories(regex, enumerationOptions))
      .ToArray();
  }

  public static IEnumerable<DirectoryInfo> EnumerateDirectories(this DirectoryInfo directoryInfo, IEnumerable<String> searchPatterns)
  {
    directoryInfo.Name(nameof(directoryInfo)).NotNull().DirectoryExists();
    searchPatterns.Name(nameof(searchPatterns)).NotNull().NotEmpty().NoStringsAreNullOrWhiteSpace();

    return directoryInfo.EnumerateDirectories(searchPatterns, SearchOption.AllDirectories);
  }

  public static IEnumerable<DirectoryInfo> EnumerateDirectories(this DirectoryInfo directoryInfo, IEnumerable<String> searchPatterns, SearchOption searchOption)
  {
    directoryInfo.Name(nameof(directoryInfo)).NotNull().DirectoryExists();
    searchPatterns.Name(nameof(searchPatterns)).NotNull().NotEmpty().NoStringsAreNullOrWhiteSpace();

    return directoryInfo.EnumerateDirectories(searchPatterns, GetEnumerationOptions(searchOption));
  }

  public static IEnumerable<DirectoryInfo> EnumerateDirectories(this DirectoryInfo directoryInfo, IEnumerable<String> searchPatterns, EnumerationOptions enumerationOptions)
  {
    directoryInfo.Name(nameof(directoryInfo)).NotNull().DirectoryExists();
    searchPatterns.Name(nameof(searchPatterns)).NotNull().NotEmpty().NoStringsAreNullOrWhiteSpace();

    return
      searchPatterns
      .AsParallel()
      .SelectMany(searchPattern => directoryInfo.EnumerateDirectories(searchPattern, enumerationOptions))
      .ToArray();
  }

  public static IEnumerable<FileInfo> EnumerateFiles(this DirectoryInfo directoryInfo, Regex regex)
  {
    directoryInfo.Name(nameof(directoryInfo)).NotNull().DirectoryExists();
    regex.Name(nameof(regex)).NotNull();

    return directoryInfo.EnumerateFiles(regex, SearchOption.AllDirectories);
  }

  public static IEnumerable<FileInfo> EnumerateFiles(this DirectoryInfo directoryInfo, Regex regex, SearchOption searchOption)
  {
    directoryInfo.Name(nameof(directoryInfo)).NotNull().DirectoryExists();
    regex.Name(nameof(regex)).NotNull();

    return directoryInfo.EnumerateFiles(regex, GetEnumerationOptions(searchOption));
  }

  public static IEnumerable<FileInfo> EnumerateFiles(this DirectoryInfo directoryInfo, Regex regex, EnumerationOptions enumerationOptions)
  {
    directoryInfo.Name(nameof(directoryInfo)).NotNull().DirectoryExists();
    regex.Name(nameof(regex)).NotNull();

    return
      directoryInfo
      .EnumerateFiles("*", enumerationOptions)
      .Where(p => regex.IsMatch(p.FullName))
      .ToArray();
  }

  public static IEnumerable<FileInfo> EnumerateFiles(this DirectoryInfo directoryInfo, IEnumerable<Regex> regexes)
  {
    directoryInfo.Name(nameof(directoryInfo)).NotNull().DirectoryExists();
    regexes.Name(nameof(regexes)).NotNull().NotEmpty();

    return directoryInfo.EnumerateFiles(regexes, SearchOption.AllDirectories);
  }

  public static IEnumerable<FileInfo> EnumerateFiles(this DirectoryInfo directoryInfo, IEnumerable<Regex> regexes, SearchOption searchOption)
  {
    directoryInfo.Name(nameof(directoryInfo)).NotNull().DirectoryExists();
    regexes.Name(nameof(regexes)).NotNull().NotEmpty();

    return directoryInfo.EnumerateFiles(regexes, GetEnumerationOptions(searchOption));
  }

  public static IEnumerable<FileInfo> EnumerateFiles(this DirectoryInfo directoryInfo, IEnumerable<Regex> regexes, EnumerationOptions enumerationOptions)
  {
    directoryInfo.Name(nameof(directoryInfo)).NotNull().DirectoryExists();
    regexes.Name(nameof(regexes)).NotNull().NotEmpty();

    return
      regexes
      .AsParallel()
      .SelectMany(regex => directoryInfo.EnumerateFiles(regex, enumerationOptions))
      .ToArray();
  }

  public static IEnumerable<FileInfo> EnumerateFiles(this DirectoryInfo directoryInfo, IEnumerable<String> searchPatterns)
  {
    directoryInfo.Name(nameof(directoryInfo)).NotNull().DirectoryExists();
    searchPatterns.Name(nameof(searchPatterns)).NotNull().NotEmpty().NoStringsAreNullOrWhiteSpace();

    return directoryInfo.EnumerateFiles(searchPatterns, SearchOption.AllDirectories);
  }

  public static IEnumerable<FileInfo> EnumerateFiles(this DirectoryInfo directoryInfo, IEnumerable<String> searchPatterns, SearchOption searchOption)
  {
    directoryInfo.Name(nameof(directoryInfo)).NotNull().DirectoryExists();
    searchPatterns.Name(nameof(searchPatterns)).NotNull().NotEmpty().NoStringsAreNullOrWhiteSpace();

    return directoryInfo.EnumerateFiles(searchPatterns, GetEnumerationOptions(searchOption));
  }

  public static IEnumerable<FileInfo> EnumerateFiles(this DirectoryInfo directoryInfo, IEnumerable<String> searchPatterns, EnumerationOptions enumerationOptions)
  {
    directoryInfo.Name(nameof(directoryInfo)).NotNull().DirectoryExists();
    searchPatterns.Name(nameof(searchPatterns)).NotNull().NotEmpty().NoStringsAreNullOrWhiteSpace();

    return
      searchPatterns
      .AsParallel()
      .SelectMany(searchPattern => directoryInfo.EnumerateFiles(searchPattern, enumerationOptions))
      .ToArray();
  }

  public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(this DirectoryInfo directoryInfo, Regex regex)
  {
    directoryInfo.Name(nameof(directoryInfo)).NotNull().DirectoryExists();
    regex.Name(nameof(regex)).NotNull();

    return directoryInfo.EnumerateFileSystemInfos(regex, SearchOption.AllDirectories);
  }

  public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(this DirectoryInfo directoryInfo, Regex regex, SearchOption searchOption)
  {
    directoryInfo.Name(nameof(directoryInfo)).NotNull().DirectoryExists();
    regex.Name(nameof(regex)).NotNull();

    return directoryInfo.EnumerateFileSystemInfos(regex, GetEnumerationOptions(searchOption));
  }

  public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(this DirectoryInfo directoryInfo, Regex regex, EnumerationOptions enumerationOptions)
  {
    directoryInfo.Name(nameof(directoryInfo)).NotNull().DirectoryExists();
    regex.Name(nameof(regex)).NotNull();

    return
      directoryInfo
      .EnumerateFileSystemInfos("*", enumerationOptions)
      .Where(p => regex.IsMatch(p.FullName))
      .ToArray();
  }

  public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(this DirectoryInfo directoryInfo, IEnumerable<Regex> regexes)
  {
    directoryInfo.Name(nameof(directoryInfo)).NotNull().DirectoryExists();
    regexes.Name(nameof(regexes)).NotNull().NotEmpty();

    return directoryInfo.EnumerateFileSystemInfos(regexes, SearchOption.AllDirectories);
  }

  public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(this DirectoryInfo directoryInfo, IEnumerable<Regex> regexes, SearchOption searchOption)
  {
    directoryInfo.Name(nameof(directoryInfo)).NotNull().DirectoryExists();
    regexes.Name(nameof(regexes)).NotNull().NotEmpty();

    return directoryInfo.EnumerateFileSystemInfos(regexes, GetEnumerationOptions(searchOption));
  }

  public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(this DirectoryInfo directoryInfo, IEnumerable<Regex> regexes, EnumerationOptions enumerationOptions)
  {
    directoryInfo.Name(nameof(directoryInfo)).NotNull().DirectoryExists();
    regexes.Name(nameof(regexes)).NotNull().NotEmpty();

    return
      regexes
      .AsParallel()
      .SelectMany(regex => directoryInfo.EnumerateFileSystemInfos(regex, enumerationOptions))
      .ToArray();
  }

  public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(this DirectoryInfo directoryInfo, IEnumerable<String> searchPatterns)
  {
    directoryInfo.Name(nameof(directoryInfo)).NotNull().DirectoryExists();
    searchPatterns.Name(nameof(searchPatterns)).NotNull().NotEmpty().NoStringsAreNullOrWhiteSpace();

    return directoryInfo.EnumerateFileSystemInfos(searchPatterns, SearchOption.AllDirectories);
  }

  public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(this DirectoryInfo directoryInfo, IEnumerable<String> searchPatterns, SearchOption searchOption)
  {
    directoryInfo.Name(nameof(directoryInfo)).NotNull().DirectoryExists();
    searchPatterns.Name(nameof(searchPatterns)).NotNull().NotEmpty().NoStringsAreNullOrWhiteSpace();

    return directoryInfo.EnumerateFileSystemInfos(searchPatterns, GetEnumerationOptions(searchOption));
  }

  public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(this DirectoryInfo directoryInfo, IEnumerable<String> searchPatterns, EnumerationOptions enumerationOptions)
  {
    directoryInfo.Name(nameof(directoryInfo)).NotNull().DirectoryExists();
    searchPatterns.Name(nameof(searchPatterns)).NotNull().NotEmpty().NoStringsAreNullOrWhiteSpace();

    return
      searchPatterns
      .AsParallel()
      .SelectMany(searchPattern => directoryInfo.EnumerateFileSystemInfos(searchPattern, enumerationOptions))
      .ToArray();
  }
}

