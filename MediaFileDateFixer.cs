/*
  Media File Date Fixer
  Copyright 2018 Bernhard Schelling

  Licensed under the Apache License, Version 2.0 (the "License");
  you may not use this file except in compliance with the License.
  You may obtain a copy of the License at

      http://www.apache.org/licenses/LICENSE-2.0

  Unless required by applicable law or agreed to in writing, software
  distributed under the License is distributed on an "AS IS" BASIS,
  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
  See the License for the specific language governing permissions and
  limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;

[assembly: System.Reflection.AssemblyTitle("MediaFileDateFixer")]
[assembly: System.Reflection.AssemblyProduct("MediaFileDateFixer")]
[assembly: System.Reflection.AssemblyVersion("1.0.0.0")]
[assembly: System.Reflection.AssemblyFileVersion("1.0.0.0")]
[assembly: System.Runtime.InteropServices.ComVisible(false)]

namespace MediaFileDateFixer
{
    public enum EProgressState { Ready, Counting, Querying, Applying }
    public enum EProgressType { CountInit, CountIncrement, Finish };

    class Entry
    {
        internal static TimeSpan FixOffset = new TimeSpan(0);

        public bool   ColActive    { get { return Active; } }
        public string ColDetection { get { return Detection.ToString().Replace('_', ' '); } }
        public string ColFileName  { get { return FileName; } }
        public string ColFileDate  { get { return (FileDate != DateTime.MinValue ? FileDate.ToString() : null); } }
        public string ColMetaDate  { get { return (MetaDate != DateTime.MinValue ? MetaDate.ToString() + (!Active || FixOffset.Ticks == 0 ? null : (FixOffset.Ticks > 0 ? " (+" : " (") + FixOffset.ToString() + ")") : null); } }
        public string ColError     { get { return Error; } }
        public string ColDiff      { get { return (MetaDate != DateTime.MinValue && Diff != 0 ? new TimeSpan(0, 0, Diff).ToString() : null); } }

        internal enum EDetection { Matching_Dates = 1, Normal_Difference = 2, Small_Difference = 4, Big_Difference = 8, Time_Zone_Difference = 16, Applied = 32, No_Meta_Date = 1024, Format_Error = 2048 };
        internal bool Active;
        internal EDetection Detection;
        internal string FileName, Error;
        internal DateTime FileDate = DateTime.MinValue, MetaDate = DateTime.MinValue;
        internal int Diff;
    }

    static class MediaFileDateFixer
    {
        static List<Entry> Entries;
        static int ActiveCount = 0;
        static int DetectionFilter = (int)Entry.EDetection.Matching_Dates | (int)Entry.EDetection.Format_Error | (int)Entry.EDetection.No_Meta_Date;
        internal static string Directory = null;

        internal static List<Entry> GetEntries() { return (Entries == null || Entries.Count == 0 ? null : Entries); }
        internal static int GetActiveCount() { return ActiveCount; }
        internal static int GetDetectionFilter() { return DetectionFilter; }

        internal delegate void OnProgressDelegate(EProgressState State, EProgressType Type, int Progress = 0);
        internal static OnProgressDelegate OnProgress = null;
        
        internal static void MessageBoxAllMetaData(string f)
        {
            string res = "";
            int lines = 0;
            foreach (MetadataExtractor.Directory d in MetadataExtractor.ImageMetadataReader.ReadMetadata(f))
                foreach (MetadataExtractor.Tag t in d.Tags)
                {
                    res += (res.Length > 0 ? "\n" : "") + t.ToString().PadRight(150).Substring(0, 150).TrimEnd();
                    if (++lines == 50) { MessageBox.Show(res); res = ""; lines = 0; }

                }
            if (lines > 0) MessageBox.Show(res);
        }

        internal static void SortEntries(string SortProp, bool Desc)
        {
            if (Entries == null || Entries.Count == 0) return;
            System.Reflection.FieldInfo fi = typeof(Entry).GetField(SortProp.Substring(3), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Comparison<Entry> Fn;
            if      (fi.FieldType == typeof(string)   && !Desc) Fn = (Entry a, Entry b) =>  string.CompareOrdinal((string)fi.GetValue(a), (string)fi.GetValue(b));
            else if (fi.FieldType == typeof(string)   &&  Desc) Fn = (Entry a, Entry b) => -string.CompareOrdinal((string)fi.GetValue(a), (string)fi.GetValue(b));
            else if (fi.FieldType == typeof(DateTime) && !Desc) Fn = (Entry a, Entry b) =>  DateTime.Compare((DateTime)fi.GetValue(a), (DateTime)fi.GetValue(b));
            else if (fi.FieldType == typeof(DateTime) &&  Desc) Fn = (Entry a, Entry b) => -DateTime.Compare((DateTime)fi.GetValue(a), (DateTime)fi.GetValue(b));
            else if (fi.FieldType == typeof(int)      && !Desc) Fn = (Entry a, Entry b) => (int)fi.GetValue(a)-(int)fi.GetValue(b);
            else if (fi.FieldType == typeof(int)      &&  Desc) Fn = (Entry a, Entry b) => (int)fi.GetValue(b)-(int)fi.GetValue(a);
            else if (fi.FieldType.IsEnum              && !Desc) Fn = (Entry a, Entry b) => (int)fi.GetValue(a)-(int)fi.GetValue(b);
            else if (fi.FieldType.IsEnum              &&  Desc) Fn = (Entry a, Entry b) => (int)fi.GetValue(b)-(int)fi.GetValue(a);
            else throw new NotImplementedException();
            Entries.Sort(Fn);
        }

        static void GetFilesRecursive(List<string> Files, string Directory)
        {
            DirectoryInfo di = new DirectoryInfo(Directory);
            foreach (FileInfo fi in di.GetFiles()) Files.Add(fi.FullName);
            foreach (DirectoryInfo cdi in di.GetDirectories()) GetFilesRecursive(Files, cdi.FullName);
        }
        
        static int[] TagsExif    = { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagDateTimeOriginal, MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagDateTimeDigitized, MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagDateTime };
        static int[] TagsQTMovie = { MetadataExtractor.Formats.QuickTime.QuickTimeMovieHeaderDirectory.TagCreated, MetadataExtractor.Formats.QuickTime.QuickTimeMovieHeaderDirectory.TagModified };
        static int[] TagsQTTrack = { MetadataExtractor.Formats.QuickTime.QuickTimeTrackHeaderDirectory.TagCreated, MetadataExtractor.Formats.QuickTime.QuickTimeTrackHeaderDirectory.TagModified };
        static int[] TagsQTMeta  = { MetadataExtractor.Formats.QuickTime.QuickTimeMetaDirectory.TagCreationDateNoTimeZone, MetadataExtractor.Formats.QuickTime.QuickTimeMetaDirectory.TagCreationDate };
        static int[] TagsPng     = { MetadataExtractor.Formats.Png.PngDirectory.TagLastModificationTime };

        internal static void Query()
        {
            OnProgress(EProgressState.Counting, EProgressType.CountInit, 1);
            DirectoryInfo di = new DirectoryInfo(Directory);
            List<string> Files = new List<string>();
            DirectoryInfo[] SubDirectories = di.GetDirectories();
            foreach (FileInfo fi in di.GetFiles()) Files.Add(fi.FullName);

            OnProgress(EProgressState.Querying, EProgressType.CountInit, SubDirectories.Length < 1 ? 1 : SubDirectories.Length);
            foreach (DirectoryInfo cdi in SubDirectories)
            {
                GetFilesRecursive(Files, cdi.FullName);
                OnProgress(EProgressState.Querying, EProgressType.CountIncrement);
            }

            OnProgress(EProgressState.Querying, EProgressType.CountInit, Files.Count);
            ActiveCount = 0;
            Entries = new List<Entry>();
            foreach (string f in Files)
            {
                Entry en = new Entry();
                en.FileName = f;
                en.FileDate = new FileInfo(f).LastWriteTime;
                try
                {
                    foreach (MetadataExtractor.Directory d in MetadataExtractor.ImageMetadataReader.ReadMetadata(f))
                    {
                        int[] Tags;
                        if      (d is MetadataExtractor.Formats.Exif.ExifDirectoryBase)                  Tags = TagsExif;
                        else if (d is MetadataExtractor.Formats.QuickTime.QuickTimeMovieHeaderDirectory) Tags = TagsQTMovie;
                        else if (d is MetadataExtractor.Formats.QuickTime.QuickTimeTrackHeaderDirectory) Tags = TagsQTTrack;
                        else if (d is MetadataExtractor.Formats.QuickTime.QuickTimeMetaDirectory)        Tags = TagsQTMeta;
                        else if (d is MetadataExtractor.Formats.Png.PngDirectory)                        Tags = TagsPng;
                        else continue;
                        foreach (int Tag in Tags)
                        {
                            DateTime dt;
                            if (!MetadataExtractor.DirectoryExtensions.TryGetDateTime(d, Tag, out dt)) continue;
                            en.MetaDate = (Tags == TagsPng ? dt.ToLocalTime() : new DateTime(dt.Ticks, DateTimeKind.Local)); //PNG stores time in UTC
                            if (Tags == TagsQTMeta) goto FoundMetaDate; //Prioritize meta directory because the other QT time fields can be in UTC (but aren't always)
                            break;
                        }
                    }
                }
                catch (Exception e) { en.Error = e.Message; }
                FoundMetaDate:

                long Diff = (en.MetaDate == DateTime.MinValue || en.Error != null ? 0 : (long)(en.FileDate - en.MetaDate).TotalSeconds);
                long AbsDiff = Math.Abs(Diff), HourAbsDiff = Math.Abs(((AbsDiff + 1800) % 3600) - 1800);
                if      (en.Error != null)                       en.Detection = Entry.EDetection.Format_Error;
                else if (en.FileDate.Ticks == en.MetaDate.Ticks) en.Detection = Entry.EDetection.Matching_Dates;
                else if (en.MetaDate == DateTime.MinValue)       en.Detection = Entry.EDetection.No_Meta_Date;
                else if (AbsDiff < 10)                           en.Detection = Entry.EDetection.Small_Difference;
                else if (AbsDiff > 30758400)                     en.Detection = Entry.EDetection.Big_Difference;
                else if (AbsDiff < 86405 && HourAbsDiff < 5)     en.Detection = Entry.EDetection.Time_Zone_Difference;
                else                                             en.Detection = Entry.EDetection.Normal_Difference;
                en.Active = false;
                en.Diff = (Diff > int.MaxValue ? int.MaxValue : (Diff < int.MinValue ? int.MinValue : (int)Diff));
                Entries.Add(en);

                OnProgress(EProgressState.Querying, EProgressType.CountIncrement);
            }
            OnProgress(EProgressState.Ready, EProgressType.Finish);
        }

        internal static void Apply()
        {
            if (Entries == null || Entries.Count == 0 || ActiveCount == 0) return;

            OnProgress(EProgressState.Counting, EProgressType.CountInit, 1);
            int Count = 0;
            foreach (Entry en in Entries) if (en.Active && en.MetaDate == DateTime.MinValue && en.FileDate != en.MetaDate && ((DetectionFilter & (int)en.Detection) == 0)) Count++;
            OnProgress(EProgressState.Applying, EProgressType.CountInit, 1);

            foreach (Entry en in Entries)
            {
                if (!en.Active || en.MetaDate == DateTime.MinValue || en.FileDate == en.MetaDate || ((DetectionFilter & (int)en.Detection) != 0)) continue;
                en.Diff = 0;
                en.Active = false;
                en.Detection = Entry.EDetection.Applied;
                en.FileDate = en.MetaDate + Entry.FixOffset;
                File.SetLastWriteTime(en.FileName, en.FileDate);
                OnProgress(EProgressState.Applying, EProgressType.CountIncrement);
            }

            ActiveCount = 0;
            OnProgress(EProgressState.Ready, EProgressType.Finish);
        }

        internal static bool SetActive(Entry en, bool NewActive)
        {
            if (en.Active == NewActive || en.MetaDate == DateTime.MinValue || (en.FileDate == en.MetaDate + Entry.FixOffset && NewActive)) return false;
            en.Active = NewActive; 
            ActiveCount += (NewActive ? 1 : -1);
            return true;
        }

        internal static bool ToggleActive(Entry en)
        {
            if (en.MetaDate == DateTime.MinValue || en.FileDate == en.MetaDate + Entry.FixOffset) return false;
            en.Active ^= true; 
            ActiveCount += (en.Active ? 1 : -1);
            return true;
        }
        
        internal static void ToggleFilter(Entry.EDetection DetectionToggle)
        {
            int OldFilter = DetectionFilter;
            DetectionFilter ^= (int)DetectionToggle;
            if (Entries == null || Entries.Count == 0) return;
            foreach (Entry en in Entries)
            {
                if (!en.Active) continue;
                bool OldVisibility = ((OldFilter & (int)en.Detection) == 0), NewVisibility = ((DetectionFilter & (int)en.Detection) == 0);
                if (OldVisibility != NewVisibility) ActiveCount += (NewVisibility ? 1 : -1);
            }
        }
    }

    static class MediaFileDateFixerUI
    {
        static MediaFileDateFixerForm f;

        static DataGridViewColumn MakeCol<T>(string HeaderText, string DataPropertyName, float FillWeight) where T : DataGridViewCell, new()
        {
            DataGridViewColumn res = new DataGridViewColumn(new T());
            res.HeaderText = HeaderText;
            res.DataPropertyName = DataPropertyName;
            res.HeaderCell.Style.WrapMode = DataGridViewTriState.False;
            res.SortMode = DataGridViewColumnSortMode.Programmatic;
            if (FillWeight > 0) res.FillWeight = FillWeight;
            else { res.AutoSizeMode = DataGridViewAutoSizeColumnMode.None; res.Resizable = DataGridViewTriState.False; res.Width = 20; }
            return res;
        }

        static void RefreshApplyButton()
        {
            f.btnApply.Text = f.btnApply.Tag.ToString().Replace("#", MediaFileDateFixer.GetActiveCount().ToString());
            f.btnApply.Enabled = (MediaFileDateFixer.GetActiveCount() > 0);
        }

        static void SetActive(DataGridViewRow r, bool NewActive)
        {
            if (!MediaFileDateFixer.SetActive(r.DataBoundItem as Entry, NewActive)) return;
            f.gridMain.InvalidateRow(r.Index);
            RefreshApplyButton();
        }

        static void ToggleActive(DataGridViewRow r, bool Invalidate = true)
        {
            if (!MediaFileDateFixer.ToggleActive(r.DataBoundItem as Entry)) return;
            if (Invalidate) f.gridMain.InvalidateRow(r.Index);
            RefreshApplyButton();
        }

        static void SetAllActive(bool NewActive)
        {
            f.gridMain.SuspendDrawing();
            foreach (DataGridViewRow r in f.gridMain.Rows)
                if (r.Visible)
                    SetActive(r, NewActive);
            f.gridMain.ResumeDrawing();
        }

        static void RefreshFilter()
        {
            f.gridMain.SuspendDrawing();
            int NewFilter = MediaFileDateFixer.GetDetectionFilter();
            foreach (DataGridViewRow r in f.gridMain.Rows)
            {
                bool NewVisibility = ((NewFilter & (int)(r.DataBoundItem as Entry).Detection) == 0);
                if (r.Visible == NewVisibility) continue;
                if (f.gridMain.CurrentCell != null && f.gridMain.CurrentCell.RowIndex == r.Index) f.gridMain.CurrentCell = null;
                if (r.Selected) r.Selected = false;
                r.Visible = NewVisibility;
            }
            f.gridMain.ResumeDrawing();
        }

        static void ToggleFilter(Entry.EDetection DetectionToggle)
        {
            MediaFileDateFixer.ToggleFilter(DetectionToggle);
            RefreshFilter();
            RefreshApplyButton();
        }

        [STAThread] static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (args.Length >= 1)
            {
                if (!Directory.Exists(args[0]))
                {
                    MessageBox.Show("Directory '" + args[0] + "' does not exist", "Error");
                    return;
                }

                MediaFileDateFixer.Directory = args[0];
            }

            f = new MediaFileDateFixerForm();

            f.gridMain.AutoGenerateColumns = false;
            f.gridMain.AutoSize = false;
            f.gridMain.ShowCellToolTips = true;
            f.gridMain.DataSource = MediaFileDateFixer.GetEntries();
            f.gridMain.ReadOnly = false;
            f.gridMain.Columns.Add(MakeCol<DataGridViewCheckBoxCell>("", "ColActive", 0));
            f.gridMain.Columns.Add(MakeCol<DataGridViewTextBoxCell>("Name", "ColFileName", 3));
            f.gridMain.Columns.Add(MakeCol<DataGridViewTextBoxCell>("File Date", "ColFileDate", 2));
            f.gridMain.Columns.Add(MakeCol<DataGridViewTextBoxCell>("Meta Date", "ColMetaDate", 2));
            f.gridMain.Columns.Add(MakeCol<DataGridViewTextBoxCell>("Detection", "ColDetection", 1));
            f.gridMain.Columns.Add(MakeCol<DataGridViewTextBoxCell>("Error", "ColError", 1));
            f.gridMain.Columns.Add(MakeCol<DataGridViewTextBoxCell>("Difference", "ColDiff", 1));
            f.gridMain.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            f.gridMain.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;

            f.gridMain.ColumnHeaderMouseClick += (object sender, DataGridViewCellMouseEventArgs e) =>
            {
                SortOrder NewOrder = (f.gridMain.Columns[e.ColumnIndex].HeaderCell.SortGlyphDirection == SortOrder.Ascending ? SortOrder.Descending : SortOrder.Ascending);
                foreach (DataGridViewColumn c in f.gridMain.Columns) c.HeaderCell.SortGlyphDirection = SortOrder.None;
                MediaFileDateFixer.SortEntries(f.gridMain.Columns[e.ColumnIndex].DataPropertyName, (NewOrder == SortOrder.Descending));
                f.gridMain.Columns[e.ColumnIndex].HeaderCell.SortGlyphDirection = NewOrder;
                RefreshFilter();
                f.gridMain.Invalidate();
            };

            f.gridMain.CellFormatting += (object sender, DataGridViewCellFormattingEventArgs e) =>
            {
                Entry en = f.gridMain.Rows[e.RowIndex].DataBoundItem as Entry;
                if      (en.MetaDate == DateTime.MinValue) { e.CellStyle.SelectionBackColor = e.CellStyle.BackColor = SystemColors.ControlDark; e.CellStyle.SelectionForeColor = e.CellStyle.ForeColor = SystemColors.ControlText; }
                else if (!en.Active && en.FileDate == en.MetaDate + Entry.FixOffset) { e.CellStyle.SelectionBackColor = e.CellStyle.BackColor = SystemColors.ControlLight; e.CellStyle.SelectionForeColor = e.CellStyle.ForeColor = SystemColors.ControlText; }
                else if (e.ColumnIndex == 2 && !en.Active) { e.CellStyle.SelectionBackColor = e.CellStyle.BackColor = Color.Green;              e.CellStyle.SelectionForeColor = e.CellStyle.ForeColor = Color.White; }
                else if (e.ColumnIndex == 3 &&  en.Active) { e.CellStyle.SelectionBackColor = e.CellStyle.BackColor = Color.Green;              e.CellStyle.SelectionForeColor = e.CellStyle.ForeColor = Color.White; }
            };

            f.gridMain.KeyPress += (object sender, KeyPressEventArgs e) =>
            {
                if (e.KeyChar != ' ') return;
                if (f.gridMain.SelectedRows.Count == 0) return;
                bool NewActive = !((f.gridMain.CurrentCell.Selected ? f.gridMain.CurrentCell.OwningRow : f.gridMain.SelectedRows[0]).DataBoundItem as Entry).Active;
                foreach (DataGridViewRow r in f.gridMain.SelectedRows) if (r.Visible) SetActive(r, NewActive);
                if (f.gridMain.CurrentCell.ColumnIndex == 0) ToggleActive(f.gridMain.CurrentCell.OwningRow, false);
            };

            f.gridMain.CellClick += (object s, DataGridViewCellEventArgs e) =>
            {
                if (e.RowIndex >= 0 && e.ColumnIndex == 2) SetActive(f.gridMain.Rows[e.RowIndex], false);
                if (e.RowIndex >= 0 && e.ColumnIndex == 3) SetActive(f.gridMain.Rows[e.RowIndex], true);
            };

            f.gridMain.CellContentClick += (object s, DataGridViewCellEventArgs e) =>
            {
                if (e.RowIndex >= 0 && e.ColumnIndex == 0) ToggleActive(f.gridMain.Rows[e.RowIndex]);
            };

            f.gridMain.CellContentDoubleClick += (object s, DataGridViewCellEventArgs e) =>
            {
                if (e.RowIndex >= 0 && e.ColumnIndex == 0) ToggleActive(f.gridMain.Rows[e.RowIndex]);
            };

            f.gridMain.CellDoubleClick += (object s, DataGridViewCellEventArgs e) =>
            {
                Entry en = (e.RowIndex >= 0 ? f.gridMain.Rows[e.RowIndex].DataBoundItem as Entry : null);
                if (e.ColumnIndex == 1 && en != null) System.Diagnostics.Process.Start("explorer", en.FileName);
                if (e.ColumnIndex == 4 && en != null) MediaFileDateFixer.MessageBoxAllMetaData(en.FileName);
            };

            MediaFileDateFixer.OnProgress = (EProgressState State, EProgressType Type, int Progress) =>
            {
                if (f.Disposing || f.IsDisposed) { return; }
                if (f.InvokeRequired) { try { f.Invoke((Action) delegate { MediaFileDateFixer.OnProgress(State, Type, Progress); }); } catch (Exception) { } return; }

                if (Type == EProgressType.CountInit)
                {
                    f.pbProgress.Maximum = Progress;
                    f.pbProgress.Value = 0;
                }
                else if (Type == EProgressType.CountIncrement)
                {
                    if (f.pbProgress.Value < f.gridMain.RowCount) f.gridMain.InvalidateRow(f.pbProgress.Value);
                    f.pbProgress.SetProgressNoAnimation(f.pbProgress.Value + 1);
                }
                else if (Type == EProgressType.Finish)
                {
                    f.gridMain.DataSource = MediaFileDateFixer.GetEntries();
                    RefreshApplyButton();
                    RefreshFilter();
                    f.pbProgress.Value = 0;
                }
                f.lblState.Text = State.ToString().Replace('_', ' ');
            };

            Action<ThreadStart> RunThreaded = (ThreadStart ts) => { Thread t = new Thread(ts); t.IsBackground = true; t.Start(); };
            EventHandler DisableSelection = (object s, EventArgs e) => { f.gridMain.ClearSelection(); };
            Action<bool> Lock = (bool DoLock) => { if (f.Disposing || f.IsDisposed) return; f.Invoke((Action) delegate
            {
                f.gridMain.Enabled = f.btnFilter.Enabled = !DoLock;
                f.gridMain.SelectionChanged -= DisableSelection;
                if (DoLock) { f.gridMain.SelectionChanged += DisableSelection; f.gridMain.ClearSelection(); f.btnApply.Enabled = false; }
            });};

            f.btnFilter.Click += (object s, EventArgs e) =>
            { 
                ContextMenu context = new ContextMenu();
                foreach (Entry.EDetection Name in Enum.GetValues(typeof(Entry.EDetection)))
                {
                    MenuItem i = context.MenuItems.Add(Name.ToString().Replace('_', ' '));
                    i.Checked = ((MediaFileDateFixer.GetDetectionFilter() & (int)Name) == 0);
                    i.Tag = Name;
                    i.Click += (object sender, EventArgs ee) =>
                    {
                        (sender as MenuItem).Checked ^= true;
                        ToggleFilter((Entry.EDetection)(sender as MenuItem).Tag);
                        (sender as MenuItem).GetContextMenu().Show(f.btnFilter, new Point(0,0));
                    };
                }
                context.Show(f.btnFilter, new Point(0,0));
            };

            f.numOffsetHour.ValueChanged   += (object sender, EventArgs e) => { Entry.FixOffset = new TimeSpan((int)f.numOffsetHour.Value, (int)f.numOffsetMinute.Value, 0); f.gridMain.Invalidate(); };
            f.numOffsetMinute.ValueChanged += (object sender, EventArgs e) => { Entry.FixOffset = new TimeSpan((int)f.numOffsetHour.Value, (int)f.numOffsetMinute.Value, 0); f.gridMain.Invalidate(); };
            f.btnApply.Click += (object s, EventArgs e) => { Lock(true); RunThreaded(() => { MediaFileDateFixer.Apply(); Lock(false); }); };
            f.btnEnableAll.Click += (object s, EventArgs e) => { SetAllActive(true); };
            f.btnDisableAll.Click += (object s, EventArgs e) => { SetAllActive(false); };

            RefreshApplyButton();
            f.gridMain.Enabled = f.btnFilter.Enabled = f.btnApply.Enabled = false;

            f.btnOpen.Click += (object sender, EventArgs e) =>
            {
                FolderBrowserDialog fbd = new FolderBrowserDialog();
                fbd.RootFolder = Environment.SpecialFolder.MyComputer;
                fbd.SelectedPath = MediaFileDateFixer.Directory;
                if (fbd.ShowDialog() != DialogResult.OK) { fbd.Dispose(); return; }
                MediaFileDateFixer.Directory = fbd.SelectedPath;
                fbd.Dispose();
                f.gridMain.DataSource = null;
                RunThreaded(() => { MediaFileDateFixer.Query(); Lock(false); });
            };

            f.Shown += (object sender, EventArgs e) =>
            {
                if (MediaFileDateFixer.Directory == null) f.btnOpen.PerformClick();
                else RunThreaded(() => { MediaFileDateFixer.Query(); Lock(false); });
            };

            Application.Run(f);
        }
    }

    static class ExtensionMethods
    {
        internal static void SetProgressNoAnimation(this ProgressBar pb, int value)
        {   
            //To avoid animation, we need to move the progress bar backwards
            if (value < pb.Maximum) { pb.Value = value + 1; } //Move past
            else { pb.Value = value; pb.Value = value - 1; } //Special case (can't set value > Maximum)
            pb.Value = value;
        }

        [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "SendMessageA", ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Ansi, SetLastError = true)]
        private static extern int SendMessage(IntPtr hwnd, int wMsg, int wParam, int lParam);
        private const int WM_SETREDRAW = 0xB;

        public static void SuspendDrawing(this Control target)
        {
            target.SuspendLayout();
            try { SendMessage(target.Handle, WM_SETREDRAW, 0, 0); } catch (Exception) { }
        }

        public static void ResumeDrawing(this Control target)
        {
            try { SendMessage(target.Handle, WM_SETREDRAW, 1, 0); } catch (Exception) { }
            target.ResumeLayout();
            target.Refresh();
        }
    }
}
