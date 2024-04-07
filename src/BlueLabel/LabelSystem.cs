using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace BlueLabel;

/// <summary>
///     The Label class, used to sort files inside a folder.
/// </summary>
public class Label
{
    /// <summary>
    ///     Creates an empty Label object.
    /// </summary>
    public Label()
    {
    }

    /// <summary>
    ///     Creates a label object with a name.
    /// </summary>
    /// <param name="name">Name of the label.</param>
    /// <exception cref="ArgumentNullException">Exception thrown when name is null.</exception>
    public Label(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    /// <summary>
    ///     Name of the label.
    /// </summary>
    public string Name { get; set; } = string.Empty;
}

/// <summary>
///     File class.
/// </summary>
public class LabelFile
{
    private string originalName = string.Empty;

    /// <summary>
    ///     Original location of the file.
    /// </summary>
    public string OriginalPath { get; set; } = string.Empty;

    /// <summary>
    ///     Gets the appropriate file type from the extension.
    /// </summary>
    public FileType Type
    {
        get
        {
            switch (FileExtension.ToLowerInvariant())
            {
                // Audio files (that are supported by LibVLCSharp):
                case ".mp1":
                case ".mp2":
                case ".mp3":
                case ".aac":
                case ".ogg":
                case ".ac3":
                case ".eac3":
                case ".dts":
                case ".wma":
                case ".flac":
                case ".m4a":
                case ".spx":
                case ".mpc":
                case ".aa3":
                case ".wv":
                case ".mod":
                case ".tta":
                case ".ape":
                case ".ra":
                // ReSharper disable once StringLiteralTypo
                case ".alaw":
                // ReSharper disable once StringLiteralTypo
                case ".ulaw":
                case ".amr":
                case ".mid":
                case ".pcm":
                // ReSharper disable once StringLiteralTypo
                case ".adpcm":
                // ReSharper disable once StringLiteralTypo
                case ".qcelp":
                case ".dvf":
                case ".qdm":
                    return FileType.Audio;

                // Archive files (that are supported by SharpZipLib):
                case ".zip":
                case ".bz2":
                case ".gz":
                case ".tgz":
                case ".tar.gz":
                    return FileType.Archive;

                // Video files (that are supported by libVLCSharp):
                case ".mpg":
                case ".mpeg":
                case ".avi":
                // ReSharper disable once StringLiteralTypo
                case ".divx":
                case ".mkv":
                case ".mp4":
                case ".flv":
                // ReSharper disable once StringLiteralTypo
                case ".xvid":
                case ".3ivx":
                case ".d4":
                case ".h261":
                case ".h263":
                case ".h264":
                case ".ogv":
                case ".mjpeg":
                case ".wmv":
                case ".asf":
                case ".dv":
                case ".rm":
                case ".rv":
                    return FileType.Video;

                // Text files (plain text files or source codes from different programming languages that store them as text files):
                case ".txt":
                case ".md":
                case ".xml":
                case ".js":
                case ".html":
                case ".css":
                case ".py":
                case ".php":
                case ".sh":
                case ".bat":
                case ".sql":
                case ".json":
                case ".java":
                case ".c":
                case ".h":
                case ".cpp":
                case ".cs":
                case ".go":
                case ".r":
                case ".ui":
                case ".glade":
                // ReSharper disable once StringLiteralTypo
                case ".axaml":
                // ReSharper disable once StringLiteralTypo
                case ".axaml.cs":
                case ".xaml":
                case ".xaml.cs":
                    return FileType.Text;


                // Image files (that are supported by Avalonia):
                case ".png":
                case ".jpg":
                case ".jpeg":
                case ".gif":
                case ".bmp":
                case ".tiff":
                    return FileType.Image;

                // Unknown
                default:
                    return FileType.Unsupported;
            }
        }
    }

    /// <summary>
    ///     Labels of the file. Determined by the user or automated.
    /// </summary>
    public List<Label> Labels { get; set; } = new();

