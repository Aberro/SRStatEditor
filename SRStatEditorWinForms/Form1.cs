#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using SRStatEditor;

namespace SRStatEditorWinForms
{
    public partial class Form1 : Form
    {
        private string? _header;
        private string? _stats;
        private int _year;
        public Form1()
        {
            InitializeComponent();
        }

        private void LoadSave()
        {
            if(_header == null)
                throw new ArgumentNullException(nameof(_header));
            _year = Header.ReadYear(_header);
            tbYear.Text = _year.ToString();
            btnStart.Enabled = true;
        }

        private void EditSave()
        {
            if (_header == null || _stats == null) throw new ArgumentNullException();
            int modifyYear = (int)nudYear.Value;
            if (cbHeader.Checked)
                Header.ModifyHeader(_header, _year, modifyYear);


            var file = StatFile.ReadFile(_stats);
            var entries = StatEntry.FixDuplicates(file.Entries).ToArray();
            if (modifyYear != 0)
                entries.ForEach(x => x.Year += modifyYear);

            entries = StatEntry.DoMinimize(entries, (int)nudRecords.Value, (int)nudUnchanged.Value).OrderBy(x => x.Date).ToArray();
            file.ReplaceEntries(entries);
            file.WriteFile();
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dlg = new FolderBrowserDialog();
            if (tbSavePath.Text.Length > 0)
                dlg.SelectedPath = tbSavePath.Text;
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                tbSavePath.Text = dlg.SelectedPath;
                TryLoad();
            }
        }

        private void TryLoad()
        {
            _stats = Path.Combine(tbSavePath.Text, "stats.ini");
            _header = Path.Combine(tbSavePath.Text, "header.bin");
            if (!File.Exists(_stats) || !File.Exists(_header))
            {
                MessageBox.Show("Selected path does not contain a save!");
                _stats = null;
                _header = null;
            }
            else
            {
                LoadSave();
            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            EditSave();
        }

        private void tbSavePath_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\n' && tbSavePath.Text.Length > 0)
                TryLoad();
        }

        private void tbSavePath_Leave(object sender, EventArgs e)
        {
            if(tbSavePath.Text.Length > 0)
                TryLoad();
        }
    }
}
