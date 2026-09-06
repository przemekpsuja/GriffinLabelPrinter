using GryfLabelManager.Helpers;
using GryfLabelManager.Models;
using GryfLabelManager.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.ComponentModel;
using System.Windows.Data;
using Wpf.Ui.Input;

namespace GryfLabelManager.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly ISymfoniaService _symfoniaService;
        private readonly IProductCatalogService _productCatalogService;
        private readonly IPrinterService _printerService;

        // cache nieprzefiltrowanej listy towarów - potrzebne do wyszukiwarki,
        // żeby nie odpytywać CSV/SQL przy każdym wpisanym znaku
        private List<LabelItem> _allProducts = new List<LabelItem>();

        // Widok "podsumowania" - te same obiekty LabelItem co w Items,
        // ale przefiltrowane do samych zaznaczonych (IsSelected == true).
        private readonly CollectionViewSource _selectedItemsSource;

        // To jest to, co zbindujesz w XAML jako drugą siatkę na dole okna.
        public ICollectionView SelectedItems => _selectedItemsSource.View;

        // Licznik zaznaczonych - do wyświetlenia w nagłówku panelu.
        public int SelectedCount => Items.Count(i => i.IsSelected);

        public MainViewModel(ISymfoniaService symfoniaService, IProductCatalogService productCatalogService, IPrinterService printerService)
        {
            _symfoniaService = symfoniaService;
            _productCatalogService = productCatalogService;
            _printerService = printerService;

            Documents = new ObservableCollection<DocumentHeader>();
            Items = new ObservableCollection<LabelItem>();

            _selectedItemsSource = new CollectionViewSource { Source = Items };
            _selectedItemsSource.Filter += (s, e) => e.Accepted = e.Item is LabelItem item && item.IsSelected;

            // Live filtering - widok sam się przelicza przy zmianie IsSelected
            // na dowolnym LabelItem (bez tego trzeba by ręcznie wołać Refresh()
            // po każdym kliknięciu checkboxa "Drukuj" w głównej siatce).
            if (_selectedItemsSource.View is ICollectionViewLiveShaping liveShaping)
            {
                liveShaping.IsLiveFiltering = true;
                liveShaping.LiveFilteringProperties.Add(nameof(LabelItem.IsSelected));
            }

            // Gdy filtr coś doda/usunie z widoku, odśwież licznik w nagłówku.
            _selectedItemsSource.View.CollectionChanged += (s, e) => OnPropertyChanged(nameof(SelectedCount));
            SwitchModeCommand = new AsyncRelayCommand(async param => await SwitchModeAsync((ViewMode)param));
            RefreshCommand = new AsyncRelayCommand(async _ => await RefreshCurrentModeAsync());
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
        public System.Windows.Input.ICommand RefreshCommand { get; }

        private async Task SwitchModeAsync(ViewMode mode)
        {
            CurrentMode = mode;
            Items.Clear();

            switch (mode)
            {
                case ViewMode.Dokumenty:
                    // przy zwykłym przełączeniu trybu korzystamy z cache, jeśli już wczytany raz
                    if (Documents.Count == 0)
                        await LoadDocumentsAsync();
                    break;

                case ViewMode.WszystkieTowary:
                    await LoadAllProductsAsync();
                    break;

                case ViewMode.Reczny:
                    // pusta siatka - użytkownik dodaje pozycje ręcznie
                    break;
            }
        }

        /// <summary>
        /// Przycisk "Odśwież" - wymusza ponowne pobranie danych z Symfonii
        /// dla aktualnie aktywnego trybu (Dokumenty albo Wszystkie towary).
        /// W trybie Ręcznym nic nie robi, bo nie ma tam danych z bazy.
        /// </summary>
        private async Task RefreshCurrentModeAsync()
        {
            switch (CurrentMode)
            {
                case ViewMode.Dokumenty:
                    await LoadDocumentsAsync();
                    // po odświeżeniu listy dokumentów siatka pozycji też traci sens - czyścimy
                    SelectedDocument = null;
                    Items.Clear();
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
            Items.Clear();
            SearchText = string.Empty; // czyścimy filtr przy odświeżeniu/wejściu do zakładki
            _allProducts = await _productCatalogService.GetAllProductsAsync();
            foreach (var p in _allProducts) Items.Add(p);
        }

        // ---------- Wyszukiwarka (tryb: Wszystkie towary) ----------

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                ApplySearchFilter();
            }
        }

        /// <summary>
        /// Filtruje po Kod i Nazwa, ignorując wielkość liter oraz białe znaki
        /// (spacje, tabulatory) zarówno we frazie szukanej, jak i w danych -
        /// dzięki temu np. "0008 1106" znajdzie "0008110661N".
        /// </summary>
        private void ApplySearchFilter()
        {
            if (CurrentMode != ViewMode.WszystkieTowary) return;

            Items.Clear();
            var query = Normalize(_searchText);

            var filtered = string.IsNullOrEmpty(query)
                ? _allProducts
                : _allProducts.Where(p =>
                    Normalize(p.Kod).Contains(query) ||
                    Normalize(p.Nazwa).Contains(query));

            foreach (var p in filtered) Items.Add(p);
        }

        private static string Normalize(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            // usuwamy wszystkie białe znaki i sprowadzamy do wielkich liter
            return new string(value.Where(c => !char.IsWhiteSpace(c)).ToArray()).ToUpperInvariant();
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