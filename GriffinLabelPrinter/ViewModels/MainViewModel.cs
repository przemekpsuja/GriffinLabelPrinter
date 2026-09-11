using GryfLabelManager.Helpers;
using GryfLabelManager.Models;
using GryfLabelManager.Services;
using System.Collections.Generic;
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
        private readonly IProductCatalogService _productCatalogService;
        private readonly IPrinterService _printerService;

        private List<LabelItem> _allProducts = new List<LabelItem>();

        public MainViewModel(ISymfoniaService symfoniaService, IProductCatalogService productCatalogService, IPrinterService printerService)
        {
            _symfoniaService = symfoniaService;
            _productCatalogService = productCatalogService;
            _printerService = printerService;

            Documents = new ObservableCollection<DocumentHeader>();
            BrowseItems = new ObservableCollection<LabelItem>();
            PrintQueue = new ObservableCollection<LabelItem>(); // <- NOWE: trwała lista do druku, niezależna od trybu

            SwitchModeCommand = new AsyncRelayCommand(async param => await SwitchModeAsync((ViewMode)param));
            RefreshCommand = new AsyncRelayCommand(async _ => await RefreshCurrentModeAsync());
            DodajRecznieCommand = new RelayCommands(_ => DodajReczniePozycje(), _ => !string.IsNullOrWhiteSpace(RecznyKod));
            DodajZaznaczoneCommand = new RelayCommands(_ => DodajZaznaczoneDoWydruku(), _ => ZrodloZaznaczen().Any(i => i.IsSelected));
            UsunZKolejkiCommand = new RelayCommands(param => PrintQueue.Remove((LabelItem)param)); // <- zastępuje UsunPozycjeCommand
            DrukujCommand = new RelayCommands(_ => Drukuj(), _ => PrintQueue.Any());

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
        public System.Windows.Input.ICommand RefreshCommand { get; }

        private async Task SwitchModeAsync(ViewMode mode)
        {
            CurrentMode = mode;
            BrowseItems.Clear(); // czyścimy TYLKO listę przeglądania, PrintQueue zostaje nietknięta

            switch (mode)
            {
                case ViewMode.Dokumenty:
                    if (Documents.Count == 0)
                        await LoadDocumentsAsync();
                    break;

                case ViewMode.WszystkieTowary:
                    await LoadAllProductsAsync();
                    break;

                case ViewMode.Reczny:
                    break;
            }
        }

        private async Task RefreshCurrentModeAsync()
        {
            switch (CurrentMode)
            {
                case ViewMode.Dokumenty:
                    await LoadDocumentsAsync();
                    SelectedDocument = null;
                    BrowseItems.Clear();
                    break;

                case ViewMode.WszystkieTowary:
                    await LoadAllProductsAsync();
                    break;
            }
        }

        private async Task LoadDocumentsAsync()
        {
            var docs = await _symfoniaService.GetRecentDocumentsAsync();
            Documents = new ObservableCollection<DocumentHeader>(docs);
            OnPropertyChanged(nameof(Documents));
        }

        private async Task LoadAllProductsAsync()
        {
            BrowseItems.Clear();
            SearchText = string.Empty;
            _allProducts = await _productCatalogService.GetAllProductsAsync();
            foreach (var p in _allProducts) BrowseItems.Add(p);
        }

        // ---------- Wyszukiwarka (tryb: Wszystkie towary) ----------

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); ApplySearchFilter(); }
        }

        private void ApplySearchFilter()
        {
            if (CurrentMode != ViewMode.WszystkieTowary) return;

            BrowseItems.Clear();
            var query = Normalize(_searchText);

            var filtered = string.IsNullOrEmpty(query)
                ? _allProducts
                : _allProducts.Where(p =>
                    Normalize(p.Kod).Contains(query) ||
                    Normalize(p.Nazwa).Contains(query));

            foreach (var p in filtered) BrowseItems.Add(p);
        }

        private static string Normalize(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return new string(value.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
        }

        // ---------- Tryb: Dokumenty PZ/PW ----------

        public ObservableCollection<DocumentHeader> Documents { get; private set; }

        private DocumentHeader _selectedDocument;
        public DocumentHeader SelectedDocument
        {
            get => _selectedDocument;
            set { _selectedDocument = value; OnPropertyChanged(); _ = LoadDocumentItemsAsync(value); }
        }

        private async Task LoadDocumentItemsAsync(DocumentHeader doc)
        {
            if (doc == null) return;
            BrowseItems.Clear();
            var pozycje = await _symfoniaService.GetDocumentItemsAsync(doc.Id);
            foreach (var p in pozycje) BrowseItems.Add(p);
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
            // Ręczny wpis trafia od razu do kolejki druku - nie ma etapu "przeglądania"
            DodajDoKolejki(new LabelItem
            {
                Kod = RecznyKod?.Trim(),
                Nazwa = RecznyNazwa?.Trim(),
                Ilosc = RecznaIlosc,
                IsManual = true
            });

            RecznyKod = string.Empty;
            RecznyNazwa = string.Empty;
            RecznaIlosc = 1;
        }

        // ---------- Górna siatka (przeglądanie: dokument / kartoteka) ----------

        public ObservableCollection<LabelItem> BrowseItems { get; }

        public System.Windows.Input.ICommand DodajZaznaczoneCommand { get; }

        private void DodajZaznaczoneDoWydruku()
        {
            foreach (var item in ZrodloZaznaczen().Where(i => i.IsSelected).ToList())
            {
                DodajDoKolejki(new LabelItem
                {
                    Kod = item.Kod,
                    Nazwa = item.Nazwa,
                    Ilosc = item.Ilosc,
                    IsManual = item.IsManual
                });
                item.IsSelected = false;
            }
        }

        /// <summary>
        /// Dodaje pozycję do kolejki druku. Jeśli Kod już tam jest - sumuje ilość
        /// zamiast tworzyć duplikat wiersza.
        /// </summary>
        private void DodajDoKolejki(LabelItem item)
        {
            var istniejacy = PrintQueue.FirstOrDefault(i => i.Kod == item.Kod);
            if (istniejacy != null)
                istniejacy.Ilosc += item.Ilosc;
            else
                PrintQueue.Add(item);
        }

        // ---------- Dolna siatka: kolejka do wydruku (trwała, wspólna dla wszystkich trybów) ----------

        public ObservableCollection<LabelItem> PrintQueue { get; }

        public System.Windows.Input.ICommand UsunZKolejkiCommand { get; }
        public System.Windows.Input.ICommand DrukujCommand { get; }

        private void Drukuj()
        {
            if (PrintQueue.Count == 0)
            {
                MessageBox.Show("Dodaj co najmniej jedną pozycję do wydruku.", "GryfLabelManager",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            _printerService.Print(PrintQueue.ToList());
        }

        /// <summary>
        /// Źródło do sprawdzania/dodawania zaznaczeń. W trybie WszystkieTowary patrzymy
        /// na _allProducts (cała kartoteka w pamięci), nie na BrowseItems - inaczej
        /// zaznaczenie towaru "gubiłoby się" po zmianie frazy w wyszukiwarce, mimo że
        /// w pamięci wciąż jest zaznaczony (BrowseItems to tylko przefiltrowany widok).
        /// W pozostałych trybach BrowseItems i tak pokazuje wszystko, co jest dostępne.
        /// </summary>
        private IEnumerable<LabelItem> ZrodloZaznaczen()
            => CurrentMode == ViewMode.WszystkieTowary ? _allProducts : BrowseItems;
    }
}