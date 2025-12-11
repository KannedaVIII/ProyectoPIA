namespace ProyectoPIA_IzanDelgado_WPF
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


        public Videojuegos(string title, string releaseDate, string team, decimal rating,
                           string timesListed, string numberReviews, string genres,
                           string summary, string review)
        {
            Title = title;
            ReleaseDate = releaseDate;
            Team = team;
            Rating = rating;
            TimesListed = timesListed;
            NumberReviews = numberReviews;
            Genres = genres;
            Summary = summary;
            Review = review;
        }

        public void MostrarInfo()
        {
            Console.WriteLine("----- Información del Videojuego -----");
            Console.WriteLine($"Título: {Title}");
            Console.WriteLine($"Fecha de lanzamiento: {ReleaseDate}");
            Console.WriteLine($"Equipo desarrollador: {Team}");
            Console.WriteLine($"Géneros: {Genres}");
            Console.WriteLine($"Rating: {Rating}/5");
            Console.WriteLine($"Número de reseñas: {NumberReviews}");
            Console.WriteLine($"Veces listado: {TimesListed}");
            Console.WriteLine($"Resumen: {Summary}");
            Console.WriteLine($"Reseña destacada: {Review}");
            Console.WriteLine("--------------------------------------");
        }





    }
}
