using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace SimpleMJPEGStreamViewer {
    public partial class MainForm : Form {

        readonly SimpleBindingList<VideoItem> videoList = new SimpleBindingList<VideoItem>();
        readonly SynchronizationContext sync;

        const string defaultCamFile = "default.xml";

        public MainForm() {
            InitializeComponent();
            sync = SynchronizationContext.Current;

            dataGridView1.AutoGenerateColumns = false;

            IEnumerable<DataGridViewRow> sel = null;

            dataGridView1.SaveSelection = columnIndex => {
                sel = columnIndex == PlayingColumn.Index || columnIndex == VisibleColumn.Index ? dataGridView1.SelectedRows.OfType<DataGridViewRow>() : null;
            };

            dataGridView1.RestoreSelection += () => {
                if(sel != null) {
                    foreach(var row in sel)
                        row.Selected = true;
                }
            };

            dataGridView1.SelectionChanged += (s, e) => {
                propertyGrid1.SelectedObjects = selectedItems.ToArray();
            };

            dataGridView1.DataSource = videoList;
            videoList.ListChanged += VideoList_ListChanged;
            videoList.BeforeRemove += VideoList_BeforeRemove;

            if(File.Exists(defaultCamFile)) {
                loadCams(defaultCamFile);
            }

            this.Disposed += (s, e) => {
                clearVideoList(); ;
            };
        }

        private void VideoList_BeforeRemove(object sender, VideoItem item) {
            item.Playing = false;
            var ctl = simpleLayoutPanel1.Controls[item.UUID.ToString()];
            if(ctl != null)
                ctl.Dispose();
            item.Dispose();
        }

        void adaptProperties(VideoItem item, string propertyName = null) {
            var byPass = propertyName == null;
            if(propertyName == "Visible" || byPass) {
                simpleLayoutPanel1.Controls[item.UUID.ToString()].Visible = item.Visible;
            }

            if(propertyName == "Playing" || byPass) {
                if(item.Playing) {
                    var pb = (SimplePictureBox)simpleLayoutPanel1.Controls[item.UUID.ToString()];
                    Task.Run(() => startVideoAsync(pb, item));
                }
            }
        }

        void VideoList_ListChanged(object sender, ListChangedEventArgs e) {
            if(e.ListChangedType == ListChangedType.ItemAdded) {
                var item = (VideoItem)((IList)sender)[e.NewIndex];
                var pb = new SimplePictureBox { Name = item.UUID.ToString() };
                simpleLayoutPanel1.Controls.Add(pb);
                adaptProperties(item);
            }
            else if(e.ListChangedType == ListChangedType.ItemChanged) {
                var item = (VideoItem)((IList)sender)[e.NewIndex];
                adaptProperties(item, e.PropertyDescriptor.Name);
            }
        }

        async Task startVideoAsync(SimplePictureBox pb, VideoItem item) {
            try {
                await SimpleMJPEGDecoder.StartAsync(
                    image => {
                        sync.Post(new SendOrPostCallback(_ => pb.Image = image), null);
                    },
                    item.Url,
                    item.Login,
                    item.Password,
                    item.Token,
                    item.MaxStreamBufferSize);
            }
            catch(OperationCanceledException ex) {
                Console.WriteLine(ex);
            }
            catch(Exception ex) {
                Console.WriteLine(ex);
                pb.Image = (Image)Properties.Resources.notready.Clone();
            }
            finally {
                item.Playing = false;
            }
        }

        private void toolStripButton1_Click(object sender, EventArgs e) {
            videoList.Add(new VideoItem());
        }

        IEnumerable<VideoItem> selectedItems {
            get {
                return
                    dataGridView1
                        .SelectedRows
                        .OfType<DataGridViewRow>()
                        .Select(
                            r => {
                                return (VideoItem)r.DataBoundItem;
                            });
            }
        }

        private void toolStripButton2_Click(object sender, EventArgs e) {
            foreach(var item in selectedItems) {
                videoList.Remove(item);
            }
        }

        private void toolStripButton4_Click(object sender, EventArgs e) {
            foreach(var item in selectedItems)
                item.Playing = false;
        }

        private void toolStripButton3_Click(object sender, EventArgs e) {
            foreach(var item in selectedItems.Where(v => !v.Playing)) {
                item.Playing = true;
            }
        }

        private void toolStripButton5_Click(object sender, EventArgs e) {
            foreach(var item in selectedItems) {
                item.Visible = true;
            }
        }

        private void toolStripButton6_Click(object sender, EventArgs e) {
            foreach(var item in selectedItems)
                item.Visible = false;
        }

        private void toolStripButton7_Click(object sender, EventArgs e) {
            foreach(var item in selectedItems)
                videoList.Add(new VideoItem {
                    Visible = item.Visible,
                    Name = item.Name,
                    Url = item.Url,
                    Playing = item.Playing,
                    Login = item.Login,
                    Password = item.Password
                });
        }

        private void toolStripButton8_Click(object sender, EventArgs e) {
            singleView();
        }

        private void singleView() {
            foreach(var item in dataGridView1.Rows.OfType<DataGridViewRow>().Select(d => new { row = d, vid = (VideoItem)d.DataBoundItem }))
                item.vid.Visible = item.row.Selected;
        }

        private void toolStripButton9_Click(object sender, EventArgs e) {
            var row = dataGridView1.SelectedRows.OfType<DataGridViewRow>().FirstOrDefault();
            if(row == null)
                return;

            var nextRow = dataGridView1.Rows.GetPreviousRow(row.Index, DataGridViewElementStates.None);
            if(nextRow == -1)
                return;

            dataGridView1.ClearSelection();
            dataGridView1.Rows[nextRow].Selected = true;

            foreach(var item in dataGridView1.Rows.OfType<DataGridViewRow>().Select(d => new { row = d, vid = (VideoItem)d.DataBoundItem }))
                item.vid.Visible = item.row.Selected;
        }

        private void toolStripButton10_Click(object sender, EventArgs e) {
            var row = dataGridView1.SelectedRows.OfType<DataGridViewRow>().FirstOrDefault();
            if(row == null)
                return;

            var nextRow = dataGridView1.Rows.GetNextRow(row.Index, DataGridViewElementStates.None);
            if(nextRow == -1)
                return;

            dataGridView1.ClearSelection();
            dataGridView1.Rows[nextRow].Selected = true;

            foreach(var item in dataGridView1.Rows.OfType<DataGridViewRow>().Select(d => new { row = d, vid = (VideoItem)d.DataBoundItem }))
                item.vid.Visible = item.row.Selected;
        }

        private void dataGridView1_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e) {
            if(e.ColumnIndex == PlayingColumn.Index) {
                e.Value = (bool)e.Value ? "4" : ";";
                e.FormattingApplied = true;
            }
            else if(e.ColumnIndex == VisibleColumn.Index) {
                e.Value = (bool)e.Value ? "ü" : "û";
                e.FormattingApplied = true;
            }
        }

        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e) {
            if((e.ColumnIndex == PlayingColumn.Index || e.ColumnIndex == VisibleColumn.Index) && e.RowIndex != -1) {
                var value = (bool)((DataGridView)sender)[e.ColumnIndex, e.RowIndex].Value;
                foreach(var sel in dataGridView1.SelectedRows.OfType<DataGridViewRow>()) {
                    sel.Cells[e.ColumnIndex].Value = !value;
                }
            }
        }

        static readonly XmlSerializer s = new XmlSerializer(typeof(VideoItem[]));

        private void saveToolStripMenuItem_Click(object sender, EventArgs e) {
            using(var fileSave = new SaveFileDialog()) {
                if(fileSave.ShowDialog() == DialogResult.OK) {
                    using(var f = new FileStream(fileSave.FileName, FileMode.Create))
                        s.Serialize(f, videoList.ToArray());
                }
            }
        }

        private void loadToolStripMenuItem_Click(object sender, EventArgs e) {
            using(var fileLoad = new OpenFileDialog()) {
                if(fileLoad.ShowDialog() == DialogResult.OK) {
                    var camFile = fileLoad.FileName;
                    loadCams(camFile);
                }
            }
        }

        void clearVideoList() {
            foreach(var item in videoList.ToList()) {
                videoList.Remove(item);
            }
        }

        private void loadCams(string camFile) {
            using(var f = new FileStream(camFile, FileMode.Open)) {
                var loadedItems = (VideoItem[])s.Deserialize(f);
                clearVideoList();
                foreach(var item in loadedItems) {
                    videoList.Add(item);
                }
            }
        }

        private void dataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e) {
            singleView();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e) {
            this.Close();
        }
    }
}
