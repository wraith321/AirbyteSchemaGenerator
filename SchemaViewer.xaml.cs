using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace AirbyteSchemaGeneratorWPF
{
    /// <summary>
    /// Interaction logic for SchemaViewer.xaml
    /// </summary>
    public partial class SchemaViewer : Window
    {
        private JsonObject? schema;

        public SchemaViewer()
        {
            InitializeComponent();
        }

        public void LoadSchema(JsonObject schema)
        {   
            //Load JsonObject into a TreeView
            TreeViewItem root = new TreeViewItem();
            root.Header = "Schema";
            root.IsExpanded = true;
            SchemaTree.Items.Add(root);
            FillSchemaTree(schema, root);
            this.schema = schema;
		}

        public void FillSchemaTree(JsonObject schema, TreeViewItem parent)
        {
			foreach (var property in schema)
            {
				TreeViewItem item = new TreeViewItem();
                if (property.Value is JsonObject)
                {				    
                    item.Header = property.Key;
					FillSchemaTree((JsonObject)property.Value, item);
                }
                else
                { 
                    item.Header = $"{property.Key}: {property.Value}";
                }
				parent.Items.Add(item);
			}
		}

		private void SaveSchema_Click(object sender, RoutedEventArgs e)
		{
            //Save the schema to a file
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
            if (saveFileDialog.ShowDialog() == true)
            {
                string json = schema!.ToString();
				File.WriteAllText(saveFileDialog.FileName, json);
			}
		}
	}
}
