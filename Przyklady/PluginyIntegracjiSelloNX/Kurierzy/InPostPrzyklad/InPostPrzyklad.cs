using InsERT.Moria.HandelElektroniczny;
using InsERT.Moria.HandelElektroniczny.Rozszerzenia;
using InsERT.Moria.HandelElektroniczny.Rozszerzenia.Kurierzy;
using InsERT.Moria.HandelElektroniczny.Rozszerzenia.UI;
using InsERT.Moria.Klienci;
using InsERT.Moria.Rozszerzanie;
using System;
using System.Collections.Generic;
using System.Net;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using InPostPrzyklad.DTO;
using System.Linq;
using InsERT.Moria.ModelDanych;

namespace InPostPrzyklad
{
    public class InPostPrzyklad : IIntegracjaKuriera
    {
        private readonly Lazy<ObslugaAutoryzacjiUI> _obslugaKonta;
        private readonly Lazy<ObslugaKontaIntegracji> _obslugaKontaIntegracji;

        /// <summary>
        /// Zalecamy korzystanie z klasy HttpClient do łączenia się z serwisami zewnętrznymi.
        /// W przyszłości planujemy aby pluginy integracji mogły pozyskiwać tę klasę z Sello NX, dzięki czemu będzie np. zapewnione działanie logowania komunikacji
        /// </summary>
        private readonly HttpClient _httpClient;

        /// <summary>
        /// Adres uri API kuriera wskazuje na testowy serwis API ShipX, do którego można zalogować się pod adresem https://sandbox-manager.paczkomaty.pl/auth/login
        /// Stamtąd też należy pozyskać dane pozwalające dodać konto tej przykładowej integracji.
        /// </summary>
        public const string UriString = "https://sandbox-api-shipx-pl.easypack24.net";

        public InPostPrzyklad()
        {
            _obslugaKonta = new Lazy<ObslugaAutoryzacjiUI>(() => new ObslugaAutoryzacjiUI());
            _obslugaKontaIntegracji = new Lazy<ObslugaKontaIntegracji>(() => new ObslugaKontaIntegracji());
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(UriString);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("pl-PL"));
        }

        // Interfejs bazowy plugina nexo, zawiera podstawowe informacje dotyczące plugina systemu nexo. Należy je uzupełnić odpowiednio do potrzeb.
        // Dane te będą prezentowane użytkownikowi w systemie nexo
        #region IFunkcja

        /// <summary>
        /// Unikalny identyfikator pluginu nexo. Każdy nowy plugin musi zawierać tutaj nowy wygenerowany GUID.
        /// Jeśli planujesz opublikować swój plugin na podstawie tego przykładu, koniecznie wygeneruj tutaj nową wartość, np. za pomocą generatorów online.
        /// </summary>
        public Guid Identyfikator => new Guid("94C8C7DB-DE59-45F7-B2E5-58178E311122");

        /// <summary>
        /// Nazwa plugina, będąca zarazem nazwą integracji.
        /// Podaj tutaj nazwę firmy kurierskiej, z którą plugin współpracuje. Nazwa ta wyświetlana jest użytkownikowi w postaci kolorowego stempelka 
        /// identyfikującego dostawcę paczek. Zadbaj o to, aby nazwa była krótka a wielkość liter odpowiadała realnej nazwie jaką stosuje firma kurierska.
        /// Dobre nazwy to: DPD, InPost, Poczta Polska, Pocztex, DHL, itp.
        /// </summary>
        public string Nazwa => "InPost przykład";

        /// <summary>
        /// Krótki opis plugina. informacje te mogą być widoczne dla użytkownika programu nexo.
        /// </summary>

        public string Opis => "Przykładowa integracja kurierska z testowym serwisem InPost API ShipX";
        /// <summary>
        /// Informacje o dostawcy plugina (autorze). Informacje te mogą być widoczne dla użytkownika programu nexo.
        /// Uzupełnij je w pliku <code>DostawcaPluginow.cs</code>.
        /// </summary>
        public IDostawcaPluginow Dostawca => new DostawcaPluginow();
        #endregion IFunkcja

