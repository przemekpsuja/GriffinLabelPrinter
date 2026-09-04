using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Threading;
using GryfLabelManager.Helpers;
using GryfLabelManager.Models;
using GryfLabelManager.Services;

namespace GryfLabelManager.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly ISymfoniaService _symfoniaService;
        private readonly DispatcherTimer _searchDebounceTimer;

        // Pełna lista towarów wczytana z serwisu (CSV lub docelowo SQL).
        public ObservableCollection<LabelItem> Items { get; } = new();

        // Widok nad Items, który obsługuje filtrowanie bez przeładowywania danych.
        // ListView w XAML binduje się do TEGO, nie bezpośrednio do Items.
        public ICollectionView ItemsView { get; }

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                // Debounce: nie odświeżamy filtra przy każdym znaku, tylko po 250ms ciszy.
                // Przy dużej liście (kilka tysięcy pozycji) zapobiega to laggom podczas pisania.
                _searchDebounceTimer.Stop();
                _searchDebounceTimer.Start();
            }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        public RelayCommand DrukujCommand { get; }
        public RelayCommand ZaznaczWszystkoCommand { get; }

        public MainViewModel(ISymfoniaService symfoniaService)
        {
            _symfoniaService = symfoniaService;

            ItemsView = CollectionViewSource.GetDefaultView(Items);
            ItemsView.Filter = FilterItem;

            _searchDebounceTimer = new DispatcherTimer { Interval = System.TimeSpan.FromMilliseconds(250) };
            _searchDebounceTimer.Tick += (s, e) =>
            {
                _searchDebounceTimer.Stop();
                ItemsView.Refresh();
            };

            DrukujCommand = new RelayCommand(_ => Drukuj(), _ => Items.Any(i => i.IsSelected));
            ZaznaczWszystkoCommand = new RelayCommand(_ => ZaznaczWszystkoWidoczne());

            _ = LoadDataAsync(); // fire-and-forget na starcie; UI pokazuje IsLoading w międzyczasie
        }

        private async Task LoadDataAsync()
        {
            IsLoading = true;
            try
            {
                var towary = await _symfoniaService.GetItemsAsync();
                Items.Clear();
                foreach (var item in towary)
                    Items.Add(item);
            }
            finally
            {
                IsLoading = false;
            }
        }

        // Filtr: ignoruje wielkość liter i WSZYSTKIE białe znaki (spacje, taby),
        // żeby np. "Karnet24godz" znalazło "Karnet 24-godz." - przydatne przy
        // wyszukiwaniu z ręki na magazynie, gdzie nikt nie wpisuje idealnie.
        private bool FilterItem(object obj)
        {
            if (string.IsNullOrWhiteSpace(SearchText)) return true;
            if (obj is not LabelItem item) return false;

            return Normalize(item.Kod).Contains(Normalize(SearchText))
                || Normalize(item.Nazwa).Contains(Normalize(SearchText));
        }

        private static string Normalize(string s) =>
            Regex.Replace(s, @"\s+", "").ToLowerInvariant();

        // Zaznacza tylko to, co aktualnie widoczne po filtrze (nie całą, niewidoczną resztę listy).
        private void ZaznaczWszystkoWidoczne()
        {
            foreach (var obj in ItemsView.Cast<LabelItem>())
                obj.IsSelected = true;
        }

        private void Drukuj()
        {
            var doWydruku = Items.Where(i => i.IsSelected).ToList();
            // TODO Faza 2 (BrotherBpacService): tu podpięcie wysyłki do drukarki.
            // Na razie zostawiamy jako stub - kolejny krok po zamknięciu warstwy danych.
        }
    }
}