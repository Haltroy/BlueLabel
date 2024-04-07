using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml;
using Avalonia.Media;
using File = System.IO.File;

namespace BlueLabel;

/// <summary>
///     Settings class used to manage application settings.
/// </summary>
public class Settings
{
    /// <summary>
    ///     Path of the default settings file location.
    /// </summary>
    private static string SettingsPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "haltroy", "bluelabel",
            "settings");

    /// <summary>
    ///     Theme of the application.
    /// </summary>
    public Theme Theme { get; set; } = Theme.Default;

    /// <summary>
    ///     The default accent color for BlueLabel.
    /// </summary>
    public static Color DefaultAccentColor => Color.Parse("#bd0082");

    /// <summary>
    ///     Accent color (pressed button, checkmark, toggle switch on mode etc.) color.
    /// </summary>
    public Color AccentColor { get; set; } = DefaultAccentColor;

    /// <summary>
    ///     The "Last Opened" items list.
    /// </summary>
    public SettingsItem[] LastItems { get; set; } = Array.Empty<SettingsItem>();

    /// <summary>
    ///     Determines if the background should be blurred or not.
    /// </summary>
    public bool UseBlur { get; set; } = true;

    /// <summary>
    ///     Loads settings (which is an XML file that is Brotli compressed) from file.
    /// </summary>
    /// <param name="fileName">XML file that is Brotli compressed and features the settings.</param>
    /// <returns>Returns the settings class itself.</returns>
    public Settings Load(string? fileName = null)
    {
        if (string.IsNullOrWhiteSpace(fileName) || !File.Exists(fileName))
            fileName = SettingsPath;

        if (!File.Exists(fileName)) return this;
        using var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var stream = new BrotliStream(fs, CompressionMode.Decompress);
        var doc = new XmlDocument();
        doc.Load(stream);
        if (doc.DocumentElement is null) return this;

        List<string> applied = [];
        foreach (XmlNode node in doc.DocumentElement.ChildNodes)
        {
            if (applied.Contains(node.Name.ToLowerInvariant())) continue;
            applied.Add(node.Name.ToLowerInvariant());
            switch (node.Name.ToLowerInvariant())
            {
                case "theme":
                    if (int.TryParse(node.InnerXml.ToLowerInvariant(), NumberStyles.Integer, null, out var theme))
                        Theme = (Theme)theme;
                    break;
                case "color":
                    if (uint.TryParse(node.InnerXml.ToLowerInvariant(), NumberStyles.Integer, null, out var color))
                        AccentColor = Color.FromUInt32(color);
                    break;
                case "blur":
                    UseBlur = node.InnerXml.ToLowerInvariant() == "true";
                    break;
                case "items":
                    LastItems = new SettingsItem[node.ChildNodes.Count];
                    for (var i = 0; i < node.ChildNodes.Count; i++)
                    {
                        var sub_node = node.ChildNodes[i];
                        if (sub_node is null || sub_node.Name.ToLowerInvariant() != "item" ||
                            sub_node.Attributes is null) continue;
                        var path = sub_node.InnerXml;
                        var date = string.Empty;

                        foreach (XmlAttribute attr in sub_node.Attributes)
                            if (attr.Name.ToLowerInvariant() == "date")
                                date = string.IsNullOrWhiteSpace(date) ? attr.InnerXml : date;

                        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path) && DateTime.TryParseExact(date, "G",
                                null, DateTimeStyles.AssumeUniversal, out var last_opened))
                            LastItems[i] = new SettingsItem(path, last_opened);
                    }

                    break;
            }
        }

        LastItems = LastItems.OrderByDescending(it => it.LastOpened).ToArray();
        return this;
    }

    /// <summary>
    ///     Saves the settings into a file (or to default settings path) which is an XML file compressed with Brotli.
    /// </summary>
    /// <param name="fileName">Fİle to store settings to.</param>
    /// <returns>The settings class itself.</returns>
    public Settings Save(string? fileName = null)
    {
        if (string.IsNullOrWhiteSpace(fileName) || !File.Exists(fileName))
            fileName = SettingsPath;
        new FileInfo(fileName).Directory?.Create();
        using var fs = File.Exists(fileName)
            ? new FileStream(fileName, FileMode.Truncate, FileAccess.Write, FileShare.ReadWrite)
            : File.Create(fileName);
        using var compress = new BrotliStream(fs, CompressionMode.Compress);
        using var stream = new StreamWriter(compress, Encoding.UTF8);

        LastItems = LastItems.OrderByDescending(it => it.LastOpened).ToArray();

        stream.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        stream.WriteLine("<root>");
        stream.WriteLine($"<Theme>{(int)Theme}</Theme>");
        stream.WriteLine($"<Color>{AccentColor.ToUInt32()}</Color>");
        stream.WriteLine($"<Blur>{(UseBlur ? "true" : "false")}</Blur>");
        if (LastItems.Length > 0)
        {
            stream.WriteLine("<Items>");
            foreach (var item in LastItems)
                stream.WriteLine($"<Item Date=\"{item.LastOpened.ToUniversalTime():G}\">{item.Path.ToXML()}</Item>");

            stream.WriteLine("</Items>");
        }

        stream.WriteLine("</root>");
        return this;
    }
}

/// <summary>
///     The app theme.
/// </summary>
public enum Theme
{
    /// <summary>
    ///     Sues the system default theme.
    /// </summary>
    Default,

    /// <summary>
    ///     Uses bright white light theme.
    /// </summary>
    Light,

    /// <summary>
    ///     Uses pitch black dark theme.
    /// </summary>
    Dark
}

/// <summary>
///     Item to display on "Last opened".
/// </summary>
public class SettingsItem
{
    /// <summary>
    ///     Creates a new empty item.
    /// </summary>
    // ReSharper disable once MemberCanBePrivate.Global
    public SettingsItem()
    {
    }

    /// <summary>
    ///     Creates a new item with path.
    /// </summary>
    /// <param name="path">Path of the project.</param>
    public SettingsItem(string path) : this()
    {
        Path = path;
    }

    /// <summary>
    ///     Creates a new item with date.
    /// </summary>
    /// <param name="last_opened">Date of the last open of the project.</param>
    public SettingsItem(DateTime last_opened) : this()
    {
        LastOpened = last_opened;
    }

    /// <summary>
    ///     Creates a new item with both path and last opened date.
    /// </summary>
    /// <param name="path">Path of the project.</param>
    /// <param name="last_opened">Date of the last open of the project.</param>
    public SettingsItem(string path, DateTime last_opened) : this()
    {
        Path = path;
        LastOpened = last_opened;
    }

    /// <summary>
    ///     Path of the last opened file.
    /// </summary>
    public DateTime LastOpened { get; set; } = DateTime.Now;

    /// <summary>
    ///     Date of the last opened file.
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    ///     Gets user-friendly name of the file.
    /// </summary>
    public string FileName => System.IO.Path.GetFileNameWithoutExtension(Path);
}