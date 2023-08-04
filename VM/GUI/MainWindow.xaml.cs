using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VM;
using VM.OPSYS;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;
using System.Xml;

namespace VM.GUI
{

    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
            using (XmlReader reader = XmlReader.Create(new StringReader(JAVASCRIPT_SYNTAX_HIGHLIGHTING.HIGHLIGHTING)))
            {
                IHighlightingDefinition jsHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
                HighlightingManager.Instance.RegisterHighlighting("JavaScriptCustom", new[] { ".js" }, jsHighlighting);
            }
        }

        public static Dictionary<Computer, ComputerWindow> Computers = new();
        private uint lastID;

        public static ComputerWindow GetPCWindow(Computer pc)
        {
            return Computers[pc];
        }

        private void NewComputerButton(object sender, RoutedEventArgs e)
        {
            Computer pc = new(lastID++);
            ComputerWindow wnd = new(pc);

            Computers[pc] = wnd;



            wnd.Show();
            wnd.Closed += (o, e) => Computers.Remove(pc);
        }
    }
}
