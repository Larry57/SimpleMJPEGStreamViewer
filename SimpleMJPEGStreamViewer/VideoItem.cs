using System;
using System.ComponentModel;
using System.Threading;
using System.Xml.Serialization;

namespace SimpleMJPEGStreamViewer {
    public class VideoItem : INotifyPropertyChanged, IDisposable {

        public event PropertyChangedEventHandler PropertyChanged;

        CancellationTokenSource cts;

        public VideoItem() {
            UUID = Guid.NewGuid();
            Playing = false;
            Name = "Unnamed";
            MaxStreamBufferSize = 1024;
        }

        string login;
        public string Login {
            get {
                return login;
            }
            set {
                if(login == value)
                    return;

                login = value;

                OnPropertyChanged(new PropertyChangedEventArgs(nameof(Login)));
            }
        }

        string password;
        public string Password {
            get {
                return password;
            }
            set {
                if(password == value)
                    return;

                password = value;

                OnPropertyChanged(new PropertyChangedEventArgs(nameof(Password)));
            }
        }

        string name;
        public string Name {
            get {
                return name;
            }
            set {
                if(name == value)
                    return;

                name = value;

                OnPropertyChanged(new PropertyChangedEventArgs(nameof(Name)));
            }
        }

        string url;
        public string Url {
            get {
                return url;
            }
            set {
                if(url == value)
                    return;

                url = value;

                OnPropertyChanged(new PropertyChangedEventArgs(nameof(Url)));
            }
        }

        bool status;
        public bool Playing {
            get {
                return status;
            }
            set {
                if(status == value)
                    return;

                status = value;

                if(value) {
                    cts = new CancellationTokenSource();
                }
                else {
                    cts.Cancel();
                    cts.Dispose();
                }

                OnPropertyChanged(new PropertyChangedEventArgs(nameof(Playing)));
            }
        }

        [XmlIgnore]
        [Browsable(false)]
        public CancellationToken Token {
            get {
                return cts.Token;
            }
        }

        public readonly Guid UUID;

        bool visible;
        public bool Visible {
            get {
                return visible;
            }
            set {
                if(visible == value)
                    return;

                visible = value;

                OnPropertyChanged(new PropertyChangedEventArgs(nameof(Visible)));
            }
        }

        public int MaxStreamBufferSize { get; set; }

        protected void OnPropertyChanged(PropertyChangedEventArgs e) {
            if(PropertyChanged != null) {
                var handler = PropertyChanged;
                if(handler != null)
                    handler(this, e);
            }
        }

        public void Dispose() {
            this.cts.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