        // Interfejs bazowy plugina integracji Sello NX, zawiera podstawowe informacje na temat kuriera (m.in. kolory identyfikujące)
        #region IIntegracja
        /// <summary>
        /// Trzyliterowy symbol integracji, widoczny w module konta integracji, wykorzystywany w module numeracja w nexo jako składnik numeru.
        /// Zadbaj o to, aby symbol ten kojarzył się z firmą kurierską.
        /// Dobre nazwy to: DPD, INP (InPost), PPL (Poczta Polska), PCX (Pocztex), DHL itp.
        /// </summary>
        public string Symbol => "INP";

        /// <summary>
        /// Numer wersji plugina integracji. Wersja ta może być widoczne dla użytkowników systemu nexo.
        /// W przyszłości pozwoli nam zrealizować system aktualizacji pluginów u użytkowników.
        /// Uwaga! Wersja w przyszłości zostanie zamieniona na klasę Version w formacie (1.0.0)
        /// </summary>
        public int Wersja => 1;

        /// <summary>
        /// Główny kolor identyfikujący integrację. Jest to kolor stempelka (tła) prezentowanego użytkownikowi w programie, na którym widoczna jest <see cref="Nazwa"/> integracji.
        /// Jeśli kolor ten będzie ustawiony na biały (#ffffff), stempelek w Sello NX będzie zawierał ramkę w kolorze tekstu (<see cref="KolorDodatkowy"/>).
        /// Zadbaj o to, aby było to pierwszy kolor, który kojarzy się z daną firmą kurierską. Jako wyznacznik weź pod uwagę kolor wyróżnień ze strony internetowej kuriera czy kolor logotypu.
        /// Identyfikacja kolorystyczna jest w Sello NX stosowana zamiast logotypów, dlatego ważne jest, aby zachować spójność ze światem zewnętrznym.
        /// Dobre kolory to: #dc0032 (czerwony, DPD), #ffcb04 (złoty, InPost), #e71905 (czerwony, Poczta Polska, Pocztex), #ffcc00 (złoty, DHL) 
        /// </summary>
        public string KolorGlowny => "#FF009900";

        /// <summary>
        /// Dodatkowy kolor identyfikujący integrację. Jest to kolor tekstu (<see cref="Nazwa"/>) stempelka prezentowanego użytkownikowi w programie.
        /// Zadbaj o to, aby kolor dodatkowy dopełniał kolor główny (<see cref="KolorGlowny"/>) tak, aby cały stempelek kojarzył się z daną firmą kurierską.
        /// Jako wyznacznik weź pod uwagę kolor dodatkowy ze strony internetowej kuriera czy kolor czcionki logotypu.
        /// Identyfikacja kolorystyczna jest w Sello NX stosowana zamiast logotypów, dlatego ważne jest, aby zachować spójność ze światem zewnętrznym.
        /// Dobre kolory to: #ffffff (biały, DPD, ponieważ szary na czerwonym jest nieczytelny), 
        /// #3c3c3c (ciemnoszary, InPost), #ffffff (biały, Poczta Polska, Pocztex), #d40511 (czerwony, DHL)
        /// </summary>
        public string KolorDodatkowy => "#FFFFFFFF";

        /// <summary>
        /// Ustawienie klasy umożliwiającej zarządzaniem kontem integracji
        /// </summary>
        public IObslugaKontaIntegracji ObslugaKontaIntegracji => _obslugaKontaIntegracji.Value;

        /// <summary>
        /// Czy integracja komunikuje się ze środowiskiem testowym kuriera?
        /// Jeśli tak, zwróć tutaj wartość <c>true</c>, dzięki czemu Sello NX wyświetli taką informację w programie, a użytkownik będzie wiedział, że 
        /// korzystanie z tego plugina integracji nie spowoduje naliczenia opłat w serwisie kurierskim z tytułu utworzenia paczki, bądź też wezwania kuriera po jej odbiór.
        /// Gdy plugin integracji przeznaczony jest do pracy produkcyjnej, zwróć <c>false</c>.
        /// </summary>
        public bool IntegracjaTestowa => true;
        #endregion IIntegracja

