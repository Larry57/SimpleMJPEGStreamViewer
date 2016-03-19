using System;
using System.ComponentModel;

namespace SimpleMJPEGStreamViewer {
    public class SimpleBindingList<T> : BindingList<T> {
        public event Action<Object, T> BeforeRemove;
        protected override void RemoveItem(int itemIndex) {
            if(BeforeRemove != null) {
                BeforeRemove(this, this.Items[itemIndex]);
            }
            base.RemoveItem(itemIndex);
        }
    }
}