    /// <summary>
    ///     Gets the file extension from original path.
    /// </summary>
    public string FileExtension =>
        Path.GetFileName(OriginalPath).Remove(0, Path.GetFileNameWithoutExtension(OriginalPath).Length);

    /// <summary>
    ///     Index of the file in the entire folder.
    /// </summary>
    public int ID { get; set; }

    /// <summary>
    ///     Gets the original file name (without the full path and extension).
    /// </summary>
    public string OriginalFileName
    {
        get
        {
            if (string.IsNullOrWhiteSpace(originalName)) originalName = Path.GetFileNameWithoutExtension(OriginalPath);

            return originalName;
        }
    }

    /// <summary>
    ///     Determines the target file. Null if not determined yet.
    /// </summary>
    public string? FinalTargetFile { get; set; }

    /// <summary>
    ///     Gets the path to the output file.
    /// </summary>
    /// <param name="setting">Setting to use.</param>
    /// <returns>A string containing the full path of output file.</returns>
    public string TargetFile(LabelerSetting setting)
    {
        if (setting.LabelFilesBy == LabelFilesOptions.Subfolder)
        {
            var labels = Labels[0].Name;
            if (setting.AllowRecursiveSubfolders)
            {
                labels = string.Empty;
                foreach (var label in Labels) labels += label.Name + Path.PathSeparator;
            }

            FinalTargetFile = Path.Combine(setting.SortInInputFolder ? setting.InputFolder : setting.OutputFolder,
                labels, OriginalFileName + FileExtension);

            return FinalTargetFile;
        }
        else
        {
            var labels = string.Empty;
            foreach (var label in Labels) labels += label.Name + " ";

            FinalTargetFile = Path.Combine(
                setting.OutputFolder,
                setting.RenameFilesWith
                    .Replace("%label%", labels)
                    .Replace("%id%", "" + ID)
                    .Replace("%name%", OriginalFileName)
                + FileExtension);

            return FinalTargetFile;
        }
    }
}

/// <summary>
///     Settings for the process of sorting files.
/// </summary>
public class LabelerSetting
{
    private string outputFolder = string.Empty;

    /// <summary>
    ///     List of all labels.
    /// </summary>
    public List<Label> Labels { get; set; } = [];

    /// <summary>
    ///     Determines the type of automation.
    /// </summary>
    public AutomationType Automation { get; set; } = AutomationType.Manual;

    /// <summary>
    ///     Determines the minimum duration of the files.
    /// </summary>
    public TimeSpan AutomationDuration { get; set; } = TimeSpan.Zero;

    /// <summary>
    ///     Use extensions for labels instead of file types.
    /// </summary>
    public bool AutomateFileTypeUseExtensions { get; set; }

    /// <summary>
    ///     Determines the minimum file size of the file.
    /// </summary>
    public long AutomateFileSizeMinSize { get; set; } = 1;

    /// <summary>
    ///     Determines the minimum Width of the image file, set to 0 for no sorting by width.
    /// </summary>
    public int AutomateImageSizeMinWidth { get; set; } = 100;

    /// <summary>
    ///     Determines the minimum Height of the image file, set to 0 for no sorting by height.
    /// </summary>
    public int AutomateImageSizeMinHeight { get; set; } = 100;

    /// <summary>
    ///     The folder to sort.
    /// </summary>
    public string InputFolder { get; set; } = string.Empty;

    /// <summary>
    ///     Determines if the files should be moved to another folder or
    /// </summary>
    public bool SortInInputFolder { get; set; } = true;

    /// <summary>
    ///     Determines the output folder.
    /// </summary>
    public string OutputFolder
    {
        get => SortInInputFolder ? InputFolder : outputFolder;
        set
        {
            SortInInputFolder = false;
            outputFolder = value;
        }
    }

    /// <summary>
    ///     Determines if the files should be copied or moved.
    /// </summary>
    public OperationType Operation { get; set; } = OperationType.Copy;

    /// <summary>
    ///     Determines labeling type of files, either put them in a subfolder or rename each file.
    /// </summary>
    public LabelFilesOptions LabelFilesBy { get; set; } = LabelFilesOptions.Subfolder;

