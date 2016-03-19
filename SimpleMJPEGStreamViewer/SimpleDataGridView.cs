using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SimpleMJPEGStreamViewer {
    class SimpleDataGridView : DataGridView {

        public Action<int> SaveSelection;
        public Action RestoreSelection;

        protected override void OnCellMouseDown(DataGridViewCellMouseEventArgs e) {
            if(SaveSelection != null)
                SaveSelection(e.ColumnIndex);

            base.OnCellMouseDown(e);

            if(RestoreSelection != null)
                RestoreSelection();
        }

        protected override void OnCellDoubleClick(DataGridViewCellEventArgs e) {
            if(SaveSelection != null)
                SaveSelection(e.ColumnIndex);

            base.OnCellDoubleClick(e);

            if(RestoreSelection != null)
                RestoreSelection();
        }
    }
}