        //Interfejs integracji z kurierem, zawiera m.in. implementację metod wykorzystywanych przez Sello NX do komunikacji z serwisem kurierskim.
        #region IIntegracjaKuriera
        /// <summary>
        /// Guid typu obiektu wykorzystywanego do zarządzania oknem konfiguracji sposobów dostawy dla plugina.
        /// Poniżej wykorzystany własny typ 
        /// </summary>
        public DeklaracjaTypuObiektu KonfiguracjaSposobuDostawyPaczkiUI => new DeklaracjaTypuObiektu(IdyTypowWbudowanych.KonfiguracjaBazowaSposobuDostawyPaczkiKurierowUI);
        /// <summary>
        /// Ustawienie klasy obsługi autoryzacji do konta
        /// </summary>
        public IObslugaAutoryzacjiUI ObslugaAutoryzacjiUI => _obslugaKonta.Value;
        /// <summary>
        /// Obecnie nieużywane w pluginach zewnętrznych. Zwróć wartość false
        /// </summary>
        public bool RozszerzoneMapowanieDostawy => false;


        /// <summary>
        /// Metoda, której zadaniem jest zwrócenie listy usług dostawy oferowanych przez kuriera.
        /// Zadanie Sello NX wywołujące tę metodę jest przystosowane do obsługi komunikacji z serwisem zewnętrznym (pasek postępu, obsługa błędów itp)
        /// ale nie jest to wymagane i plugin może te dane zwrócić jako statyczną listę usług. 
        /// Dane zwracane z tej metody są przechowywane przez Sello NX w dedykowanej pamięci podręcznej a metoda ta jest wywoływana tylko jeśli następuje konieczność
        /// odświeżenia listy usług (np. pierwsze wejście do okna z listą usług, ręczne wywołanie odświeżenia przez użytkownika).
        /// Użytkownik programu będzie mógł wybrać jedną z tych usług wraz z opcjami dodatkowymi w konfiguracji sposobu wysyłki paczki w Sello NX.
        /// </summary>
        /// <param name="dane">Dane przekazane przez zadanie pobierania usług oferowanych przez kuriera z Sello NX, 
        /// na podstawie których plugin integracji może pobrać te usługi z serwisu kurierskiego.</param>
        /// <returns>Lista usług kurierskich w ustalonym formacie wynikowym</returns>
        public PobranieUslugWynik PobierzUslugi(PobranieUslugDane dane)
        {
            // Lista usług oferowanych przez kuriera jest w miarę stałym zbiorem więc w tym przypadku plugin nie
            // komunikuje się z serwisem aby tę listę pobrać, tylko zwraca statyczną listę wybranych usług, które będą przez plugin obsługiwane.
            // Lista usług pobranych od kuriera często zawiera dużo szerszy zakres niż ten, który faktycznie będzie używany w programie.

            List<UslugaKurierska> uslugi = new List<UslugaKurierska>();

            // Lista usług uzupełniana statycznymi danymi wg dokumentacji kuriera.
            // https://dokumentacja-inpost.atlassian.net/wiki/spaces/PL/pages/11731062/Rozmiary+i+us+ugi+dla+przesy+ek
            // Użytkownik programu będzie mógł wybrać jedną z tych usług w konfiguracji sposobu wysyłki paczki w Sello NX.
            uslugi.Add(new UslugaKurierska()
            {
                Id = "inpost_courier_standard",
                Nazwa = "Przesyłka kurierska standardowa",

                // Lista usług dodatkowych oferowanych dla danej usługi.
                // Użytkownik programu będzie mógł zaznaczyć dowolne usługi dodatkowe z poniższej listy w postaci checkboxów.
                // Są to takie usługi jak: dodatkowy SMS, e-mail, dostawa przed południem. itp.
                // WAŻNE! Nie umieszczaj tutaj usług COD (pobranie) oraz INSURANCE (ubezpieczenie), ponieważ są one traktowane 
                // indywidualnie przez Sello NX a w oknie konfiguracji sposobu dostawy są wyniesione do osobnych pól.
                UslugiDodatkowe = new List<UslugaKurierskaDodatkowa>()
                {
                    new UslugaKurierskaDodatkowa()
                    {
                        Id = "sms",
                        Nazwa = "SMS",
                        Opis = "Powiadomienie o przesyłce via SMS."
                    },
                    new UslugaKurierskaDodatkowa()
                    {
                        Id = "email",
                        Nazwa = "EMAIL",
                        Opis = "Powiadomienie o przesyłce via e-mail."
                    },
                    new UslugaKurierskaDodatkowa()
                    {
                        Id = "saturday",
                        Nazwa = "Doręczenie w sobotę",
                        Opis = "Doręczenie przesyłki w sobotę."
                    }
                },

                // Lista dostępnych metod nadania paczki dla wybranej usługi
                // https://dokumentacja-inpost.atlassian.net/wiki/spaces/PL/pages/11731047/Spos+b+nadania
                // Użytkownik programu będzie mógł wybrać jedną z tych metod
                MetodyNadania = new List<MetodaNadania>()
                {
                    new MetodaNadania()
                    {
                        IdKuriera = "dispatch_order",
                        Nazwa = "Odbiór przez Kuriera",
                        Opis = "Utworzę zelecenie odbioru - przesyłkę odbierze Kurier InPost"
                    },
                    new MetodaNadania()
                    {
                        IdKuriera = "pop",
                        Nazwa = "Nadanie w POP",
                        Opis = "Nadam przesyłkę w Punkcie Obsługi Przesyłek"
                    }
                },
            });

            // Usługi należy zapisać w strukturze wynikowej tego zadania. 
            return new PobranieUslugWynik()
            {
                Uslugi = uslugi
            };
        }