    /// <summary>
    ///     Allows creating subfolders for each label.
    /// </summary>
    public bool AllowRecursiveSubfolders { get; set; }

    /// <summary>
    ///     Allows searching subfolders inside of input folder.
    /// </summary>
    public bool AllowSearchingSubfolders { get; set; }

    /// <summary>
    ///     String to use when renaming files.
    /// </summary>
    public string RenameFilesWith { get; set; } = "%label% - %name%";

    /// <summary>
    ///     File types to work on, just the names (like "mp3" and not "*.mp3").
    /// </summary>
    public string[] Filter { get; set; } = Array.Empty<string>();

    /// <summary>
    ///     use filters while searching files to sort.
    /// </summary>
    public bool UseFilters { get; set; }

    public LabelerSetting Load(string fileName)
    {
        if (!File.Exists(fileName)) throw new FileNotFoundException(fileName);
        using var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var stream = new BrotliStream(fs, CompressionMode.Decompress);
        // TODO
        throw new NotImplementedException();
    }

    public LabelerSetting Save(string fileName)
    {
        using var fs = !File.Exists(fileName)
            ? File.Create(fileName)
            : new FileStream(fileName, FileMode.Truncate, FileAccess.Write, FileShare.ReadWrite);
        using var stream = new BrotliStream(fs, CompressionMode.Compress);
        // TODO
        throw new NotImplementedException();
    }

    private IEnumerable<string> FilterFiles(string path, params string[] exts)
    {
        return
            exts.Select(x => "*." + x) // turn into globs
                .SelectMany(x =>
                    Directory.EnumerateFiles(path, x,
                        AllowSearchingSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
                );
    }

    /// <summary>
    ///     Gets the files.
    /// </summary>
    /// <returns>Array of files.</returns>
    public LabelFile[] GetFiles()
    {
        var files = UseFilters
            ? FilterFiles(InputFolder, Filter).ToArray()
            : Directory.GetFiles(InputFolder, "*.*",
                AllowSearchingSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
        var labelFiles = new LabelFile[files.Length];
        for (var i = 0; i < files.Length; i++)
            labelFiles[i] = new LabelFile
            {
                OriginalPath = files[i],
                ID = i
            };

        return labelFiles;
    }
}

/// <summary>
///     Determines how files should be operated.
/// </summary>
public enum LabelFilesOptions
{
    /// <summary>
    ///     Puts files inside subfolders each named after a label.
    /// </summary>
    Subfolder,

    /// <summary>
    ///     Renames files with the labels.
    /// </summary>
    Rename
}

/// <summary>
///     File Type, used for automation and previewer.
/// </summary>
public enum FileType
{
    /// <summary>
    ///     Unknown file type, unsupported by previewer.
    /// </summary>
    Unsupported,

    /// <summary>
    ///     Archive files
    /// </summary>
    Archive,

    /// <summary>
    ///     Audio files
    /// </summary>
    Audio,

    /// <summary>
    ///     Image files
    /// </summary>
    Image,

    /// <summary>
    ///     Video files
    /// </summary>
    Video,

    /// <summary>
    ///     Text files
    /// </summary>
    Text
}

/// <summary>
///     Determines what should be done to the files.
/// </summary>
public enum OperationType
{
    /// <summary>
    ///     Copies each file to the output folder.
    /// </summary>
    Copy,

    /// <summary>
    ///     Moves each file to the output folder.
    /// </summary>
    Move
}

/// <summary>
///     Determines the automation type.
/// </summary>
public enum AutomationType
{
    /// <summary>
    ///     No automation.
    /// </summary>
    Manual,

    /// <summary>
    ///     Automate by video/audio duration.
    /// </summary>
    AutomateByDuration,

    /// <summary>
    ///     Automate by video/image resolution.
    /// </summary>
    AutomateByImageSize,

    /// <summary>
    ///     Automate by file type.
    /// </summary>
    AutomateByFileType,

    /// <summary>
    ///     Automate by file size.
    /// </summary>
    AutomateByFileSize
}