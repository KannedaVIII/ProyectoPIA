using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
namespace ProyectoPIA_IzanDelgado_WPF
{
    public class MySQLRepository : IRepository
    {
        private readonly string connectionString;

        public MySQLRepository(string connectionString)
        {
            this.connectionString = connectionString;
        }

        // ============================================================
        // OBTENER TODOS LOS VIDEOJUEGOS
        // ============================================================
        public List<Videojuegos> GetAll()
        {
            List<Videojuegos> lista = new List<Videojuegos>();

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT * FROM videojuegos";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                MySqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    lista.Add(new Videojuegos(
                        reader.GetString("title"),
                        reader.GetString("releaseDate"),
                        reader.GetString("team"),
                        reader.GetDecimal("rating"),
                        reader.GetString("timesListed"),
                        reader.GetString("numberReviews"),
                        reader.GetString("genres"),
                        reader.GetString("summary"),
                        reader.GetString("review")
                    ));
                }
            }

            return lista;
        }

        // ============================================================
        // BUSCAR POR TÍTULO
        // ============================================================
        public Videojuegos GetByTitle(string title)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT * FROM videojuegos WHERE title = @title LIMIT 1";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@title", title);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new Videojuegos(
                            reader.GetString("title"),
                            reader.GetString("releaseDate"),
                            reader.GetString("team"),
                            reader.GetDecimal("rating"),
                            reader.GetString("timesListed"),
                            reader.GetString("numberReviews"),
                            reader.GetString("genres"),
                            reader.GetString("summary"),
                            reader.GetString("review")
                        );
                    }
                }
            }

            return null;
        }

        // ============================================================
        // INSERTAR VIDEOJUEGO
        // ============================================================
        public void Insert(Videojuegos videojuegos)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = @"INSERT INTO videojuegos 
                    (title, releaseDate, team, rating, timesListed, numberReviews, genres, summary, review)
                    VALUES (@title, @releaseDate, @team, @rating, @timesListed, @numberReviews, @genres, @summary, @review)";

                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@title", videojuegos.Title);
                cmd.Parameters.AddWithValue("@releaseDate", videojuegos.ReleaseDate);
                cmd.Parameters.AddWithValue("@team", videojuegos.Team);
                cmd.Parameters.AddWithValue("@rating", videojuegos.Rating);
                cmd.Parameters.AddWithValue("@timesListed", videojuegos.TimesListed);
                cmd.Parameters.AddWithValue("@numberReviews", videojuegos.NumberReviews);
                cmd.Parameters.AddWithValue("@genres", videojuegos.Genres);
                cmd.Parameters.AddWithValue("@summary", videojuegos.Summary);
                cmd.Parameters.AddWithValue("@review", videojuegos.Review);
                cmd.ExecuteNonQuery();
            }
        }

        // ============================================================
        // ACTUALIZAR VIDEOJUEGO
        // ============================================================
        public void Update(Videojuegos videojuegos)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = @"UPDATE videojuegos 
                                 SET releaseDate=@releaseDate, 
                                     team=@team, 
                                     rating=@rating, 
                                     timesListed=@timesListed, 
                                     numberReviews=@numberReviews, 
                                     genres=@genres, 
                                     summary=@summary, 
                                     review=@review
                                 WHERE title=@title";

                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@releaseDate", videojuegos.ReleaseDate);
                cmd.Parameters.AddWithValue("@team", videojuegos.Team);
                cmd.Parameters.AddWithValue("@rating", videojuegos.Rating);
                cmd.Parameters.AddWithValue("@timesListed", videojuegos.TimesListed);
                cmd.Parameters.AddWithValue("@numberReviews", videojuegos.NumberReviews);
                cmd.Parameters.AddWithValue("@genres", videojuegos.Genres);
                cmd.Parameters.AddWithValue("@summary", videojuegos.Summary);
                cmd.Parameters.AddWithValue("@review", videojuegos.Review);
                cmd.Parameters.AddWithValue("@title", videojuegos.Title);
                cmd.ExecuteNonQuery();
            }
        }

        // ============================================================
        // ELIMINAR VIDEOJUEGO
        // ============================================================
        public void Delete(Videojuegos videojuegos)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "DELETE FROM videojuegos WHERE title=@title";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@title", videojuegos.Title);
                cmd.ExecuteNonQuery();
            }
        }

        // ============================================================
        // CARGAR DESDE LISTA (por ejemplo, desde CSV)
        // ============================================================
        public void CargarDesdeLista(List<Videojuegos> lista)
        {
            foreach (var videojuegos in lista)
            {
                try
                {
                    Insert(videojuegos);
                }
                catch (Exception)
                {
                    // Si ya existe, lo puedes ignorar o actualizar:
                    // Update(juego);
                }
            }
        }
    }
}
