
using System;
using System.Windows.Forms;
using System.IO;

namespace PeepsCompress
{
    public partial class MainGUI : Form
    {
        public MainGUI()
        {
            InitializeComponent();
        }

        public string returnFilePath()
        {
            return filePathTextBox.Text;
        }

        public bool checkTextInput()
        {
            if(inputMethodComboBox.SelectedIndex == 1 && returnFilePath() != "")
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void beginButton_Click(object sender, EventArgs e)
        {
            if (File.Exists(returnFilePath()) || checkTextInput())
            {

                Compression algorithm;

                switch (compressionAlgorithmComboBox.SelectedIndex)
                {
                    case 0:
                        {
                            algorithm = new MIO0();
                            break;
                        }
                    case 1:
                        {
                            algorithm = new YAY0();
                            break;
                        }
                    case 2:
                        {
                            algorithm = new YAZ0();
                            break;
                        }
                    default:
                        {
                            algorithm = new MIO0();
                            break;
                        }
                }

                if (compressRadioButton.Checked)
                {
                    //compress mode

                    if (inputMethodComboBox.SelectedIndex == 0)
                    {
                        //file input
                        byte[] compressedFile = algorithm.compressInitialization(returnFilePath(), true);


                        if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                        {
                            try
                            {
                                using (BinaryWriter bw = new BinaryWriter(new FileStream(saveFileDialog1.FileName, FileMode.Create)))
                                {
                                    bw.Write(compressedFile);
                                    MessageBox.Show("File successfully compressed.");
                                }
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(ex.Message);
                            }

                        }
                    }
                    else
                    {
                        //string input
                        byte[] compressedFile = algorithm.compressInitialization(returnFilePath(), false);
                        if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                        {
                            try
                            {
                                using (BinaryWriter bw = new BinaryWriter(new FileStream(saveFileDialog1.FileName, FileMode.Create)))
                                {
                                    bw.Write(compressedFile);
                                    MessageBox.Show("File successfully compressed.");
                                }
                            }
                            catch(Exception ex)
                            {
                                MessageBox.Show(ex.Message);
                            }

                        }
                    }

                }
                else
                {
                    //decompress mode
                    byte[] decompressedFile = algorithm.decompressInitialization(returnFilePath());
                    if (decompressedFile != null)
                    {
                        if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                        {
                            try
                            {
                                using (BinaryWriter bw = new BinaryWriter(new FileStream(saveFileDialog1.FileName, FileMode.Create)))
                                {
                                    bw.Write(decompressedFile);
                                    MessageBox.Show("File successfully decompressed.");
                                }
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(ex.Message);
                            }

                        }
                    }
                }


            }
            else
            {
                MessageBox.Show("Error: Bad File Path");
            }
        }

        //File Selection
        private void browseButton_Click(object sender, EventArgs e)
        {
            filePathTextBox.Text = (openFileDialog1.ShowDialog() == DialogResult.OK) ? openFileDialog1.FileName : "Error: No such file found.";
        }

        //Initialize Drop-Down List Combo-Boxes
        private void MainGUI_Load(object sender, EventArgs e)
        {
            inputMethodComboBox.SelectedIndex = 0;
            compressionAlgorithmComboBox.SelectedIndex = 0;
        }

        //Compression Mode Changed
        private void compressRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            if (compressRadioButton.Checked) //if in compression mode
            {
                inputMethodComboBox.Enabled = true;
            }
            else
            {
                inputMethodComboBox.Enabled = false;
                inputMethodComboBox.SelectedIndex = 0;
            }
        }
    }
}
