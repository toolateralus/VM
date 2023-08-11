using System.Windows.Documents;
using System.Windows;
using System.Windows.Markup;
using Markdig;
using Markdig.Wpf;
using System.Windows.Controls;

namespace VM.GUI
{
    public partial class MarkdownViewer : UserControl
    {
        public MarkdownViewer()
        {
            InitializeComponent();
        }

        public string Markdown
        {
            get { return (string)GetValue(MarkdownProperty); }
            set { SetValue(MarkdownProperty, value); }
        }

        public static readonly DependencyProperty MarkdownProperty =
            DependencyProperty.Register(nameof(Markdown), typeof(string), typeof(MarkdownViewer), new PropertyMetadata(string.Empty, OnMarkdownChanged));

        public static void OnMarkdownChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is MarkdownViewer viewer && e.NewValue is string markdown)
            {
                viewer.RenderMarkdown(markdown);
            }
        }

        public void RenderMarkdown(string markdown)
        {
            if (string.IsNullOrEmpty(markdown))
            {
                markdownDisplay.Document = new FlowDocument();
                return;
            }

            var pipeline = new MarkdownPipelineBuilder().UseSupportedExtensions().Build();
            string xaml = Markdig.Wpf.Markdown.ToXaml(markdown, pipeline);
            var flowDocument = XamlReader.Parse(xaml) as FlowDocument;
            markdownDisplay.Document = flowDocument;
        }
    }
}
