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
using Microsoft.Win32;

namespace DDGModernizer
{
    public partial class Dashboard : Form
    {
        // Entity Vars

        DDGVersion currentVersion = DDGVersion.UNKNOWN;

        Dictionary<DDGVersion, string> regPaths = new Dictionary<DDGVersion, string>();

        public Dashboard()
        {
            InitializeComponent();

            ConfigRegPaths();

            comboBox_version.SelectedIndex = 0;
        }

        #region Util

        void ConfigRegPaths()
        {
            // Legacy windows charset fun...

            regPaths[DDGVersion.PRO_2] = "Software\\TAITO\\�d�Ԃłf�n�I�v���t�F�b�V���i���Q";
            regPaths[DDGVersion.SHINKANSEN] = "Software\\TAITO\\�d�Ԃłf�n�I�V���� �R�z�V������";
            regPaths[DDGVersion.FINAL] = "Software\\TAITO\\�d�Ԃłf�n�I�e�h�m�`�k";
        }

        bool IsVersionSupported(DDGVersion version)
        {
            return version == DDGVersion.PRO_2;
        }

        DDGVersion DetectVersion(string path)
        {
            string checkName = Path.GetFileNameWithoutExtension(path).ToLower();
            if (checkName.StartsWith("dgopro2"))
            {
                return DDGVersion.PRO_2;
            }
            else if (checkName.StartsWith("traingo"))
            {
                return DDGVersion.SHINKANSEN;
            }
            else if (checkName.StartsWith("perfect"))
            {
                return DDGVersion.FINAL;
            }

            return DDGVersion.UNKNOWN;
        }

        string TryGetPath(DDGVersion version)
        {
            string progFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string progFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            string checkPath = null;

            switch (version)
            {
                case DDGVersion.PRO_2:
                    checkPath = Path.Combine(progFiles, "TAITO\\dgopro2\\dgopro2.exe");

                    if (File.Exists(checkPath) == false)
                        checkPath = Path.Combine(progFilesX86, "TAITO\\dgopro2\\dgopro2.exe");

                    if (File.Exists(checkPath) == false)
                        checkPath = null;

                    break;

                case DDGVersion.SHINKANSEN:
                    checkPath = Path.Combine(progFiles, "TAITO\\TRAINGO\\TRAINGO.exe");

                    if (File.Exists(checkPath) == false)
                        checkPath = Path.Combine(progFilesX86, "TAITO\\TRAINGO\\TRAINGO.exe");

                    if (File.Exists(checkPath) == false)
                        checkPath = null;

                    break;

                case DDGVersion.FINAL:
                    checkPath = Path.Combine(progFiles, "TAITO\\perfect\\perfect.exe");

                    if (File.Exists(checkPath) == false)
                        checkPath = Path.Combine(progFilesX86, "TAITO\\perfect\\perfect.exe");

                    if (File.Exists(checkPath) == false)
                        checkPath = null;

                    break;

                case DDGVersion.UNKNOWN:
                default:
                    // Do nothing
                    break;
            }

            return checkPath;
        }

        #endregion

        #region Game Select

        private void SelectVersion(DDGVersion version)
        {
            if (comboBox_version.SelectedIndex != (int)version)
                comboBox_version.SelectedIndex = (int)version;

            currentVersion = version;

            // Try to get the path automatically
            if (version != DDGVersion.UNKNOWN &&
                textBox_EXEPath.Text.Trim().Length == 0)
            {
                string autoPath = TryGetPath(version);
                if (autoPath != null)
                {
                    textBox_EXEPath.Text = autoPath;
                    textBox_GameFolder.Text = Path.GetDirectoryName(autoPath);
                }
            }
            else if (version == DDGVersion.UNKNOWN)
            {
                textBox_EXEPath.Text = "";
                textBox_GameFolder.Text = "";
            }

            Aspect_InitForVersion(currentVersion);
            DrawDistance_InitForVersion(currentVersion);

            ValidateGoButton();
        }

        private void comboBox_version_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBox comboBox = (ComboBox)sender;
            DDGVersion selectedVersion = (DDGVersion)comboBox.SelectedIndex;
            SelectVersion(selectedVersion);
        }

