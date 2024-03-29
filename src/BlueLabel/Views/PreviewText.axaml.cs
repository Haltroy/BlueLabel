using System.IO;

namespace BlueLabel.Views;

public partial class PreviewText : PreviewUC
{
    private string text = string.Empty;

    public PreviewText()
    {
        InitializeComponent();
        Loaded += (_, _) => FileText.Text = text;
    }

    public override PreviewUC LoadWithFile(string file)
    {
        using var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(fs);
        return LoadWithText(reader.ReadToEnd());
    }

    public PreviewUC LoadWithText(string _text)
    {
        text = _text;
        return this;
    }
}