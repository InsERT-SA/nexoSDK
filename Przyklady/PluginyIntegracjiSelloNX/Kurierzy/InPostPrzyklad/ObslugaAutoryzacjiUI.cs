using InsERT.Moria.HandelElektroniczny.Rozszerzenia;

namespace InPostPrzyklad
{
    /// <summary>
    /// Klasa umożliwiająca wybranie wbudowanego sposobu autoryzacji przy dodawaniu nowego konta.
    /// Sello na podstawie tego wyboru wyświetli użytkownikowi odpowiedni interfejs, aby można było podać dane logowania do serwisu.
    /// </summary>
    public class ObslugaAutoryzacjiUI : IObslugaAutoryzacjiUI
    {
        /// <summary>
        /// W przypadku kuriera przykładowego, korzystamy z BasicAuthentication - czyli prosty login i hasło dostępowe do serwisu kuriera
        /// </summary>
        public RodzajAutoryzacjiIntegracji RodzajAutoryzacji => RodzajAutoryzacjiIntegracji.BasicAuthentication;
    }
}
