using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Avalonia.Controls;
using ICSharpCode.SharpZipLib.BZip2;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using ICSharpCode.SharpZipLib.Zip;

namespace BlueLabel.Views;

public partial class PreviewArchive : PreviewUC
{
    private string archive = string.Empty;

    public PreviewArchive()
    {
        InitializeComponent();
        Loaded += (_, _) => GenerateEntries();
    }

    public override PreviewUC LoadWithFile(string file)
    {
        archive = file;
        return this;
    }

    private void GenerateEntries()
    {
        if (string.IsNullOrWhiteSpace(archive) || !File.Exists(archive)) return;
        foreach (var entry in GetEntries(archive))
            ArchiveTreeView.Items.Add(new TreeViewItem { Header = entry.Name, Tag = entry });
    }

    private ArchiveEntry[] GetEntries(string archivePath)
    {
        var ext = Path.GetFileName(archivePath).Remove(0, Path.GetFileNameWithoutExtension(archivePath).Length)
            .ToLowerInvariant();
        switch (ext)
        {
            case ".zip":
                return ReadZipArchive(archivePath);
            case ".bz2":
                var bz2Entry = new BZip2InputStream(new FileStream(archivePath, FileMode.Open, FileAccess.Read,
                    FileShare.ReadWrite));
                return
                [
                    new ArchiveEntry
                    {
                        Name = archivePath.Substring(0, archivePath.Length - 4),
                        Size = bz2Entry.Length,
                        Entry = bz2Entry
                    }
                ];
            case ".gz":
                var gzEntry = new GZipInputStream(new FileStream(archivePath, FileMode.Open, FileAccess.Read,
                    FileShare.ReadWrite));
                return
                [
                    new ArchiveEntry
                    {
                        Name = gzEntry.GetFilename(),
                        Size = gzEntry.Length,
                        Entry = gzEntry
                    }
                ];
            case ".tar":
                return ReadTarArchive(new FileStream(archivePath, FileMode.Open, FileAccess.Read,
                    FileShare.ReadWrite));
            case ".tgz":
            case ".tar.gz":
                return ReadTarArchive(new GZipInputStream(new FileStream(archivePath, FileMode.Open, FileAccess.Read,
                    FileShare.ReadWrite)));
            case ".tar.bz2":
                return ReadTarArchive(new BZip2InputStream(new FileStream(archivePath, FileMode.Open, FileAccess.Read,
                    FileShare.ReadWrite)));
            default:
                throw new FormatNotSupportedYetException(ext);
        }
    }

    private ArchiveEntry[] ReadZipArchive(string archivePath)
    {
        using var zipFile = new ZipFile(archivePath);
        var items = new ArchiveEntry[zipFile.Count];
        for (var i = 0; i < zipFile.Count; i++)
        {
            var entry = zipFile[i];
            items[i] = new ArchiveEntry
                { Name = entry.Name, Entry = entry, Size = entry.Size, Comment = entry.Comment };
        }

        return items;
    }

    private ArchiveEntry[] ReadTarArchive(Stream inputStream)
    {
        using var tarFile = TarArchive.CreateInputTarArchive(inputStream, Encoding.UTF8);
        List<ArchiveEntry> items = [];
        tarFile.ProgressMessageEvent += (_, entry, _) =>
        {
            if (items.FindAll(it => Equals(it.Entry, entry)).Count > 0) return;

            items.Add(new ArchiveEntry
            {
                Name = entry.Name,
                Entry = entry,
                Size = entry.Size,
                Comment = entry.UserName + ":" + entry.GroupName
            });
        };
        return items.ToArray();
    }

    private static string SimplifyFileSize(long fileSize)
    {
        const long KiB = 1024;
        const long MiB = KiB * 1024;
        const long GiB = MiB * 1024;
        const long TiB = GiB * 1024;

        return fileSize switch
        {
            < KiB => fileSize + " bytes",
            < MiB => Math.Round((double)fileSize / KiB, 2) + " KiB",
            < GiB => Math.Round((double)fileSize / MiB, 2) + " MiB",
            < TiB => Math.Round((double)fileSize / GiB, 2) + " GiB",
            _ => Math.Round((double)fileSize / TiB, 2) + " TiB"
        };
    }


    private void TreeViewSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not TreeView { SelectedItem: TreeViewItem { Tag: ArchiveEntry entry } }) return;
        NameBlock.Text = entry.Name;
        SizeBlock.Text = $"{SimplifyFileSize(entry.Size)} ({entry.Size})";
        CommentBox.Text = entry.Comment;
    }

    private class ArchiveEntry
    {
        public object? Entry { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Comment { get; init; } = string.Empty;
        public long Size { get; init; }
    }
}

internal class FormatNotSupportedYetException(string ext)
    : Exception(Lang.Lang.PreviewArchive_FormatNotSupportedYet.Replace("$ext$", ext));