        private void button_EXEPathBrowse_Click(object sender, EventArgs e)
        {
            string filePath = null;
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = "c:\\";
                openFileDialog.Filter = "exe files (*.exe)|*.exe|All files (*.*)|*.*";
                openFileDialog.FilterIndex = 2;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    filePath = openFileDialog.FileName;
                }
            }

            if (filePath != null)
            {
                textBox_EXEPath.Text = filePath;

                DDGVersion detectedVersion = DetectVersion(filePath);
                if (detectedVersion != DDGVersion.UNKNOWN)
                    SelectVersion(detectedVersion);
            }
        }

        private void button_GameFolderBrowse_Click(object sender, EventArgs e)
        {
            string filePath = null;
            using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog())
            {
                folderBrowserDialog.SelectedPath = Path.GetDirectoryName(textBox_EXEPath.Text);

                if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                {
                    filePath = folderBrowserDialog.SelectedPath;
                }
            }

            if (filePath != null)
            {
                textBox_GameFolder.Text = filePath;
            }

        }

        #endregion

        #region Go Button

        private void ValidateGoButton()
        {
            bool enabled = true;

            enabled &= IsVersionSupported(currentVersion);
            enabled &= File.Exists(textBox_EXEPath.Text);
            enabled &= Directory.Exists(textBox_GameFolder.Text);

            button_Go.Enabled = enabled;
        }

        private void button_Go_Click(object sender, EventArgs e)
        {
            Patcher patcher = new Patcher(currentVersion);

            patcher.SetModuleParam(Patcher.MODULE_KEY_ASPECT, "AspectX", (float)upDown_XAspect.Value);
            patcher.SetModuleParam(Patcher.MODULE_KEY_ASPECT, "AspectY", (float)upDown_YAspect.Value);
            patcher.SetModuleParam(Patcher.MODULE_KEY_ASPECT, "WinZoom", (float)upDown_WinZoom.Value);

            patcher.SetModuleParam(Patcher.MODULE_KEY_DRAW_DISTANCE, "RendPow", (float)upDown_RendPow.Value);

            patcher.PatchAndRun();
        }

        #endregion

        #region Aspect Ratio

        private bool Aspect_GetRegistryInfo(DDGVersion version)
        {
            if (regPaths.ContainsKey(version) == false)
                return false;

            string regKey = regPaths[version];
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(regKey))
                {
                    if (key == null)
                        return false;

                    var names = key.GetValueNames();
                    object scrwidth = key.GetValue("scrwidth");
                    object scrheight = key.GetValue("scrheight");

                    if (scrwidth != null)
                        upDown_WinX.Value = Convert.ToInt32(scrwidth);

                    if (scrheight != null)
                        upDown_WinY.Value = Convert.ToInt32(scrheight);

                    return true;
                }
            }
            catch 
            {
                return false;
            }

            return false;
        }

        private void Aspect_AutoCalculate(DDGVersion version)
        {
            // Start off by setting Y to 0.5
            upDown_YAspect.Value = (decimal)0.5f;

            // 1.0 X Aspect is when X == 4.0f * Y / 3.0f
            // X Aspect is reciprocal to the desired width coefficient from 4:3
            //     So, if you want 16:9 @ 1920x1080, think of it like this:
            //     - The 4:3 width at 1080 height is 1440 (4.0 * 1080.0 / 3.0 == 1440.0)
            //     - The desired width is 1920
            //     - So, the X aspect is (1440.0 / 1920.0 == 0.75)
            float sdHeight = decimal.ToSingle(upDown_WinY.Value);
            float sdWidth = 4.0f * sdHeight / 3.0f;
            float desiredWidth = decimal.ToSingle(upDown_WinX.Value);
            float xAspect = sdWidth / desiredWidth;
            upDown_XAspect.Value = (decimal)xAspect;

            // Now we need to calculate the WinZoom
            // This controls the object culling as things get closer to the
            //     camera.
            // Default winZoom is 640.0f, but this value needs to
            //     go down as the aspect widens.
            float winZoom = xAspect * 640.0f;
            upDown_WinZoom.Value = (decimal)winZoom;
        }

        private void Aspect_RestoreDefaults(DDGVersion version)
        {
            upDown_WinX.Value = 640;
            upDown_WinY.Value = 480;

            upDown_XAspect.Value = (decimal)1.0f;
            upDown_YAspect.Value = (decimal)0.5f;
            upDown_WinZoom.Value = 640;
        }

        private void Aspect_InitForVersion(DDGVersion version)
        {
            Aspect_RestoreDefaults(version);

            if (Aspect_GetRegistryInfo(version))
                Aspect_AutoCalculate(version);
        }

        private void checkBox_AspectEnable_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            upDown_WinX.Enabled = checkBox.Checked;
            upDown_WinY.Enabled = checkBox.Checked;
            button_AspectDefault.Enabled = checkBox.Checked;
            button_AspectAuto.Enabled = checkBox.Checked;
            upDown_XAspect.Enabled = checkBox.Checked;
            upDown_YAspect.Enabled = checkBox.Checked;
            upDown_WinZoom.Enabled = checkBox.Checked;
        }

        private void button_AspectDefault_Click(object sender, EventArgs e)
        {
            Aspect_RestoreDefaults(currentVersion);
        }

        private void button_AspectAuto_Click(object sender, EventArgs e)
        {
            Aspect_AutoCalculate(currentVersion);
        }

        #endregion

        #region Draw Distance

        private void DrawDistance_RestoreDefaults(DDGVersion version)
        {
            upDown_RendPow.Value = (decimal)0.6;
            trackBar_RendPow.Value = (int)(upDown_RendPow.Value * 1000);
        }

        private void DrawDistance_InitForVersion(DDGVersion version)
        {
            DrawDistance_RestoreDefaults(version);
        }

        private void upDown_RendPow_ValueChanged(object sender, EventArgs e)
        {
            NumericUpDown upDown = (NumericUpDown)sender;
            trackBar_RendPow.Value = (int)(upDown.Value * 1000);
        }

        private void trackBar_RendPow_Scroll(object sender, EventArgs e)
        {
            TrackBar trackBar = (TrackBar)sender;
            upDown_RendPow.Value = (decimal)trackBar.Value / 1000;
        }

        private void button_RendPowDefault_Click(object sender, EventArgs e)
        {
            DrawDistance_RestoreDefaults(currentVersion);
        }

        private void checkBox_DrawDistEnable_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            upDown_RendPow.Enabled = checkBox.Checked;
            trackBar_RendPow.Enabled = checkBox.Checked;
            button_RendPowDefault.Enabled = checkBox.Checked;
        }

        #endregion

    }
}
