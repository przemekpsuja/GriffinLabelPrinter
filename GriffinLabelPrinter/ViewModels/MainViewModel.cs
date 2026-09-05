using GryfLabelManager.Helpers;
using GryfLabelManager.Models;
using GryfLabelManager.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Wpf.Ui.Input;

namespace GryfLabelManager.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly ISymfoniaService _symfoniaService;
        private readonly IPrinterService _printerService;

        public MainViewModel(ISymfoniaService symfoniaService, IPrinterService printerService)
        {
            _symfoniaService = symfoniaService;
            _printerService = printerService;

            Documents = new ObservableCollection<DocumentHeader>();
            Items = new ObservableCollection<LabelItem>();

            SwitchModeCommand = new AsyncRelayCommand(async param => await SwitchModeAsync((ViewMode)param));
            DodajRecznieCommand = new RelayCommands(_ => DodajReczniePozycje(), _ => !string.IsNullOrWhiteSpace(RecznyKod));
            UsunPozycjeCommand = new RelayCommands(param => Items.Remove((LabelItem)param));
            DrukujCommand = new RelayCommands(_ => Drukuj(), _ => Items.Any(i => i.IsSelected));

            // Domyślny tryb startowy
            _ = SwitchModeAsync(ViewMode.Dokumenty);
        }

        // ---------- Przełącznik trybu ----------

        private ViewMode _currentMode;
        public ViewMode CurrentMode
        {
            get => _currentMode;
            set { _currentMode = value; OnPropertyChanged(); }
        }

        public System.Windows.Input.ICommand SwitchModeCommand { get; }

        private async Task SwitchModeAsync(ViewMode mode)
        {
            CurrentMode = mode;
            Items.Clear();

            switch (mode)
            {
                case ViewMode.Dokumenty:
                    if (Documents.Count == 0)
                    {
                        var docs = await _symfoniaService.GetRecentDocumentsAsync();
                        Documents = new ObservableCollection<DocumentHeader>(docs);
                        OnPropertyChanged(nameof(Documents));
                    }
                    break;

                case ViewMode.WszystkieTowary:
                    var products = await _symfoniaService.GetAllProductsAsync();
                    foreach (var p in products) Items.Add(p);
                    break;

                case ViewMode.Reczny:
                    // pusta siatka - użytkownik dodaje pozycje ręcznie
                    break;
            }
        }

        // ---------- Tryb: Dokumenty PZ/PW ----------

        public ObservableCollection<DocumentHeader> Documents { get; private set; }

        private DocumentHeader _selectedDocument;
        public DocumentHeader SelectedDocument
        {
            get => _selectedDocument;
            set
            {
                _selectedDocument = value;
                OnPropertyChanged();
                // Wybór dokumentu w ListBoxie od razu wczytuje jego pozycje do wspólnej siatki
                _ = LoadDocumentItemsAsync(value);
            }
        }

        private async Task LoadDocumentItemsAsync(DocumentHeader doc)
        {
            if (doc == null) return;
            SelectedDocument = doc;
            Items.Clear();
            var pozycje = await _symfoniaService.GetDocumentItemsAsync(doc.Id);
            foreach (var p in pozycje) Items.Add(p);
        }

        // ---------- Tryb: Ręczny wpis ----------

        private string _recznyKod;
        public string RecznyKod
        {
            get => _recznyKod;
            set { _recznyKod = value; OnPropertyChanged(); }
        }

        private string _recznyNazwa;
        public string RecznyNazwa
        {
            get => _recznyNazwa;
            set { _recznyNazwa = value; OnPropertyChanged(); }
        }

        private int _recznaIlosc = 1;
        public int RecznaIlosc
        {
            get => _recznaIlosc;
            set { _recznaIlosc = value < 1 ? 1 : value; OnPropertyChanged(); }
        }

        public System.Windows.Input.ICommand DodajRecznieCommand { get; }

        private void DodajReczniePozycje()
        {
            Items.Add(new LabelItem
            {
                Kod = RecznyKod?.Trim(),
                Nazwa = RecznyNazwa?.Trim(),
                Ilosc = RecznaIlosc,
                IsSelected = true,
                IsManual = true
            });

            // czyścimy formularz pod kolejny wpis
            RecznyKod = string.Empty;
            RecznyNazwa = string.Empty;
            RecznaIlosc = 1;
        }

        // ---------- Wspólne dla wszystkich trybów ----------

        public ObservableCollection<LabelItem> Items { get; }

        public System.Windows.Input.ICommand UsunPozycjeCommand { get; }
        public System.Windows.Input.ICommand DrukujCommand { get; }

        private void Drukuj()
        {
            var doWydruku = Items.Where(i => i.IsSelected && i.Ilosc > 0).ToList();
            if (doWydruku.Count == 0)
            {
                MessageBox.Show("Zaznacz co najmniej jedną pozycję do wydruku.", "GryfLabelManager",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            _printerService.Print(doWydruku);
        }
    }
}
