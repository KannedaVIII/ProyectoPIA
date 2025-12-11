using System;

namespace InterfazMAUI.Models // <--- Fíjate que el namespace ha cambiado
{
    public class Videojuegos
    {
        public string Title { get; set; }
        public string ReleaseDate { get; set; }
        public string Team { get; set; }
        public decimal Rating { get; set; }
        public string TimesListed { get; set; }
        public string NumberReviews { get; set; }
        public string Genres { get; set; }
        public string Summary { get; set; }
        public string Review { get; set; }
        public Videojuegos() { }
    }
}