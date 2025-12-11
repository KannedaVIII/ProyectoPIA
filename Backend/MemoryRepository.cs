namespace Backend
{
    internal class MemoryRepository : IRepository
    {
        private List<Videojuegos> _videojuegos = new List<Videojuegos>();

        public List<Videojuegos> GetAll()
        {
            // Devuelve todos los videojuegos en memoria
            return new List<Videojuegos>(_videojuegos);
        }



        public Videojuegos GetByTitle(string title)
        {
            // Método personalizado para obtener un juego por título
            return _videojuegos.FirstOrDefault(v => v.Title.Equals(title, StringComparison.OrdinalIgnoreCase));
        }

        public void Insert(Videojuegos videojuegos)
        {
            if (videojuegos == null)
                throw new ArgumentNullException(nameof(videojuegos));

            // Evitar duplicados por título
            if (_videojuegos.Any(v => v.Title.Equals(videojuegos.Title, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException($"Ya existe un videojuego con el título '{videojuegos.Title}'.");

            _videojuegos.Add(videojuegos);
        }

        public void Update(Videojuegos videojuegos)
        {
            if (videojuegos == null)
                throw new ArgumentNullException(nameof(videojuegos));

            var existente = GetByTitle(videojuegos.Title);
            if (existente == null)
                throw new InvalidOperationException($"No se encontró un videojuego con el título '{videojuegos.Title}'.");

            // Actualiza los campos
            existente.ReleaseDate = videojuegos.ReleaseDate;
            existente.Team = videojuegos.Team;
            existente.Rating = videojuegos.Rating;
            existente.TimesListed = videojuegos.TimesListed;
            existente.NumberReviews = videojuegos.NumberReviews;
            existente.Genres = videojuegos.Genres;
            existente.Summary = videojuegos.Summary;
            existente.Review = videojuegos.Review;
        }

        public void Delete(Videojuegos videojuegos)
        {
            if (videojuegos == null)
                throw new InvalidOperationException($"No se encontró un videojuego con el título '{videojuegos.Title}'.");

            var existente = GetByTitle(videojuegos.Title);
            if (existente != null)
                _videojuegos.Remove(existente);
        }

        // Método auxiliar para precargar juegos desde tu lista del Main
        public void CargarDesdeLista(List<Videojuegos> lista)
        {
            if (lista == null)
                throw new ArgumentNullException(nameof(lista));

            _videojuegos = new List<Videojuegos>(lista);
        }
    }
}
