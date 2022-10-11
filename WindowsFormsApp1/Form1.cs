using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
           
        }
        
        private HashSet<string>[] path_list = new HashSet<string>[11];
        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            //Take dropped items and store in array
            string[] droppedFiles = (string[]) e.Data.GetData(DataFormats.FileDrop);
            //loop trought all dropped items and display them
            foreach (string file in droppedFiles)
            {
                listBox1.Items.Clear();
                listBox1.Items.Add(file);
            }
            int newSize = 10;
            button1.Font = new Font(button1.Font.FontFamily, newSize);
            button1.BackColor = Color.LightGray;
        }

        private string GetFileName(string file)
        {
            return Path.GetFileNameWithoutExtension(file);
        }

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop, false) == true)
            {
                e.Effect = DragDropEffects.All;
            }
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            if (button1.BackColor == Color.LightGray )
            {
                backgroundWorker1.DoWork += BackgroundWorker1_DoWork;
                button1.Enabled = false;

                //If you support progress notification...
                backgroundWorker1.ProgressChanged += BackgroundWorker1_ProgressChanged;
                backgroundWorker1.WorkerReportsProgress = true;
                backgroundWorker1.RunWorkerAsync();
                /*System.Collections.IEnumerator em = listBox1.Items.GetEnumerator();
                while (em.MoveNext())
                {
                    Execute.ExecuteCleaning((string) em.Current, progressBar1, toolStripStatusLabel1);
                    //Execute.final_rename((string)em.Current + "/msg_h_data");
                    //Execute.CompressDirectory((string)em.Current);
                }*/
            }
            else
            {
                MessageBox.Show("Veuillez déposer un dossier");
            }

        }

        private void BackgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            Execute.DefinitonProgressBar(path_list, progressBar1);
            progressBar1.PerformStep();
        }

        private void BackgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            var worker = sender as BackgroundWorker;
            System.Collections.IEnumerator em = listBox1.Items.GetEnumerator();
            while (em.MoveNext())
            {
                Execute.ExecuteCleaning(path_list, (string)em.Current, progressBar1, toolStripStatusLabel1,worker);
            }
        }

        private void Button2_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderDlg = new FolderBrowserDialog
            {
                Description = "Selectionner le dossier contenant les données à traiter."
            };
            DialogResult result = folderDlg.ShowDialog();

            if (result == DialogResult.OK)
            {
                listBox1.Items.Clear();
                listBox1.Items.Add(folderDlg.SelectedPath);
                int newSize = 10;
                button1.Font = new Font(button1.Font.FontFamily, newSize);
                button1.BackColor = Color.LightGray;
            }
        }
    }
}