        /// <summary>
        /// Metoda, której zadaniem jest stworzenie wysyłki w serwisie kuriera
        /// </summary>
        /// <param name="dane">Dane przekazane przez zadanie stworzenia wysyłki w serwisie kuriera z Sello NX, 
        /// na podstawie których plugin integracji może taką wysyłkę utworzyć.</param>
        /// <returns>Informacje o stworzonej u kuriera wysyłce w ustalonym formacie wynikowym</returns>
        public StworzenieWysylkiWynik StworzWysylkeWSerwisie(StworzenieWysylkiDane dane)
        {
            // Tworzymy strukturę definiującą nową przesyłkę wg kuriera (w tym przypadku do InPostu)
            var wysylka = new InpostCreateShipmentDto();

            var zamowienie = dane.Paczka.Zamowienie;
            var odbiorca = zamowienie.Odbiorca;
            var sposobDostawy = zamowienie.SposobDostawy;

            // Uzupełniamy pola struktury paczki dannymi potrzebnymi do jej stworzenia

            // Dane paczki
            InpostParcelDto paczkaInpost = new InpostParcelDto();
            paczkaInpost.Template = "small";
            wysylka.Parcels = new InpostParcelDto[] { paczkaInpost };

            // Ze sposobu dostawy za pomocą metody PodajPole() sięgamy po parametry jakie ustawił użytkownik, poniżej pole z wybraną usługą
            wysylka.Service = sposobDostawy.PodajPole(PolaIntegracjiSelloStale.SposobyDostawy.Usluga);

            // Dodajemy wybrane usługi dodatkowe
            wysylka.AdditionalServices = sposobDostawy.PodajPole(PolaIntegracjiSelloStale.SposobyDostawy.UslugiDodatkowe)?.ToList() ?? new List<string>();


            wysylka.CustomAttributes = new InpostAttributesDto();

            // Ustawiamy sposób wysyłki (nadania paczki)
            wysylka.CustomAttributes.SendingMethod = sposobDostawy.PodajPole(PolaIntegracjiSelloStale.SposobyDostawy.SposobWysylki);

            // Pole z numerem referencyjnym, umożliwiające przypisanie później wygenerowanej etykiety do zamówienia wysyłkowego w Sello NX
            wysylka.Reference = zamowienie.Tytul;


            // Dane odbiorcy
            wysylka.Receiver = new InpostPeerDto();
            if (odbiorca.TypPodmiotu == (int)TypObiektu.Firma)
                wysylka.Receiver.CompanyName = odbiorca.NazwaSkrocona;
            else
            {
                wysylka.Receiver.FirstName = odbiorca.Imie;
                wysylka.Receiver.LastName = odbiorca.Nazwisko;
            }
            wysylka.Receiver.Email = zamowienie.Email;
            wysylka.Receiver.Phone = zamowienie.Telefon;

            // W zależności od typu wybranej usługi, należy przekazać różne dane
            // Kurier wymaga podania adresu dostawy, odbiór w punkcie - identyfikatora tego punktu
            if (wysylka.Service == "inpost_courier_standard")
            {
                // jeśli adres odbiorcy nie posiada szczegółów (adres mieści się w nieustrukturyzowanych liniach 1,2,3) to za pomocą klasy pomocniczej 
                // jest konwertowany na szczegóły, tak aby można było przesłać do kuriera dane w odpowiednich polach
                var adres = zamowienie.AdresOdbiorcy.Adres;
                if (adres.Szczegoly == null)
                    AddressFormatter.NaSzczegolyZLini(adres);

                var buildingNumber = adres.Szczegoly.NrDomu;
                if (!string.IsNullOrEmpty(adres.Szczegoly.NrLokalu))
                    buildingNumber += "/" + adres.Szczegoly.NrLokalu;

                wysylka.Receiver.Address = new InpostAddressDto()
                {
                    Street = adres.Szczegoly.Ulica,
                    BuildingNumber = buildingNumber,
                    City = adres.Szczegoly.Miejscowosc,
                    ZipCode = adres.Szczegoly.KodPocztowy,
                    CountryCode = adres.Panstwo.KodPanstwaUE
                };
            }

            // Uzupełnienie danych autoryzujących w żądaniu do serwisu kurierskiego
            // Uwaga! Sposób ten jest tymczasowy i w najbliższych wersjach dane autoryzacyjne będą dostarczane wprost w parametrze metody (dane.DaneUwierzytelnienia)
            PobierzDaneUwierzytelnienia(dane);

            var idOrganizacji = dane.Paczka.Zamowienie.SposobDostawy.KontoIntegracji.PodajPole(PolaIntegracjiSelloStale.NazwaKonta);

            // Wywołanie endpointa w serwisie kurierskim, pod którym zostanie stworzona przesyłka.
            // Ważne! Wywołanie asynchroniczne PostAsync() jest tutaj zamienione na synchroniczne dzięki wywołaniu .Result
            // Zadania w Sello NX są wykonywane w osobnych wątkach i należy tutaj wywoływać metody API i zwracać ich wyniki synchronicznie
            var response = _httpClient.PostAsync($"/v1/organizations/{idOrganizacji}/shipments",
                new StringContent(JsonConvert.SerializeObject(wysylka), System.Text.Encoding.UTF8, "application/json")).Result;
            var content = HandleErrorsWithContent<InpostShipmentDto>(response);


            // Zapisanie identyfikatora stworzonej u kuriera paczki
            // Uwaga! Jest to tymczasowe rozwiązanie w wersji Sello NX 49 i starszych. Będzie zmienione w jednej z kolejnych wersji
            using (IPaczkaWysylkowa paczkaWysylkowa = dane.Uchwyt.PodajObiektTypu<IPaczkiWysylkowe>().Znajdz(dane.Paczka))
            {
                paczkaWysylkowa.Dane.IdApiKuriera = content.Id;
                paczkaWysylkowa.Zapisz();
            }

            // Informacje o stworzonej przesyłkce należy zapisać w strukturze wynikowej tego zadania. 
            return new StworzenieWysylkiWynik()
            {
                Wysylka = new DaneUtworzonejWysylki()
                {
                    Id = content.Id,
                    Status = StatusMapper.MapToApiStatus(content.Status, !string.IsNullOrEmpty(content.TrackingNumber))
                }
            };
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="dane"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public AktualizacjaWysylkiWynik AktualizujWysylkeWSerwisie(AktualizacjaWysylkiDane dane)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Metoda, której zadaniem jest pobranie statusu przesyłki z serwisu kuriera.
        /// </summary>
        /// <param name="dane">Dane przekazane przez zadanie pobierania statusu wysyłki z Sello NX, 
        /// na podstawie których plugin integracji może pobrać status wysyłki z serwisu kurierskiego.</param>
        /// <returns>Status przesyłki w ustalonym formacie wynikowym</returns>
        public AktualizacjaStatusuWynik PobierzStatusWysylki(AktualizacjaStatusuDane dane)
        {
            // Uzupełnienie danych autoryzujących w żądaniu do serwisu kurierskiego
            // Uwaga! Sposób ten jest tymczasowy i w najbliższych wersjach dane autoryzacyjne będą dostarczane wprost w parametrze metody (dane.DaneUwierzytelnienia)
            PobierzDaneUwierzytelnienia(dane);
            //_httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", dane.DaneUwierzytelnienia.TokenOdswiezeniaLubHaslo);

            // Wywołanie endpointa w serwisie kurierskim, pod którym są dostępne dane paczki.
            // Ważne! Wywołanie asynchroniczne GetAsync() jest tutaj zamienione na synchroniczne dzięki wywołaniu .Result
            // Zadania w Sello NX są wykonywane w osobnych wątkach i należy tutaj wywoływać metody API i zwracać ich wyniki synchronicznie
            var response = _httpClient.GetAsync($"/v1/shipments/{dane.IdApiKuriera}").Result;
            var content = HandleErrorsWithContent<InpostShipmentDto>(response);

            // Dane pobrane z serwisu, należy zapisać w strukturze wynikowej tego zadania. 
            // W przykładzie używamy klasy dodatkowej StatusMapper, która definiuje mapowania statusów oraz opisów, dla czytelności kodu.
            return new AktualizacjaStatusuWynik()
            {
                // Status z serwisu kuriera należy zmapować na jeden ze statusów dostępnych w Sello NX
                // Statusy te są oznaczone różnymi atrybutami, które są wykorzystywane przez Sello NX do jednolitego prezentowania informacji w interfejsie
                // ale też bazują na nich różne algorytmy, dostępne funkcje itp.
                Status = StatusMapper.MapToApiStatus(content.Status, !string.IsNullOrEmpty(content.TrackingNumber)),

                // Dodatkowy opis słowny danego statusu paczki, również wyświetlany w interfejsie użytkownika.
                // Zalecamy stosowanie możliwie krótkich opisów, które wyjaśnią co dokładnie dzieje się z paczką.
                StatusSlowny = StatusMapper.GetStatusDescription(content.Status),
            };
        }

        /// <summary>
        /// Metoda, której zadaniem jest pobranie etykiety z serwisu kuriera dla konkretnej paczki
        /// </summary>
        /// <param name="dane">Dane przekazane przez zadanie pobierania etykiety wysyłki z Sello NX, 
        /// na podstawie których plugin integracji może pobrać plik etykiety z serwisu kurierskiego.</param>
        /// <returns>Etykieta nadawcza w ustalonym formacie wynikowym</returns>
        public PobranieEtykietyWynik PobierzEtykiete(PobranieEtykietyDane dane)
        {

            // Uzupełnienie danych autoryzujących w żądaniu do serwisu kurierskiego
            // Uwaga! Sposób ten jest tymczasowy i w najbliższych wersjach dane autoryzacyjne będą dostarczane wprost w parametrze metody (dane.DaneUwierzytelnienia)
            PobierzDaneUwierzytelnienia(dane);
            //_httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", dane.DaneUwierzytelnienia.TokenOdswiezeniaLubHaslo);

            // Wywołanie endpointa w serwisie kurierskim, pod którym jest dostępna etykieta przesyłki
            // Ważne! Wywołanie asynchroniczne GetAsync() jest tutaj zamienione na synchroniczne dzięki wywołaniu .Result
            // Zadania w Sello NX są wykonywane w osobnych wątkach i należy tutaj wywoływać metody API i zwracać ich wyniki synchronicznie
            var response = _httpClient.GetAsync($"/v1/shipments/{dane.IdZewnetrznyPaczki}/label?format=pdf").Result;

            // Sprawdzenie odpowiedzi serwera kurierskiego i obsłużenie błędów 
            if (!response.IsSuccessStatusCode)
                HandleErrors(response);

            // Dane etykiety pobrane z serwisu kuriera w formacie PDF zapisujemy jako tablicę bajtów (synchronicznie!)
            byte[] etykieta = response.Content.ReadAsByteArrayAsync().Result;

            // Dane pobrane z serwisu, należy zapisać w strukturze wynikowej tego zadania.
            return new PobranieEtykietyWynik()
            {
                Etykieta = etykieta
            };
        }

        /// <summary>
        /// Metoda, której zadaniem jest pobranie numeru nadawczego z serwisu kuriera dla konkretnej paczki identyfikowanej zewnętrznym identyfikatorem kuriera
        /// </summary>
        /// <param name="dane">Dane przekazane przez zadanie pobierania numeru nadawczego paczki z Sello NX, 
        /// na podstawie których plugin integracji może pobrać numer nadawczy (z danych o paczce) z serwisu kurierskiego.</param>
        /// <returns>Numer nadania przesyłki w ustalonym formacie wynikowym</returns>
        public PobranieNumeruNadawczegoWynik PobierzNumerNadawczy(PobranieNumeruNadawczegoDane dane)
        {

            // Uzupełnienie danych autoryzujących w żądaniu do serwisu kurierskiego
            // Uwaga! Sposób ten jest tymczasowy i w najbliższych wersjach dane autoryzacyjne będą dostarczane wprost w parametrze metody (dane.DaneUwierzytelnienia)
            PobierzDaneUwierzytelnienia(dane);

            // Wywołanie endpointa w serwisie kurierskim, pod którym jest dostępna informacja o numerze przesyłki.
            // Najczęściej są to pełne informacje o przesyłce, z których należy ten numer przesyłki wyciągnąć i zwrócić
            // Ważne! Wywołanie asynchroniczne GetAsync() jest tutaj zamienione na synchroniczne dzięki wywołaniu .Result
            // Zadania w Sello NX są wykonywane w osobnych wątkach i należy tutaj wywoływać metody API i zwracać ich wyniki synchronicznie
            var response = _httpClient.GetAsync($"/v1/shipments/{dane.IdZewnetrznyPaczki}").Result;
            var content = HandleErrorsWithContent<InpostShipmentDto>(response);

            // Dane pobrane z serwisu, należy zapisać w strukturze wynikowej tego zadania. 
            return new PobranieNumeruNadawczegoWynik()
            {
                NumerNadania = content.TrackingNumber
            };
        }

        /// <summary>
        /// Aktualnie nieużywana jeszcze metoda.
        /// </summary>
        /// <param name="dane"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public PobranieListyPunktowNadaniaWynik PobierzPunktyNadania(PobranieListyPunktowNadaniaDane dane)
        {
            throw new NotImplementedException();
        }

        
        #endregion IIntegracjaKuriera

        /// <summary>
        /// Pobranie i ustawienie danych uwierzytelniania
        /// Uwaga! Sposób ten jest tymczasowy i w najbliższych wersjach zostanie uproszczony. Dane uwierzytelniające będą przekazywany w wywołaniach metod
        /// </summary>
        /// <param name="dane"></param>
        private void PobierzDaneUwierzytelnienia(DaneOperacjiKontaIntegracji dane)
        {

            var kontoIntegracji = dane.Uchwyt.PodajObiektTypu<IKontaIntegracji>().Dane.Wszystkie().First(x => x.Id == dane.KontekstKonta.IdKonta);
            using (IKontoIntegracji konto = dane.Uchwyt.PodajObiektTypu<IKontaIntegracji>().Znajdz(kontoIntegracji))
            {
                var haslo = konto.DeszyfrujPole(PolaIntegracjiSelloStale.KontaSprzedazy.Haslo);
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", haslo);
            }
        }

        /// <summary>
        /// Przykładowa implementacja deserializacji oraz obsługi błędów komunikacji z serwisem kurierskim dla endpointów zwracających jakąś zawartość
        /// </summary>
        /// <typeparam name="TContent">Typ zwracanych danych</typeparam>
        /// <param name="response">Odpowiedź serwera kurierskiego</param>
        /// <returns>Dane zwrócone z serwisu kuriera</returns>
        /// <exception cref="InvalidOperationException">Wyjątek rzucany w przypadku gdy serwis nie zwróci wymaganych danych</exception>
        private TContent HandleErrorsWithContent<TContent>(HttpResponseMessage response)
        {
            // Sprawdzenie statusu HTTP odpowiedzi
            if (!response.IsSuccessStatusCode)
                HandleErrors(response);

            // Sprawdzenie czy serwis zwrócił jakąkolwiek oczekiwaną zawartość
            var content = response.Content.ReadAsStringAsync().Result;
            if (content == null)
            {
                throw new InvalidOperationException();
            }

            // Deserializacja odpowiedzi z serwisu kurierskiego do typu oczekiwanego lub błąd jeśli deserializacja się nie powiodła
            var deserializedObject = JsonConvert.DeserializeObject<TContent>(content);
            if (deserializedObject == null)
            {
                throw new InvalidOperationException();
            }

            return deserializedObject;
        }

        /// <summary>
        /// Przykładowa obsługa błędu HTTP, zwróconego z serwisu kurierskiego
        /// </summary>
        /// <param name="response"></param>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="UnauthorizedAccessException"></exception>
        /// <exception cref="ArgumentException"></exception>
        private void HandleErrors(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            // W zależności od statusu błędu HTTP można zwrócić konkretną informację rzucając wyjątek. 
            // Zamówienia wysyłkowe będą wtedy podświetlone na kolor czerwony wskazując problem z synchronizacją
            // a treść przekazana w wyjątku zostanie dołączona do informacji prezentowanej użytkownikowi programu.
            switch (response.StatusCode)
            {
                // Typowy błąd 404
                case HttpStatusCode.NotFound: throw new InvalidOperationException("Nie znaleziono przesyłki w serwisie kurierskim");

                // Błędy związane z brakiem autoryzacji, odmową dostępu, niepoprawnymi danymi logowania należy zwrócić rzucając wyjątek UnauthorizedAccessException
                // Dzięki temu Sello NX oznaczy konto integracji, jako niezweryfikowane i nie będzie ponawiać prób komunikacji z tym kontem, do czasu, aż
                // użytkownik nie poprawi danych autoryzujących. Sytuacja taka występuje np po zmianie hasła dostępowego po stronie serwisu kuriera.
                case HttpStatusCode.Unauthorized:
                case HttpStatusCode.Forbidden: throw new UnauthorizedAccessException();

            }

            // W przypadkach gdy serwis kurierski zwraca dodatkowo informacje na temat błędnie wysłanego żadania (np. niepoprawne dane w paczce)
            // należy je również wyciągnąć z odpowiedzi serwera i rzucić wyjątkiem z podaniem treści tych błędów.
            // Zostaną one wyświetlone użytkownikowi, aby miał szansę poprawić dane i ponowić żądanie
            var content = response.Content.ReadAsStringAsync().Result;
            var errorObj = JsonConvert.DeserializeObject<InpostBadRequestDto>(content);
            if (errorObj != null)
            {
                throw new ArgumentException(errorObj.Errors != null ? errorObj.Errors.ToString() : errorObj.Error);
            }
            else
            {
                throw new InvalidOperationException("Problem z odczytem informacji z serwera.");
            }
        }

    }
}