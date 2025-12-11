namespace Backend
{
    public interface IRepository
    {
        public List<Videojuegos> GetAll();
        public Videojuegos GetByTitle(string Title);

        public void Insert(Videojuegos videojuegos);

        public void Update(Videojuegos videojuegos);

        public void Delete(Videojuegos videojuegos);

        public void CargarDesdeLista(List<Videojuegos> lista);



    }
}